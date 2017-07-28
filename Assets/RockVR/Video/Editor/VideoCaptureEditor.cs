using UnityEngine;
using UnityEditor;

namespace RockVR.Video.Editor
{
    /// <summary>
    /// <c>VideoCapture</c> component editor.
    /// </summary>
    [CustomEditor(typeof(VideoCapture))]
    public class VideoCaptureEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            VideoCapture videoCapture = (VideoCapture)target;

            videoCapture.mode = (VideoCapture.ModeType)EditorGUILayout.EnumPopup("Mode", videoCapture.mode);

            if (videoCapture.mode == VideoCapture.ModeType.LIVE_STREAMING)
            {
                videoCapture.streamingAddress = EditorGUILayout.TextField("Streaming Server Address", videoCapture.path);
            }
            videoCapture.format = (VideoCapture.FormatType)EditorGUILayout.EnumPopup("Format", videoCapture.format);
            if (videoCapture.format == VideoCapture.FormatType.NORMAL)
            {
                videoCapture.frameSize = (VideoCapture.FrameSizeType)EditorGUILayout.EnumPopup("Frame Size", videoCapture.frameSize);
            }
            else if (videoCapture.format == VideoCapture.FormatType.PANORAMA)
            {
                videoCapture.panoramaProjection = (VideoCapture.PanoramaProjectionType)EditorGUILayout.EnumPopup("Projection Type", videoCapture.panoramaProjection);
                if (videoCapture.panoramaProjection == VideoCapture.PanoramaProjectionType.EQUIRECTANGULAR)
                {
                    videoCapture.frameSize = (VideoCapture.FrameSizeType)EditorGUILayout.EnumPopup("Frame Size", videoCapture.frameSize);
                    videoCapture.cubemap2Equirectangular = (Material)EditorGUILayout.ObjectField("Cubemap to Equirectangular Material", videoCapture.cubemap2Equirectangular, typeof(Material));
                }
                videoCapture._cubemapSize = (VideoCapture.CubemapSizeType)EditorGUILayout.EnumPopup("Cubemap Size", videoCapture._cubemapSize);
            }
            videoCapture.offlineRender = EditorGUILayout.Toggle("Offline Render", videoCapture.offlineRender);
            videoCapture.encodeQuality = (VideoCapture.EncodeQualityType)EditorGUILayout.EnumPopup("Encode Quality", videoCapture.encodeQuality);
            videoCapture._antiAliasing = (VideoCapture.AntiAliasingType)EditorGUILayout.EnumPopup("Anti Aliasing", videoCapture._antiAliasing);
            videoCapture._targetFramerate = (VideoCapture.TargetFramerateType)EditorGUILayout.EnumPopup("Target FrameRate", videoCapture._targetFramerate);
            videoCapture.isDedicated = EditorGUILayout.Toggle("Dedicated Camera", videoCapture.isDedicated);
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}