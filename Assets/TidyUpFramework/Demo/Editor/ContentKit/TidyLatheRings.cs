using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal readonly struct TidyLatheRing
    {
        public TidyLatheRing(Vector2 point, Vector2 normal, float v)
        {
            Point = point;
            Normal = normal;
            V = v;
        }

        public Vector2 Point { get; }

        public Vector2 Normal { get; }

        public float V { get; }
    }

    internal sealed class TidyLatheRings
    {
        private const float DegenerateLength = 1e-6f;

        private readonly List<TidyLatheRing> rings = new List<TidyLatheRing>();
        private readonly int[] incoming;
        private readonly int[] outgoing;

        private TidyLatheRings(int pointCount)
        {
            incoming = new int[pointCount];
            outgoing = new int[pointCount];
        }

        public int Count => rings.Count;

        public TidyLatheRing this[int index] => rings[index];

        public int IncomingRing(int pointIndex)
        {
            return incoming[pointIndex];
        }

        public int OutgoingRing(int pointIndex)
        {
            return outgoing[pointIndex];
        }

        public static TidyLatheRings Build(Vector2[] profile, float sharpCornerDegrees)
        {
            var result = new TidyLatheRings(profile.Length);
            Vector2[] segmentNormals = SegmentNormals(profile);
            float[] vCoordinates = NormalizedLengths(profile);
            int last = profile.Length - 1;
            for (int i = 0; i <= last; i++)
            {
                Vector2 previous = segmentNormals[Mathf.Max(0, i - 1)];
                Vector2 next = segmentNormals[Mathf.Min(last - 1, i)];
                bool isEndPoint = i == 0 || i == last;
                bool isSharp = !isEndPoint && Vector2.Angle(previous, next) > sharpCornerDegrees;
                if (isSharp)
                {
                    result.incoming[i] = result.Add(profile[i], previous, vCoordinates[i]);
                    result.outgoing[i] = result.Add(profile[i], next, vCoordinates[i]);
                    continue;
                }

                Vector2 normal = i == 0 ? next : (i == last ? previous : (previous + next).normalized);
                int ring = result.Add(profile[i], normal, vCoordinates[i]);
                result.incoming[i] = ring;
                result.outgoing[i] = ring;
            }

            return result;
        }

        private int Add(Vector2 point, Vector2 normal, float v)
        {
            rings.Add(new TidyLatheRing(point, normal, v));
            return rings.Count - 1;
        }

        private static Vector2[] SegmentNormals(Vector2[] profile)
        {
            var result = new Vector2[profile.Length - 1];
            Vector2 fallback = Vector2.right;
            for (int s = 0; s < result.Length; s++)
            {
                Vector2 delta = profile[s + 1] - profile[s];
                if (delta.magnitude < DegenerateLength)
                {
                    result[s] = fallback;
                    continue;
                }

                result[s] = new Vector2(delta.y, -delta.x).normalized;
                fallback = result[s];
            }

            return result;
        }

        private static float[] NormalizedLengths(Vector2[] profile)
        {
            var result = new float[profile.Length];
            for (int i = 1; i < profile.Length; i++)
            {
                result[i] = result[i - 1] + Vector2.Distance(profile[i - 1], profile[i]);
            }

            float total = result[profile.Length - 1];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = total > DegenerateLength ? result[i] / total : i / (float)(profile.Length - 1);
            }

            return result;
        }
    }
}
