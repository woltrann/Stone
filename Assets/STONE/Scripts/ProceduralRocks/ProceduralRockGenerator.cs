using System;
using UnityEngine;

namespace ProceduralRocks
{
    public enum RockShape
    {
        Natural,
        Jagged,
        Fractured,
        Slab
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralRockGenerator : MonoBehaviour
    {
        [Header("Source (do not use a generated mesh as the source)")]
        [SerializeField] private Mesh sourceMesh;
        [SerializeField] private Material[] sourceMaterials = Array.Empty<Material>();
        [SerializeField] private Material fractureMaterial;

        [Header("Shape")]
        [SerializeField] private RockShape shape = RockShape.Natural;
        [SerializeField, Range(0f, 1f)] private float roughness = 0.45f;
        [SerializeField, Range(0f, 1f)] private float dents = 0.35f;
        [SerializeField, Range(0f, 1f)] private float spikes = 0.25f;
        [SerializeField, Range(0f, 1f)] private float cuts = 0.40f;
        [SerializeField] private int seed = 1234;

        [Header("Preview / Runtime")]
        [SerializeField] private bool autoPreviewInEditor = true;
        [SerializeField] private bool generateOnPlay = false;
        [SerializeField, HideInInspector] private Mesh savedMesh;

        [NonSerialized] private Mesh temporaryMesh;
        [NonSerialized] private string lastMessage;
        [NonSerialized] private int completedCuts;

        public Mesh SourceMesh => sourceMesh;
        public Mesh SavedMesh => savedMesh;
        public bool AutoPreviewInEditor => autoPreviewInEditor;
        public string LastMessage => lastMessage;
        public int CompletedCuts => completedCuts;

        private void Reset()
        {
            sourceMesh = GetComponent<MeshFilter>().sharedMesh;
            sourceMaterials = GetComponent<MeshRenderer>().sharedMaterials;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && autoPreviewInEditor && sourceMesh != null && savedMesh == null)
                GeneratePreview();
#endif
        }

        private void Start()
        {
            if (Application.isPlaying && generateOnPlay)
                GeneratePreview();
        }

        public bool GeneratePreview()
        {
            if (sourceMesh == null)
            {
                lastMessage = "Assign a Source Mesh first.";
                return false;
            }

            if (!sourceMesh.isReadable)
            {
                lastMessage = "Source Mesh is not readable. Enable Read/Write in model Import Settings.";
                return false;
            }

            Mesh next;
            try
            {
                var options = new RockMeshBuilder.Options
                {
                    Shape = shape,
                    Roughness = roughness,
                    Dents = dents,
                    Spikes = spikes,
                    Cuts = cuts,
                    Seed = seed
                };

                next = RockMeshBuilder.Build(sourceMesh, options, out completedCuts, out lastMessage);
            }
            catch (Exception exception)
            {
                lastMessage = "Generation failed: " + exception.Message;
                Debug.LogException(exception, this);
                return false;
            }

            next.name = sourceMesh.name + "_Rock_" + seed;
            next.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            var filter = GetComponent<MeshFilter>();
            var collider = GetComponent<MeshCollider>();
            Mesh previous = temporaryMesh;
            temporaryMesh = next;
            filter.sharedMesh = temporaryMesh;
            ApplyMaterials();

            if (collider != null)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = temporaryMesh;
            }

            DestroyGeneratedMesh(previous);
            return true;
        }

        public void AssignSavedMesh(Mesh asset)
        {
            if (asset == null)
                return;

            savedMesh = asset;
            GetComponent<MeshFilter>().sharedMesh = savedMesh;
            ApplyMaterials();

            var collider = GetComponent<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = savedMesh;
            }

            Mesh old = temporaryMesh;
            temporaryMesh = null;
            DestroyGeneratedMesh(old);
            lastMessage = "Saved mesh assigned: " + asset.name;
        }

        public void RestoreSource()
        {
            GetComponent<MeshFilter>().sharedMesh = sourceMesh;
            savedMesh = null;
            var collider = GetComponent<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = sourceMesh;
            }

            Mesh old = temporaryMesh;
            temporaryMesh = null;
            DestroyGeneratedMesh(old);
            ApplyMaterials();
            lastMessage = "Original source restored.";
        }

        private void ApplyMaterials()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh == null)
                return;

            int count = filter.sharedMesh.subMeshCount;
            var materials = new Material[count];
            for (int i = 0; i < count; i++)
            {
                if (sourceMesh != null && i >= sourceMesh.subMeshCount)
                    materials[i] = fractureMaterial != null ? fractureMaterial : FallbackMaterial();
                else
                    materials[i] = i < sourceMaterials.Length ? sourceMaterials[i] : FallbackMaterial();
            }
            GetComponent<MeshRenderer>().sharedMaterials = materials;
        }

        private Material FallbackMaterial()
        {
            return sourceMaterials.Length > 0 ? sourceMaterials[0] : null;
        }

        private void OnDisable()
        {
            if (temporaryMesh == null)
                return;

            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh == temporaryMesh)
                filter.sharedMesh = savedMesh != null ? savedMesh : sourceMesh;

            var collider = GetComponent<MeshCollider>();
            if (collider != null && collider.sharedMesh == temporaryMesh)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = savedMesh != null ? savedMesh : sourceMesh;
            }

            Mesh old = temporaryMesh;
            temporaryMesh = null;
            DestroyGeneratedMesh(old);
        }

        private static void DestroyGeneratedMesh(Mesh mesh)
        {
            if (mesh == null)
                return;
            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }
    }
}
