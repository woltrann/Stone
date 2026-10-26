using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static class TidySmoothNormalBaker
    {
        private const float WeldTolerance = 1e-4f;
        private const float QuantizeScale = 1f / WeldTolerance;
        private const float DegenerateLengthSquared = 1e-12f;

        public static List<Vector3> Bake(IReadOnlyList<Vector3> positions, IReadOnlyList<Vector3> normals)
        {
            int count = positions.Count;
            var sums = new Dictionary<Vector3Int, Vector3>(count);
            var keys = new Vector3Int[count];
            for (int i = 0; i < count; i++)
            {
                Vector3Int key = Quantize(positions[i]);
                keys[i] = key;
                sums.TryGetValue(key, out Vector3 sum);
                sums[key] = sum + normals[i];
            }

            var result = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                Vector3 sum = sums[keys[i]];
                result.Add(sum.sqrMagnitude > DegenerateLengthSquared ? sum.normalized : normals[i]);
            }

            return result;
        }

        private static Vector3Int Quantize(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x * QuantizeScale),
                Mathf.RoundToInt(position.y * QuantizeScale),
                Mathf.RoundToInt(position.z * QuantizeScale));
        }
    }
}
