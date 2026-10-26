using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyDocCapture
    {
        public const string ImageFolder = TidyDemoPaths.Root + "/Documentation/images";
        const int DefaultWidth = 1600;
        const int DefaultHeight = 900;
        const float CanvasPlaneDistance = 0.4f;

        public static string Capture(Camera camera, string fileName, bool includeCanvases, int width = DefaultWidth, int height = DefaultHeight)
        {
            var restore = new List<(Canvas canvas, RenderMode mode, Camera camera, float distance)>();
            if (includeCanvases)
            {
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay || !canvas.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    restore.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = Mathf.Max(camera.nearClipPlane + 0.01f, CanvasPlaneDistance);
                }

                Canvas.ForceUpdateCanvases();
            }

            var texture = TidyThumbnailCapture.Render(camera, width, height);
            foreach (var (canvas, mode, previousCamera, distance) in restore)
            {
                canvas.renderMode = mode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = distance;
            }

            Directory.CreateDirectory(ImageFolder);
            var path = ImageFolder + "/" + fileName + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return path;
        }

        public static string CaptureFrom(Vector3 position, Vector3 euler, float fieldOfView, string fileName, bool includeCanvases)
        {
            var source = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            var cameraObject = source != null ? Object.Instantiate(source.gameObject) : new GameObject("Doc Camera", typeof(Camera));
            foreach (var behaviour in cameraObject.GetComponents<MonoBehaviour>())
            {
                if (behaviour.GetType().Namespace == null || !behaviour.GetType().Namespace.StartsWith("UnityEngine"))
                {
                    behaviour.enabled = false;
                }
            }

            var listener = cameraObject.GetComponent<AudioListener>();
            if (listener != null)
            {
                Object.DestroyImmediate(listener);
            }

            cameraObject.transform.SetParent(null);
            cameraObject.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = fieldOfView;
            var path = Capture(camera, fileName, includeCanvases);
            Object.DestroyImmediate(cameraObject);
            return path;
        }
    }
}
