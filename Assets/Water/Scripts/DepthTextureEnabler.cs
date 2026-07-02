using UnityEngine;

[ExecuteAlways]
public static class DepthTextureEnabler
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnableDepthTexture()
    {
        // 先按标签找 MainCamera
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.depthTextureMode |= DepthTextureMode.Depth;
            Debug.Log("[DepthTextureEnabler] Camera depth mode set to: " + cam.depthTextureMode);
        }
        else
        {
            // 兜底: 遍历所有摄像机
            Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera c in allCameras)
            {
                c.depthTextureMode |= DepthTextureMode.Depth;
                Debug.Log("[DepthTextureEnabler] Fallback: depth mode set for camera: " + c.name);
            }
        }

        // 额外兜底: 监听后续动态创建的摄像机
        Camera.onPreCull += OnCameraPreCull;
    }

    static void OnCameraPreCull(Camera cam)
    {
        // 只对场景摄像机(非预览)启用, 避免 Editor Preview 干扰
        if (cam.cameraType == CameraType.Game || cam.cameraType == CameraType.SceneView)
        {
            cam.depthTextureMode |= DepthTextureMode.Depth;
        }
    }
}
