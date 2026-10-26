using System.Collections.Generic;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TidyUpFramework.Interaction
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Tidy Up Framework/Interaction/Placement Preview")]
    public sealed class TidyPlacementPreview : MonoBehaviour
    {
        const string GhostShaderName = "TidyUpFramework/Ghost";
        static readonly int GhostColorId = Shader.PropertyToID("_GhostColor");

        [SerializeField]
        Shader ghostShader;

        [SerializeField]
        Color correctColor = new Color(0.35f, 1f, 0.55f, 0.28f);

        [SerializeField]
        Color acceptedIncorrectColor = new Color(1f, 0.8f, 0.3f, 0.28f);

        [SerializeField]
        Color rejectedColor = new Color(1f, 0.3f, 0.25f, 0.28f);

        [SerializeField, Tooltip("Shows whether a placement is correct before the player commits. Disable for harder puzzle games.")]
        bool revealCorrectness = true;

        [SerializeField, Min(0f), Tooltip("How quickly the ghost glides between candidate poses.")]
        float followSharpness = 30f;

        struct MeshPart
        {
            public Mesh Mesh;
            public Matrix4x4 LocalMatrix;
        }

        readonly List<MeshPart> parts = new List<MeshPart>();
        TidyObject sourceObject;
        Material correctMaterial;
        Material incorrectMaterial;
        Material rejectedMaterial;
        bool isVisible;
        Vector3 shownPosition;
        Quaternion shownRotation;
        Vector3 targetPosition;
        Quaternion targetRotation;
        Material activeMaterial;

        public bool RevealCorrectness
        {
            get => revealCorrectness;
            set => revealCorrectness = value;
        }

        public void Show(TidyPlacementCandidate candidate)
        {
            if (candidate.Object == null || !candidate.HasPose)
            {
                Hide();
                return;
            }

            EnsureMaterials();
            if (sourceObject != candidate.Object)
            {
                CaptureParts(candidate.Object);
            }

            activeMaterial = SelectMaterial(candidate);
            targetPosition = candidate.Position;
            targetRotation = candidate.Rotation;
            if (!isVisible)
            {
                shownPosition = targetPosition;
                shownRotation = targetRotation;
            }

            isVisible = true;
        }

        public void Hide()
        {
            isVisible = false;
        }

        Material SelectMaterial(TidyPlacementCandidate candidate)
        {
            if (!candidate.IsValid)
            {
                return revealCorrectness ? rejectedMaterial : correctMaterial;
            }

            if (!revealCorrectness)
            {
                return correctMaterial;
            }

            return candidate.IsCorrect ? correctMaterial : incorrectMaterial;
        }

        void CaptureParts(TidyObject tidyObject)
        {
            sourceObject = tidyObject;
            parts.Clear();
            var rootInverse = Matrix4x4.TRS(tidyObject.transform.position, tidyObject.transform.rotation, Vector3.one).inverse;
            foreach (var renderer in tidyObject.Renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                parts.Add(new MeshPart { Mesh = meshFilter.sharedMesh, LocalMatrix = rootInverse * renderer.localToWorldMatrix });
            }
        }

        void EnsureMaterials()
        {
            if (correctMaterial != null)
            {
                return;
            }

            var shader = ghostShader != null ? ghostShader : Shader.Find(GhostShaderName);
            if (shader == null)
            {
                Debug.LogWarning("Tidy Up Framework: ghost shader not found. Placement previews are disabled.", this);
                enabled = false;
                return;
            }

            correctMaterial = CreateMaterial(shader, correctColor);
            incorrectMaterial = CreateMaterial(shader, acceptedIncorrectColor);
            rejectedMaterial = CreateMaterial(shader, rejectedColor);
        }

        static Material CreateMaterial(Shader shader, Color color)
        {
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material.SetColor(GhostColorId, color);
            return material;
        }

        void LateUpdate()
        {
            if (!isVisible || activeMaterial == null || sourceObject == null)
            {
                return;
            }

            var blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            shownPosition = Vector3.Lerp(shownPosition, targetPosition, blend);
            shownRotation = Quaternion.Slerp(shownRotation, targetRotation, blend);
            var rootMatrix = Matrix4x4.TRS(shownPosition, shownRotation, Vector3.one);
            var renderParams = new RenderParams(activeMaterial) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, layer = gameObject.layer };
            foreach (var part in parts)
            {
                var matrix = rootMatrix * part.LocalMatrix;
                for (var subMesh = 0; subMesh < part.Mesh.subMeshCount; subMesh++)
                {
                    Graphics.RenderMesh(renderParams, part.Mesh, subMesh, matrix);
                }
            }
        }

        void OnDestroy()
        {
            DestroyMaterial(correctMaterial);
            DestroyMaterial(incorrectMaterial);
            DestroyMaterial(rejectedMaterial);
        }

        static void DestroyMaterial(Material material)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
