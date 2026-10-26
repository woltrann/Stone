using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public sealed partial class TidyMeshBuilder
    {
        private const float CornerSharpnessDegrees = 40f;
        private const int MinimumRadialSegments = 3;
        private const float CapUvCenter = 0.5f;

        public void AddLathe(Vector2[] profile, int radialSegments, int subMesh = 0, Matrix4x4? transform = null, bool capBottom = true, bool capTop = true)
        {
            if (profile == null || profile.Length < 2)
            {
                return;
            }

            int segments = Mathf.Max(MinimumRadialSegments, radialSegments);
            var placement = new TidyPlacement(transform);
            TidyLatheRings rings = TidyLatheRings.Build(profile, CornerSharpnessDegrees);
            var ringStarts = new int[rings.Count];
            for (int r = 0; r < rings.Count; r++)
            {
                ringStarts[r] = EmitLatheRing(placement, rings[r], segments);
            }

            for (int s = 0; s < profile.Length - 1; s++)
            {
                StitchRings(subMesh, ringStarts[rings.OutgoingRing(s)], ringStarts[rings.IncomingRing(s + 1)], segments);
            }

            if (capBottom)
            {
                AddLatheCap(placement, subMesh, profile[0], -1f, segments);
            }

            if (capTop)
            {
                AddLatheCap(placement, subMesh, profile[profile.Length - 1], 1f, segments);
            }
        }

        public void AddCylinder(float radius, float height, int segments, int subMesh = 0, Matrix4x4? transform = null, bool capBottom = true, bool capTop = true)
        {
            var profile = new[] { new Vector2(radius, 0f), new Vector2(radius, height) };
            AddLathe(profile, segments, subMesh, transform, capBottom, capTop);
        }

        public void AddBeveledCylinder(float radius, float height, float bevel, int segments, int bevelSegments = 2, int subMesh = 0, Matrix4x4? transform = null)
        {
            AddLathe(TidyProfiles.RoundedCylinder(radius, height, bevel, bevelSegments), segments, subMesh, transform, false, false);
        }

        private int EmitLatheRing(in TidyPlacement placement, TidyLatheRing ring, int segments)
        {
            int start = positions.Count;
            for (int k = 0; k <= segments; k++)
            {
                float u = k / (float)segments;
                float angle = u * TidyMath.TwoPi;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                var point = new Vector3(ring.Point.x * cos, ring.Point.y, ring.Point.x * sin);
                var normal = new Vector3(ring.Normal.x * cos, ring.Normal.y, ring.Normal.x * sin);
                Emit(placement, point, normal, new Vector2(u, ring.V));
            }

            return start;
        }

        private void StitchRings(int subMesh, int lower, int upper, int segments)
        {
            for (int k = 0; k < segments; k++)
            {
                Quad(subMesh, lower + k, lower + k + 1, upper + k + 1, upper + k);
            }
        }

        private void AddLatheCap(in TidyPlacement placement, int subMesh, Vector2 rim, float facing, int segments)
        {
            if (rim.x < MinimumExtent)
            {
                return;
            }

            Vector3 normal = Vector3.up * facing;
            int center = Emit(placement, new Vector3(0f, rim.y, 0f), normal, new Vector2(CapUvCenter, CapUvCenter));
            int first = positions.Count;
            for (int k = 0; k < segments; k++)
            {
                float angle = k / (float)segments * TidyMath.TwoPi;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                var uv = new Vector2(CapUvCenter + cos * CapUvCenter, CapUvCenter + sin * CapUvCenter);
                Emit(placement, new Vector3(rim.x * cos, rim.y, rim.x * sin), normal, uv);
            }

            for (int k = 0; k < segments; k++)
            {
                Triangle(subMesh, center, first + k, first + (k + 1) % segments);
            }
        }
    }
}
