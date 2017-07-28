using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

namespace RockVR.Video
{
    /// <summary>
    /// <c>VideoCapture</c> component.
    /// Place this script to target <c>Camera</c> component, this will capture
    /// camera's render texture and encode to video file.
    /// </summary>
    public class VideoCapture : VideoCaptureBase
    {
        /// <summary>
        /// Get or set the current status.
        /// </summary>
        /// <value>The current status.</value>
        public VideoCaptureCtrl.StatusType status { get; set; }
        /// <summary>
        /// For generate equirectangular video.
        /// </summary>
        public Material cubemap2Equirectangular;
        /// <summary>
        /// Setup Time.maximumDeltaTime to avoiding nasty stuttering.
        /// https://docs.unity3d.com/ScriptReference/Time-maximumDeltaTime.html
        /// </summary>
        public bool offlineRender = false;
        /// <summary>
        /// The texture holding the video frame data.
        /// </summary>
        private Texture2D frameTexture;
        private RenderTexture frameRenderTexture;
        private Cubemap frameCubemap;
        /// <summary>
        /// Whether or not there is a frame capturing now.
        /// </summary>
        private bool isCapturingFrame;
        /// <summary>
        /// The time spent during capturing.
        /// </summary>
        private float capturingTime;
        /// <summary>
        /// Frame statistics info.
        /// </summary>
        private int capturedFrameCount;
        private int encodedFrameCount;
        /// <summary>
        /// Reference to native lib API.
        /// </summary>
        private System.IntPtr libAPI;
        /// <summary>
        /// The original maximum delta time.
        /// </summary>
        private float originalMaximumDeltaTime;
        /// <summary>
        /// Frame data will be sent to frame encode queue.
        /// </summary>
        private struct FrameData
        {
            /// <summary>
            /// The RGB pixels will be encoded.
            /// </summary>a
            public byte[] pixels;
            /// <summary>
            /// How many this frame will be counted.
            /// </summary>
            public int count;
            /// <summary>
            /// Constructor.
            /// </summary>
            public FrameData(byte[] p, int c)
            {
                pixels = p;
                count = c;
            }
        }
        /// <summary>
        /// The frame encode queue.
        /// </summary>
        private Queue<FrameData> frameQueue;
        /// <summary>
        /// The frame encode thread.
        /// </summary>
        private Thread encodeThread;
        /// <summary>
        /// Cleanup this instance.
        /// </summary>
        public void Cleanup()
        {
            if (mode != ModeType.LIVE_STREAMING)
            {
                if (File.Exists(path)) File.Delete(path);
            }
            if (mode == ModeType.LIVE_STREAMING)
            {
                LibVideoStreamingAPI_Clean(libAPI);
            }
            else
            {
                LibVideoCaptureAPI_Clean(libAPI);
            }
        }
        /// <summary>
        /// Start capture video.
        /// </summary>
        public override void StartCapture()
        {
            // Check if we can start capture session.
            if (status != VideoCaptureCtrl.StatusType.NOT_START &&
                status != VideoCaptureCtrl.StatusType.FINISH)
            {
                Debug.LogWarning("[VideoCapture::StartCapture] Previous " +
                                 " capture not finish yet!");
                return;
            }
            if (mode == ModeType.LIVE_STREAMING)
            {
                if (!StringUtils.IsRtmpAddress(streamingAddress))
                {
                    Debug.LogWarning(
                       "[VideoCapture::StartCapture] Video live streaming " +
                       "require rtmp server address setup!"
                    );
                    return;
                }
            }
            if (format == FormatType.PANORAMA && !isDedicated)
            {
                Debug.LogWarning(
                    "[VideoCapture::StartCapture] Capture equirectangular " +
                    "video always require dedicated camera!"
                );
                isDedicated = true;
            }
            if (mode == ModeType.LOCAL)
            {
                path = PathConfig.saveFolder + StringUtils.GetMp4FileName(StringUtils.GetRandomString(5));
            }
            // Create a RenderTexture with desired frame size for dedicated
            // camera capture to store pixels in GPU.
            if (isDedicated)
            {
                // Use Camera.targetTexture as RenderTexture if already existed.
                if (captureCamera.targetTexture != null)
                {
                    // Use binded rendertexture will ignore antiAliasing config.
                    frameRenderTexture = captureCamera.targetTexture;
                }
                else
                {
                    // Create a rendertexture for video capture.
                    // Size it according to the desired video frame size.
                    frameRenderTexture = new RenderTexture(frameWidth, frameHeight, 24);
                    frameRenderTexture.antiAliasing = antiAliasing;
                    frameRenderTexture.wrapMode = TextureWrapMode.Clamp;
                    frameRenderTexture.filterMode = FilterMode.Trilinear;
                    frameRenderTexture.anisoLevel = 0;
                    frameRenderTexture.hideFlags = HideFlags.HideAndDontSave;
                    // Make sure the rendertexture is created.
                    frameRenderTexture.Create();
                    captureCamera.targetTexture = frameRenderTexture;
                }
            }
            // For capturing normal 2D video, use frameTexture(Texture2D) for
            // intermediate cpu saving, frameRenderTexture(RenderTexture) store
            // the pixels read by frameTexture.
            if (format == FormatType.NORMAL)
            {
                if (isDedicated)
                {
                    // Set the aspect ratio of the camera to match the rendertexture.
                    captureCamera.aspect = frameWidth / ((float)frameHeight);
                    captureCamera.targetTexture = frameRenderTexture;
                }
            }
            // For capture panorama video:
            // EQUIRECTANGULAR: use frameCubemap(Cubemap) for intermediate cpu
            // saving.
            // CUBEMAP: use frameTexture(Texture2D) for intermediate cpu saving.
            else if (format == FormatType.PANORAMA)
            {
                // Create render cubemap.
                frameCubemap = new Cubemap(cubemapSize, TextureFormat.RGB24, false);
                // Setup camera as required for panorama capture.
                captureCamera.aspect = 1.0f;
                captureCamera.fieldOfView = 90;
            }
            // Pixels stored in frameRenderTexture(RenderTexture) always read by frameTexture(Texture2D).
            // NORMAL:
            // camera render -> frameRenderTexture -> frameTexture -> frameQueue
            // CUBEMAP:
            // 6 cameras render -> 6 faceRenderTexture -> frameTexture -> frameQueue
            // EQUIRECTANGULAR:
            // 6 camera render -> 6 faceRenderTexture-> frameCubemap -> Cubemap2Equirect -> 
            // frameRenderTexture -> frameTexture -> frameQueue
            frameTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
            frameTexture.hideFlags = HideFlags.HideAndDontSave;
            frameTexture.wrapMode = TextureWrapMode.Clamp;
            frameTexture.filterMode = FilterMode.Trilinear;
            frameTexture.hideFlags = HideFlags.HideAndDontSave;
            frameTexture.anisoLevel = 0;
            // Reset tempory variables.
            capturingTime = 0f;
            capturedFrameCount = 0;
            encodedFrameCount = 0;
            frameQueue = new Queue<FrameData>();
            // Projection info for native plugin.
            int proj = 0;
            if (format == FormatType.PANORAMA)
            {
                if (panoramaProjection == PanoramaProjectionType.EQUIRECTANGULAR)
                    proj = 1;
                if (panoramaProjection == PanoramaProjectionType.CUBEMAP)
                    proj = 2;
            }
            if (mode == ModeType.LIVE_STREAMING)
            {
                libAPI = LibVideoStreamingAPI_Get(
                    frameWidth,
                    frameHeight,
                    targetFramerate,
                    proj,
                    streamingAddress,
                    PathConfig.ffmpegPath);
            }
            else
            {
                libAPI = LibVideoCaptureAPI_Get(
                    frameWidth,
                    frameHeight,
                    targetFramerate,
                    proj,
                    path,
                    PathConfig.ffmpegPath);
            }
            if (libAPI == System.IntPtr.Zero)
            {
                Debug.LogWarning("[VideoCapture::StartCapture] Get native " +
                                 "capture api failed!");
                return;
            }
            if (offlineRender)
            {
                // Backup maximumDeltaTime states.
                originalMaximumDeltaTime = Time.maximumDeltaTime;
                Time.maximumDeltaTime = Time.fixedDeltaTime;
            }
            // Start encoding thread.
            encodeThread = new Thread(FrameEncodeThreadFunction);
            encodeThread.Priority = System.Threading.ThreadPriority.Lowest;
            encodeThread.IsBackground = true;
            encodeThread.Start();
            // Update current status.
            status = VideoCaptureCtrl.StatusType.STARTED;
        }
        /// <summary>
        /// Stop capture video.
        /// </summary>
        public override void StopCapture()
        {
            if (status != VideoCaptureCtrl.StatusType.STARTED)
            {
                Debug.LogWarning("[VideoCapture::StopCapture] capture session " +
                                 "not start yet!");
                return;
            }
            if (offlineRender)
            {
                // Restore maximumDeltaTime states.
                Time.maximumDeltaTime = originalMaximumDeltaTime;
            }
            // Update current status.
            status = VideoCaptureCtrl.StatusType.STOPPED;
        }

