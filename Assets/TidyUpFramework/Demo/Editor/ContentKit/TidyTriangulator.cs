using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static class TidyTriangulator
    {
        private const float ConvexityEpsilon = 1e-9f;

        public static List<int> Triangulate(IReadOnlyList<Vector2> polygon)
        {
            var result = new List<int>();
            if (polygon == null || polygon.Count < 3)
            {
                return result;
            }

            List<int> remaining = InitialOrder(polygon);
            while (remaining.Count > 3)
            {
                int ear = FindEar(polygon, remaining);
                ClipEar(remaining, ear < 0 ? 0 : ear, result);
            }

            result.Add(remaining[0]);
            result.Add(remaining[1]);
            result.Add(remaining[2]);
            return result;
        }

        private static List<int> InitialOrder(IReadOnlyList<Vector2> polygon)
        {
            var order = new List<int>(polygon.Count);
            for (int i = 0; i < polygon.Count; i++)
            {
                order.Add(i);
            }

            if (TidyPolygon.SignedArea(polygon) < 0f)
            {
                order.Reverse();
            }

            return order;
        }

        private static void ClipEar(List<int> remaining, int position, List<int> result)
        {
            int count = remaining.Count;
            result.Add(remaining[(position + count - 1) % count]);
            result.Add(remaining[position]);
            result.Add(remaining[(position + 1) % count]);
            remaining.RemoveAt(position);
        }

        private static int FindEar(IReadOnlyList<Vector2> polygon, List<int> remaining)
        {
            int count = remaining.Count;
            for (int i = 0; i < count; i++)
            {
                int previous = remaining[(i + count - 1) % count];
                int current = remaining[i];
                int next = remaining[(i + 1) % count];
                if (IsEar(polygon, remaining, previous, current, next))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsEar(IReadOnlyList<Vector2> polygon, List<int> remaining, int previous, int current, int next)
        {
            Vector2 a = polygon[previous];
            Vector2 b = polygon[current];
            Vector2 c = polygon[next];
            if (Cross(b - a, c - b) <= ConvexityEpsilon)
            {
                return false;
            }

            foreach (int index in remaining)
            {
                if (index != previous && index != current && index != next && InsideTriangle(polygon[index], a, b, c))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool InsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            return Cross(b - a, point - a) >= 0f && Cross(c - b, point - b) >= 0f && Cross(a - c, point - c) >= 0f;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}
