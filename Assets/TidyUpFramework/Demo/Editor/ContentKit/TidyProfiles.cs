using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static class TidyProfiles
    {
        private const float DuplicateDistance = 1e-6f;
        private const int CurveSegments = 8;
        private const float QuarterTurnDegrees = 90f;

        public static Vector2[] RoundedCylinder(float radius, float height, float bevel, int bevelSegments)
        {
            float rounding = Mathf.Clamp(bevel, 0f, Mathf.Min(radius, height * 0.5f));
            var points = new List<Vector2> { Vector2.zero };
            int segments = Mathf.Max(1, bevelSegments);
            AppendArc(points, new Vector2(radius - rounding, rounding), rounding, -QuarterTurnDegrees, 0f, segments);
            AppendArc(points, new Vector2(radius - rounding, height - rounding), rounding, 0f, QuarterTurnDegrees, segments);
            Append(points, new Vector2(0f, height));
            return points.ToArray();
        }

        public static Vector2[] Bottle(float radius, float height, float neckRadius)
        {
            float lip = neckRadius * 1.18f;
            var points = new List<Vector2> { Vector2.zero };
            AppendArc(points, new Vector2(radius * 0.88f, radius * 0.12f), radius * 0.12f, -QuarterTurnDegrees, 0f, 3);
            Append(points, new Vector2(radius, height * 0.58f));
            AppendQuadratic(points, new Vector2(radius, height * 0.58f), new Vector2(radius, height * 0.78f), new Vector2(neckRadius, height * 0.8f), CurveSegments);
            Append(points, new Vector2(neckRadius, height * 0.93f));
            Append(points, new Vector2(lip, height * 0.94f));
            Append(points, new Vector2(lip, height));
            Append(points, new Vector2(neckRadius * 0.75f, height));
            Append(points, new Vector2(neckRadius * 0.75f, height * 0.96f));
            Append(points, new Vector2(0f, height * 0.96f));
            return points.ToArray();
        }

        public static Vector2[] Jar(float radius, float height)
        {
            float shoulder = height * 0.86f;
            var points = new List<Vector2> { Vector2.zero };
            AppendArc(points, new Vector2(radius * 0.9f, radius * 0.1f), radius * 0.1f, -QuarterTurnDegrees, 0f, 3);
            Append(points, new Vector2(radius, shoulder));
            AppendQuadratic(points, new Vector2(radius, shoulder), new Vector2(radius, height * 0.93f), new Vector2(radius * 0.82f, height * 0.93f), 4);
            Append(points, new Vector2(radius * 0.82f, height));
            Append(points, new Vector2(0f, height));
            return points.ToArray();
        }

        public static Vector2[] Vase(float radius, float height)
        {
            var points = new List<Vector2> { Vector2.zero, new Vector2(radius * 0.55f, 0f) };
            AppendQuadratic(points, new Vector2(radius * 0.55f, 0f), new Vector2(radius * 1.25f, height * 0.3f), new Vector2(radius * 0.45f, height * 0.75f), CurveSegments);
            AppendQuadratic(points, new Vector2(radius * 0.45f, height * 0.75f), new Vector2(radius * 0.38f, height * 0.9f), new Vector2(radius * 0.62f, height), 4);
            Append(points, new Vector2(radius * 0.5f, height));
            Append(points, new Vector2(radius * 0.4f, height * 0.9f));
            Append(points, new Vector2(0f, height * 0.9f));
            return points.ToArray();
        }

        public static Vector2[] Bowl(float radius, float height, float wall)
        {
            float inner = Mathf.Max(radius - wall, radius * 0.5f);
            var points = new List<Vector2> { Vector2.zero, new Vector2(radius * 0.5f, 0f) };
            AppendQuadratic(points, new Vector2(radius * 0.5f, 0f), new Vector2(radius, 0f), new Vector2(radius, height), CurveSegments);
            Append(points, new Vector2(inner, height));
            AppendQuadratic(points, new Vector2(inner, height), new Vector2(inner, wall), new Vector2(inner * 0.45f, wall), CurveSegments);
            Append(points, new Vector2(0f, wall));
            return points.ToArray();
        }

        public static Vector2[] PaperTube(float outerRadius, float innerRadius, float length)
        {
            return new[]
            {
                new Vector2(innerRadius, 0f),
                new Vector2(outerRadius, 0f),
                new Vector2(outerRadius, length),
                new Vector2(innerRadius, length),
                new Vector2(innerRadius, 0f)
            };
        }

        public static Vector2[] ScrollKnob(float knobRadius, float knobLength, float collarRadius)
        {
            return new[]
            {
                new Vector2(0f, -knobLength),
                new Vector2(knobRadius * 0.45f, -knobLength),
                new Vector2(knobRadius * 0.85f, -knobLength * 0.85f),
                new Vector2(knobRadius, -knobLength * 0.6f),
                new Vector2(knobRadius * 0.85f, -knobLength * 0.35f),
                new Vector2(collarRadius, -knobLength * 0.15f),
                new Vector2(collarRadius, 0f)
            };
        }

        public static void AppendArc(List<Vector2> points, Vector2 center, float radius, float fromDegrees, float toDegrees, int segments)
        {
            int count = Mathf.Max(1, segments);
            for (int k = 0; k <= count; k++)
            {
                float angle = Mathf.Lerp(fromDegrees, toDegrees, k / (float)count) * Mathf.Deg2Rad;
                Append(points, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        public static void AppendQuadratic(List<Vector2> points, Vector2 start, Vector2 control, Vector2 end, int segments)
        {
            int count = Mathf.Max(1, segments);
            for (int k = 0; k <= count; k++)
            {
                float t = k / (float)count;
                float inverse = 1f - t;
                Append(points, inverse * inverse * start + 2f * inverse * t * control + t * t * end);
            }
        }

        public static void Append(List<Vector2> points, Vector2 point)
        {
            if (points.Count == 0 || Vector2.Distance(points[points.Count - 1], point) > DuplicateDistance)
            {
                points.Add(point);
            }
        }
    }
}