        #region Unity Lifecycle
        /// <summary>
        /// Called before any Start functions and also just after a prefab is instantiated.
        /// </summary>
        private new void Awake()
        {
            base.Awake();
            status = VideoCaptureCtrl.StatusType.NOT_START;
        }
        /// <summary>
        /// Called after a camera finishes rendering the scene.
        /// </summary>
        private void OnPostRender()
        {
            // Normal video capture process run in OnPostRender.
            if (format != FormatType.NORMAL ||
                status != VideoCaptureCtrl.StatusType.STARTED) // Capture not started yet.
            {
                return;
            }
            capturingTime += Time.deltaTime;
            if (!isCapturingFrame)
            {
                int totalRequiredFrameCount =
                    (int)(capturingTime / deltaFrameTime);
                // Skip frames if we already got enough.
                if (totalRequiredFrameCount > capturedFrameCount)
                {
                    StartCoroutine(CaptureFrameAsync());
                }
            }
        }
        /// <summary>
        /// Called once per frame, after Update has finished.
        /// </summary>
        private void LateUpdate()
        {
            // Equirectangular run in LateUpdate.
            if (format != FormatType.PANORAMA ||
                status != VideoCaptureCtrl.StatusType.STARTED) // Capture not started yet.
            {
                return;
            }
            capturingTime += Time.deltaTime;
            if (!isCapturingFrame)
            {
                int totalRequiredFrameCount =
                    (int)(capturingTime / deltaFrameTime);
                // Skip frames if we already got enough.
                if (totalRequiredFrameCount > capturedFrameCount)
                {
                    CaptureCubemapFrameSync();
                }
            }
        }
        /// <summary>
        /// Sent to all game objects before the application is quit.
        /// </summary>
        private void OnApplicationQuit()
        {
            if (status == VideoCaptureCtrl.StatusType.STARTED)
            {
                StopCapture();
            }
        }
        #endregion // Unity Lifecycle

