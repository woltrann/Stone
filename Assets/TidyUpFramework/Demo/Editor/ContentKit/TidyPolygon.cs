using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static class TidyPolygon
    {
        private const float DuplicateDistance = 1e-6f;
        private const float MinimumMiterDot = 0.25f;

        public static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        public static Vector2[] Prepare(Vector2[] outline)
        {
            if (outline == null)
            {
                return new Vector2[0];
            }

            var points = new List<Vector2>(outline.Length);
            foreach (Vector2 point in outline)
            {
                if (points.Count == 0 || Vector2.Distance(points[points.Count - 1], point) > DuplicateDistance)
                {
                    points.Add(point);
                }
            }

            if (points.Count > 1 && Vector2.Distance(points[0], points[points.Count - 1]) <= DuplicateDistance)
            {
                points.RemoveAt(points.Count - 1);
            }

            if (SignedArea(points) < 0f)
            {
                points.Reverse();
            }

            return points.ToArray();
        }

        public static Vector3 OutwardEdgeNormal(Vector2 from, Vector2 to)
        {
            Vector2 direction = (to - from).normalized;
            return new Vector3(direction.y, -direction.x, 0f);
        }

        public static Vector2[] Inset(Vector2[] counterClockwise, float distance)
        {
            int count = counterClockwise.Length;
            var result = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                Vector2 previous = counterClockwise[(i + count - 1) % count];
                Vector2 current = counterClockwise[i];
                Vector2 next = counterClockwise[(i + 1) % count];
                Vector2 inwardA = InwardNormal(previous, current);
                Vector2 inwardB = InwardNormal(current, next);
                Vector2 bisector = (inwardA + inwardB).normalized;
                if (bisector.sqrMagnitude < DuplicateDistance)
                {
                    bisector = inwardA;
                }

                float miter = distance / Mathf.Max(Vector2.Dot(bisector, inwardA), MinimumMiterDot);
                result[i] = current + bisector * miter;
            }

            return result;
        }

        private static Vector2 InwardNormal(Vector2 from, Vector2 to)
        {
            Vector2 direction = (to - from).normalized;
            return new Vector2(-direction.y, direction.x);
        }
    }
}
