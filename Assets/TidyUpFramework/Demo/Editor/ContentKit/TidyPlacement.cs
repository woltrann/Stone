using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    internal readonly struct TidyPlacement
    {
        private const float DegenerateLengthSquared = 1e-12f;

        public TidyPlacement(Matrix4x4? transform)
        {
            Matrix = transform ?? Matrix4x4.identity;
            NormalMatrix = Matrix.inverse.transpose;
        }

        public Matrix4x4 Matrix { get; }

        public Matrix4x4 NormalMatrix { get; }

        public Vector3 Point(Vector3 localPoint)
        {
            return Matrix.MultiplyPoint3x4(localPoint);
        }

        public Vector3 Normal(Vector3 localNormal)
        {
            Vector3 transformed = NormalMatrix.MultiplyVector(localNormal);
            return transformed.sqrMagnitude > DegenerateLengthSquared ? transformed.normalized : localNormal;
        }
    }
}