        #region Video Capture Core
        /// <summary>
        ///  Capture frame Async impl.
        /// </summary>
        private IEnumerator CaptureFrameAsync()
        {
            isCapturingFrame = true;
            if (status == VideoCaptureCtrl.StatusType.STARTED)
            {
                CopyFrameTexture();
                yield return new WaitForEndOfFrame();
                EnqueueFrameTexture();
            }
            isCapturingFrame = false;
        }
        /// <summary>
        /// Capture frame Sync impl.
        /// </summary>
        private void CaptureCubemapFrameSync()
        {
            int width = cubemapSize;
            int height = cubemapSize;

            CubemapFace[] faces = new CubemapFace[] {
                CubemapFace.PositiveX,
                CubemapFace.NegativeX,
                CubemapFace.PositiveY,
                CubemapFace.NegativeY,
                CubemapFace.PositiveZ,
                CubemapFace.NegativeZ
            };
            Vector3[] faceAngles = new Vector3[] {
                new Vector3(0.0f, 90.0f, 0.0f),
                new Vector3(0.0f, -90.0f, 0.0f),
                new Vector3(-90.0f, 0.0f, 0.0f),
                new Vector3(90.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 180.0f, 0.0f)
            };

            // Reset capture camera rotation.
            captureCamera.transform.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);

            // Create cubemap face render texture.
            RenderTexture faceTexture = new RenderTexture(width, height, 24);
            faceTexture.antiAliasing = antiAliasing;
#if !(UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            faceTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
#endif
            faceTexture.hideFlags = HideFlags.HideAndDontSave;
            // For intermediate saving
            Texture2D swapTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            swapTexture.hideFlags = HideFlags.HideAndDontSave;
            // Prepare for target render texture.
            captureCamera.targetTexture = faceTexture;

