using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public sealed partial class TidyMeshBuilder
    {
        private const int MinimumLatitudeSegments = 2;
        private const float CrystalBaseTipRatio = 0.35f;
        private const float CrystalLowerRadiusRatio = 0.82f;
        private const float ScrollKnobRadiusRatio = 1.18f;
        private const float ScrollKnobLengthRatio = 0.6f;
        private const float ScrollRodRadiusRatio = 0.45f;
        private const float ScrollMinimumCoreRatio = 0.25f;
        private const float ScrollRodCollarRatio = 1.5f;
        private const float ScrollRodInsetRatio = 0.5f;
        private const int ScrollRadialSegments = 20;
        private const float ScrollRollAngle = -90f;

        public void AddSphere(Vector3 center, float radius, int longitude, int latitude, int subMesh = 0, Matrix4x4? transform = null)
        {
            var placement = new TidyPlacement(transform);
            int columns = Mathf.Max(MinimumRadialSegments, longitude);
            int rows = Mathf.Max(MinimumLatitudeSegments, latitude);
            int start = positions.Count;
            for (int row = 0; row <= rows; row++)
            {
                float polar = Mathf.PI * row / rows;
                float y = -Mathf.Cos(polar);
                float ring = Mathf.Sin(polar);
                for (int column = 0; column <= columns; column++)
                {
                    float u = column / (float)columns;
                    float azimuth = u * TidyMath.TwoPi;
                    var direction = new Vector3(ring * Mathf.Cos(azimuth), y, ring * Mathf.Sin(azimuth));
                    Emit(placement, center + direction * radius, direction, new Vector2(u, row / (float)rows));
                }
            }

            StitchGrid(subMesh, start, columns + 1, rows + 1);
        }

        public void AddTorus(Vector3 center, float majorRadius, float minorRadius, int majorSegments, int minorSegments, int subMesh = 0, Matrix4x4? transform = null)
        {
            var placement = new TidyPlacement(transform);
            int columns = Mathf.Max(MinimumRadialSegments, majorSegments);
            int rows = Mathf.Max(MinimumRadialSegments, minorSegments);
            int start = positions.Count;
            for (int row = 0; row <= rows; row++)
            {
                float v = row / (float)rows;
                float minorAngle = v * TidyMath.TwoPi;
                for (int column = 0; column <= columns; column++)
                {
                    float u = column / (float)columns;
                    float majorAngle = u * TidyMath.TwoPi;
                    var radial = new Vector3(Mathf.Cos(majorAngle), 0f, Mathf.Sin(majorAngle));
                    Vector3 direction = radial * Mathf.Cos(minorAngle) + Vector3.up * Mathf.Sin(minorAngle);
                    Vector3 point = center + radial * majorRadius + direction * minorRadius;
                    Emit(placement, point, direction, new Vector2(u, v));
                }
            }

            StitchGrid(subMesh, start, columns + 1, rows + 1);
        }

        public void AddCrystal(float radius, float height, int sides, float tipHeight, int subMesh = 0, Matrix4x4? transform = null)
        {
            var placement = new TidyPlacement(transform);
            int count = Mathf.Max(MinimumRadialSegments, sides);
            float lowerY = tipHeight * CrystalBaseTipRatio;
            float upperY = lowerY + height;
            float apexY = upperY + tipHeight;
            float lowerRadius = radius * CrystalLowerRadiusRatio;
            Vector3 bottom = Vector3.zero;
            var apex = new Vector3(0f, apexY, 0f);
            for (int k = 0; k < count; k++)
            {
                float u0 = k / (float)count;
                float u1 = (k + 1) / (float)count;
                Vector3 lower0 = RingPoint(lowerRadius, lowerY, u0);
                Vector3 lower1 = RingPoint(lowerRadius, lowerY, u1);
                Vector3 upper0 = RingPoint(radius, upperY, u0);
                Vector3 upper1 = RingPoint(radius, upperY, u1);
                float vLower = lowerY / apexY;
                float vUpper = upperY / apexY;
                float uMid = (u0 + u1) * 0.5f;
                AddFlatTriangle(placement, subMesh, bottom, lower0, lower1, new Vector2(uMid, 0f), new Vector2(u0, vLower), new Vector2(u1, vLower));
                AddFlatQuad(placement, subMesh, lower0, lower1, upper1, upper0, new Vector2(u0, vLower), new Vector2(u1, vLower), new Vector2(u1, vUpper), new Vector2(u0, vUpper));
                AddFlatTriangle(placement, subMesh, upper0, upper1, apex, new Vector2(u0, vUpper), new Vector2(u1, vUpper), new Vector2(uMid, 1f));
            }
        }

        public void AddRolledScroll(float length, float radius, float paperThickness, int subMesh = 0, Matrix4x4? transform = null, int knobSubMesh = -1)
        {
            int knobMesh = knobSubMesh < 0 ? subMesh : knobSubMesh;
            float knobRadius = radius * ScrollKnobRadiusRatio;
            float knobLength = radius * ScrollKnobLengthRatio;
            float coreRadius = Mathf.Max(radius - Mathf.Abs(paperThickness), radius * ScrollMinimumCoreRatio);
            float rodRadius = coreRadius * ScrollRodRadiusRatio;
            Matrix4x4 roll = (transform ?? Matrix4x4.identity) * TRS(new Vector3(-length * 0.5f, knobRadius, 0f), new Vector3(0f, 0f, ScrollRollAngle), Vector3.one);
            AddLathe(TidyProfiles.PaperTube(radius, coreRadius, length), ScrollRadialSegments, subMesh, roll, false, false);
            float rodOverhang = knobLength * ScrollRodInsetRatio;
            AddLathe(new[] { new Vector2(rodRadius, -rodOverhang), new Vector2(rodRadius, length + rodOverhang) }, ScrollRadialSegments, knobMesh, roll);
            Vector2[] knob = TidyProfiles.ScrollKnob(knobRadius, knobLength, rodRadius * ScrollRodCollarRatio);
            AddLathe(knob, ScrollRadialSegments, knobMesh, roll);
            Matrix4x4 farEnd = roll * Matrix4x4.TRS(new Vector3(0f, length, 0f), Quaternion.identity, new Vector3(1f, -1f, 1f));
            AddLathe(knob, ScrollRadialSegments, knobMesh, farEnd);
        }

        private void AddFlatTriangle(in TidyPlacement placement, int subMesh, Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            Vector3 normal = OutwardFacetNormal(Vector3.Cross(b - a, c - a), (a + b + c) / 3f);
            int ia = Emit(placement, a, normal, uvA);
            int ib = Emit(placement, b, normal, uvB);
            int ic = Emit(placement, c, normal, uvC);
            Triangle(subMesh, ia, ib, ic);
        }

        private void AddFlatQuad(in TidyPlacement placement, int subMesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            Vector3 normal = OutwardFacetNormal(Vector3.Cross(b - a, c - a) + Vector3.Cross(c - a, d - a), (a + b + c + d) * 0.25f);
            int ia = Emit(placement, a, normal, uvA);
            int ib = Emit(placement, b, normal, uvB);
            int ic = Emit(placement, c, normal, uvC);
            int id = Emit(placement, d, normal, uvD);
            Quad(subMesh, ia, ib, ic, id);
        }

        private static Vector3 OutwardFacetNormal(Vector3 geometricNormal, Vector3 centroid)
        {
            Vector3 normal = SafeNormalize(geometricNormal, Vector3.up);
            var outward = new Vector3(centroid.x, 0f, centroid.z);
            return Vector3.Dot(normal, outward) < 0f ? -normal : normal;
        }

        private static Vector3 RingPoint(float radius, float height, float turn)
        {
            float angle = turn * TidyMath.TwoPi;
            return new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        }
    }
}
