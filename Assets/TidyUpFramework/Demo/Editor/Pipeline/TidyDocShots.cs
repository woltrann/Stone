using System.Collections.Generic;
using System.IO;
using System.Linq;
using TidyUpFramework.Cleaning;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.Pipeline
{
    public static class TidyDocShots
    {
        public static void StepFrames(int frames)
        {
            EditorApplication.isPaused = true;
            for (var index = 0; index < frames; index++)
            {
                EditorApplication.Step();
            }
        }

        public static int Organise(float fraction, bool includeCleaning)
        {
            var objects = TidyObjectRegistry.All.Where(tidyObject => tidyObject.State == TidyObjectState.Loose && tidyObject.IsMovable).OrderBy(tidyObject => tidyObject.SequenceIndex).ToList();
            var limit = Mathf.RoundToInt(objects.Count * Mathf.Clamp01(fraction));
            var placed = 0;
            foreach (var tidyObject in objects)
            {
                if (placed >= limit)
                {
                    break;
                }

                var target = TidyTargetRegistry.All.FirstOrDefault(candidate => candidate.Evaluate(TidyPlacementQuery.WithoutAim(tidyObject)) is var evaluation && evaluation.IsValid && evaluation.IsCorrect);
                if (target == null)
                {
                    continue;
                }

                tidyObject.MarkHeld();
                if (target.TryPlace(target.Evaluate(TidyPlacementQuery.WithoutAim(tidyObject)), true))
                {
                    placed++;
                }

                if (placed % 4 == 0)
                {
                    StepFrames(1);
                }
            }

            if (includeCleaning)
            {
                var cleanables = TidyCleanable.All.ToList();
                var cleanLimit = Mathf.RoundToInt(cleanables.Count * Mathf.Clamp01(fraction));
                for (var index = 0; index < cleanLimit; index++)
                {
                    cleanables[index].Scrub(100f);
                }
            }

            return placed;
        }

        public static string PlayerView(string fileName, bool includeHud)
        {
            var camera = Camera.main;
            return TidyDocCapture.Capture(camera, fileName, includeHud);
        }

        public static string TargetView(TidyPlacementTarget target, string fileName, float distance = 1.3f, float height = 0.35f)
        {
            var focus = TargetCenter(target);
            var viewer = Camera.main != null ? Camera.main.transform.position : focus - Vector3.forward;
            var forward = viewer - focus;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.back;
            var position = focus + forward * distance + Vector3.up * height;
            var rotation = Quaternion.LookRotation(focus - position);
            return TidyDocCapture.CaptureFrom(position, rotation.eulerAngles, 50f, fileName, false);
        }

        public static Vector3 TargetCenter(TidyPlacementTarget target)
        {
            var trigger = target.GetComponent<BoxCollider>();
            return trigger != null ? trigger.bounds.center : target.transform.position;
        }

        public static string Composite(IReadOnlyList<string> sourceFiles, int columns, string fileName, int cellWidth = 800, int cellHeight = 450, int gap = 8)
        {
            var rows = Mathf.CeilToInt(sourceFiles.Count / (float)columns);
            var width = columns * cellWidth + (columns + 1) * gap;
            var height = rows * cellHeight + (rows + 1) * gap;
            var sheet = new Texture2D(width, height, TextureFormat.RGB24, false);
            var background = new Color32(251, 248, 243, 255);
            var fill = new Color32[width * height];
            for (var index = 0; index < fill.Length; index++)
            {
                fill[index] = background;
            }

            sheet.SetPixels32(fill);
            for (var index = 0; index < sourceFiles.Count; index++)
            {
                var source = new Texture2D(2, 2);
                source.LoadImage(File.ReadAllBytes(sourceFiles[index]));
                var column = index % columns;
                var row = rows - 1 - index / columns;
                var originX = gap + column * (cellWidth + gap);
                var originY = gap + row * (cellHeight + gap);
                for (var y = 0; y < cellHeight; y++)
                {
                    for (var x = 0; x < cellWidth; x++)
                    {
                        sheet.SetPixel(originX + x, originY + y, source.GetPixelBilinear((x + 0.5f) / cellWidth, (y + 0.5f) / cellHeight));
                    }
                }

                Object.DestroyImmediate(source);
            }

            sheet.Apply();
            var path = TidyDocCapture.ImageFolder + "/" + fileName + ".png";
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            return path;
        }
    }
}