            // TODO, make this into shader for GPU fast processing.
            if (panoramaProjection == PanoramaProjectionType.CUBEMAP)
            {
                for (int i = 0; i < faces.Length; i++)
                {
                    captureCamera.transform.eulerAngles = faceAngles[i];
                    captureCamera.Render();
                    RenderTexture.active = faceTexture;
                    swapTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    Color[] pixels = swapTexture.GetPixels();
                    switch (i)
                    {
                        case (int)CubemapFace.PositiveX:
                            frameTexture.SetPixels(0, height, width, height, pixels);
                            break;
                        case (int)CubemapFace.NegativeX:
                            frameTexture.SetPixels(width, height, width, height, pixels);
                            break;
                        case (int)CubemapFace.PositiveY:
                            frameTexture.SetPixels(width * 2, height, width, height, pixels);
                            break;
                        case (int)CubemapFace.NegativeY:
                            frameTexture.SetPixels(0, 0, width, height, pixels);
                            break;
                        case (int)CubemapFace.PositiveZ:
                            frameTexture.SetPixels(width, 0, width, height, pixels);
                            break;
                        case (int)CubemapFace.NegativeZ:
                            frameTexture.SetPixels(width * 2, 0, width, height, pixels);
                            break;
                    }
                }
                frameTexture.Apply();
            }
            else if (panoramaProjection == PanoramaProjectionType.EQUIRECTANGULAR)
            {
                Color[] mirroredPixels = new Color[swapTexture.height * swapTexture.width];
                for (int i = 0; i < faces.Length; i++)
                {
                    captureCamera.transform.eulerAngles = faceAngles[i];
                    captureCamera.Render();
                    RenderTexture.active = faceTexture;
                    swapTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    // Mirror vertically to meet the standard of unity cubemap.
                    Color[] OrignalPixels = swapTexture.GetPixels();
                    for (int y1 = 0; y1 < height; y1++)
                    {
                        for (int x1 = 0; x1 < width; x1++)
                        {
                            mirroredPixels[y1 * width + x1] =
                                OrignalPixels[((height - 1 - y1) * width) + x1];
                        }
                    }
                    frameCubemap.SetPixels(mirroredPixels, faces[i]);
                }
                frameCubemap.SmoothEdges();
                frameCubemap.Apply();
                // Convert to equirectangular projection.
                Graphics.Blit(frameCubemap, frameRenderTexture, cubemap2Equirectangular);
                // From frameRenderTexture to frameTexture.
                CopyFrameTexture();
            }

            RenderTexture.active = null;
            captureCamera.targetTexture = null;

            // Clean temp texture.
            DestroyImmediate(swapTexture);
            DestroyImmediate(faceTexture);

            // Send for encoding.
            EnqueueFrameTexture();
        }
        /// <summary>
        /// Copy the frame texture from GPU to CPU.
        /// </summary>
        void CopyFrameTexture()
        {
            // Bind texture.
            RenderTexture.active = frameRenderTexture;
            // TODO, remove expensive step of copying pixel data from GPU to CPU.
            frameTexture.ReadPixels(new Rect(0, 0, frameWidth, frameHeight), 0, 0, false);
            frameTexture.Apply();
            // Restore RenderTexture states.
            RenderTexture.active = null;
        }
        /// <summary>
        /// Send the captured frame texture to encode queue.
        /// </summary>
        void EnqueueFrameTexture()
        {
            int totalRequiredFrameCount = (int)(capturingTime / deltaFrameTime);
            int requiredFrameCount = totalRequiredFrameCount - capturedFrameCount;
            lock (this)
            {
                frameQueue.Enqueue(
                    new FrameData(frameTexture.GetRawTextureData(), requiredFrameCount));
            }
            capturedFrameCount = totalRequiredFrameCount;
        }
        /// <summary>
        /// Frame encoding thread impl.
        /// </summary>
        private void FrameEncodeThreadFunction()
        {
            while (status == VideoCaptureCtrl.StatusType.STARTED || frameQueue.Count > 0)
            {
                if (frameQueue.Count > 0)
                {
                    FrameData frame;
                    lock (this)
                    {
                        frame = frameQueue.Dequeue();
                    }
                    if (mode == ModeType.LIVE_STREAMING)
                    {
                        LibVideoStreamingAPI_SendFrames(libAPI, frame.pixels, frame.count);
                    }
                    else
                    {
                        LibVideoCaptureAPI_SendFrames(libAPI, frame.pixels, frame.count);
                    }
                    encodedFrameCount++;
                    if (VideoCaptureCtrl.instance.debug)
                    {
                        Debug.Log(
                            "[VideoCapture::FrameEncodeThreadFunction] Encoded " +
                            encodedFrameCount + " frames. " + frameQueue.Count +
                            " frames remaining."
                        );
                    }
                }
                else
                {
                    // Wait 1 second for captured frame.
                    Thread.Sleep(1000);
                }
            }
            // Notify native encoding process finish.
            if (mode == ModeType.LIVE_STREAMING)
            {
                LibVideoStreamingAPI_Close(libAPI);
            }
            else
            {
                LibVideoCaptureAPI_Close(libAPI);
            }
            // Notify caller video capture complete.
            if (eventDelegate.OnComplete != null)
            {
                eventDelegate.OnComplete();
            }
            if (VideoCaptureCtrl.instance.debug)
            {
                Debug.Log("[VideoCapture::FrameEncodeThreadFunction] Encode " +
                          "process finish!");
            }
            // Update current status.
            status = VideoCaptureCtrl.StatusType.FINISH;
        }
        #endregion // Video Capture Core

