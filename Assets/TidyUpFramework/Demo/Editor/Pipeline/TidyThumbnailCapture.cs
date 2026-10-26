using System.IO;
using TidyUpFramework.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyThumbnailCapture
    {
        const int Width = 640;
        const int Height = 400;

        [MenuItem(TidyUpFramework.Editor.TidyEditorUtility.MenuRoot + "Demo Content/7. Level Thumbnails", false, 160)]
        public static void CaptureAll()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TidyLevelCatalog>(TidyDemoPaths.Levels + "/LevelCatalog.asset");
            if (catalog == null)
            {
                return;
            }

            foreach (var level in catalog.Levels)
            {
                if (level != null)
                {
                    Capture(level);
                }
            }

            AssetDatabase.SaveAssets();
        }

        public static void Capture(TidyLevelDefinition level)
        {
            var scenePath = TidyDemoPaths.Scenes + "/" + level.SceneName + ".unity";
            if (!File.Exists(scenePath))
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var camera = Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                return;
            }

            var texture = Render(camera);
            TidyAssetFactory.EnsureFolder(TidyDemoPaths.Thumbnails);
            var path = TidyDemoPaths.Thumbnails + "/Thumb_" + level.LevelId + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
            level.AssignThumbnail(AssetDatabase.LoadAssetAtPath<Sprite>(path));
            EditorUtility.SetDirty(level);
        }

        public static Texture2D Render(Camera camera, int width = Width, int height = Height)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            return texture;
        }
    }
}
