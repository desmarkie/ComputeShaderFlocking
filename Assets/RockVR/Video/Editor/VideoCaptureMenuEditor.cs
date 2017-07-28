using UnityEngine;
using UnityEditor;
using RockVR.Common;

namespace RockVR.Video.Editor
{
    public class VideoCaptureMenuEditor
    {
        [MenuItem("RockVR/VideoCapture/Fix FFmpeg Permission for OSX")]
        private static void FixFFmpegPermissionForOSX()
        {
            CmdProcess.Run("chmod", "a+x " + PathConfig.ffmpegPath);
            UnityEngine.Debug.Log("Grant permission for: " + PathConfig.ffmpegPath);
        }

        [MenuItem("RockVR/VideoCapture/Prepare Panorama Capture")]
        private static void PreparePanoramaCapture() {
            // Change to gamma color space.
            // https://docs.unity3d.com/Manual/LinearLighting.html
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            UnityEngine.Debug.Log("Set color space to: Gamma");
        }

        [MenuItem("RockVR/VideoCapture/Prepare Normal Capture")]
        private static void PrepareNormalCapture() {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            UnityEngine.Debug.Log("Set color space to: Linear");
        }
    }
}