        #region Dll Import
        [DllImport("VideoCaptureLib")]
        static extern System.IntPtr LibVideoCaptureAPI_Get(int width, int height, int rate, int proj, string path, string ffpath);

        [DllImport("VideoCaptureLib")]
        static extern void LibVideoCaptureAPI_SendFrames(System.IntPtr api, byte[] data, int count);

        [DllImport("VideoCaptureLib")]
        static extern void LibVideoCaptureAPI_Close(System.IntPtr api);

        [DllImport("VideoCaptureLib")]
        static extern void LibVideoCaptureAPI_Clean(System.IntPtr api);

        [DllImport("VideoCaptureLib")]
        static extern System.IntPtr LibVideoStreamingAPI_Get(int width, int height, int rate, int proj, string address, string ffpath);

        [DllImport("VideoCaptureLib")]
        static extern void LibVideoStreamingAPI_SendFrames(System.IntPtr api, byte[] data, int count);

        [DllImport("VideoCaptureLib")]
        static extern void LibVideoStreamingAPI_Close(System.IntPtr api);

        [DllImport("VideoCaptureLib")]
        static extern void LibVideoStreamingAPI_Clean(System.IntPtr api);
        #endregion // Dll Import
    }

    /// <summary>
    /// <c>VideoMerger</c> is processed after temp video captured, with or without
    /// temp audio captured. If audio captured, it will merge the video and audio
    /// within same file.
    /// </summary>
    public class VideoMerger
    {
        /// <summary>
        /// The merged video file path.
        /// </summary>
        public string path;
        /// <summary>
        /// The capture video instance.
        /// </summary>
        private VideoCapture videoCapture;
        /// <summary>
        /// The capture audio instance.
        /// </summary>
        private AudioCapture audioCapture;
        /// <summary>
        /// Initializes a new instance of the <see cref="T:RockVR.Video.VideoMerger"/> class.
        /// </summary>
        /// <param name="videoCapture">Video capture.</param>
        /// <param name="audioCapture">Audio capture.</param>
        public VideoMerger(VideoCapture videoCapture, AudioCapture audioCapture)
        {
            this.videoCapture = videoCapture;
            this.audioCapture = audioCapture;
        }
        /// <summary>
        /// Video/Audio merge function impl.
        /// Blocking function.
        /// </summary>
        public bool Merge()
        {
            path = PathConfig.saveFolder + StringUtils.GetMp4FileName(StringUtils.GetRandomString(5));
            System.IntPtr libAPI = LibVideoMergeAPI_Get(
                videoCapture.bitrate,
                path,
                videoCapture.path,
                audioCapture.path,
                PathConfig.ffmpegPath);
            if (libAPI == System.IntPtr.Zero)
            {
                Debug.LogWarning("[VideoMerger::Merge] Get native LibVideoMergeAPI failed!");
                return false;
            }
            LibVideoMergeAPI_Merge(libAPI);
            // Make sure generated the merge file.
            int waitCount = 0;
            while (!File.Exists(path))
            {
                if (waitCount++ < 100)
                    Thread.Sleep(500);
                else
                {
                    Debug.LogWarning("[VideoMerger::Merge] Merge process failed!");
                    LibVideoMergeAPI_Clean(libAPI);
                    return false;
                }
            }
            LibVideoMergeAPI_Clean(libAPI);
            if (VideoCaptureCtrl.instance.debug)
            {
                Debug.Log("[VideoMerger::Merge] Merge process finish!");
            }
            return true;
        }

        #region Dll Import
        [DllImport("VideoCaptureLib")]
        static extern System.IntPtr LibVideoMergeAPI_Get(int rate, string path, string vpath, string apath, string ffpath);

        [DllImport("VideoCaptureLib")]
        static extern void LibVideoMergeAPI_Merge(System.IntPtr api);

        [DllImport("VideoCaptureLib")]
        static extern void LibVideoMergeAPI_Clean(System.IntPtr api);
        #endregion // Dll Import
    }
}