using TidyUpFramework.Objects;
using UnityEngine;

namespace StoneGame.Cutting
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TidyObject))]
    public sealed partial class CutStoneHalf : MonoBehaviour
    {
        [SerializeField, Tooltip("Child transform located at the cut face, where ore becomes visible.")]
        Transform oreSocket;

        public OreDefinition ContainedOre { get; private set; }
        public bool ContainsOre => ContainedOre != null;
        public bool HasOreSocket => oreSocket != null;
        public string SourceStoneId { get; private set; }
        public StoneCutProfile SourceProfile { get; private set; }
        GameObject oreVisual;
        Mesh ownedMesh;
        TidyPhysicsProfile ownedPhysics;
        public Mesh GeneratedMesh => ownedMesh;
        public Vector3 CutSurfaceCenter { get; private set; }
        public Vector3 CutSurfaceNormal { get; private set; }
        public float CutSurfaceRadius { get; private set; }
        [SerializeField,HideInInspector] int cutSurfaceSubmesh=-1;
        [SerializeField,HideInInspector] Material cutSurfaceMaterial;
        [SerializeField,HideInInspector] string savedCutSurfaceMaterialId;
        public int CutSurfaceSubmesh => cutSurfaceSubmesh;
        public Material CutSurfaceMaterial => cutSurfaceMaterial;
        public string CutSurfaceMaterialId => StoneCutMaterialReferences.IdFor(cutSurfaceMaterial)??savedCutSurfaceMaterialId;
        public float Volume { get; private set; }

        public void InitializeGenerated(Mesh mesh, TidyPhysicsProfile physics, Vector3 center, Vector3 normal,
            float radius, float volume, OreDefinition ore, string sourceId, StoneCutProfile profile, StoneMeshCutSettings settings,int surfaceSubmesh=-1)
        {
            ownedMesh = mesh; ownedPhysics = physics;
            cutSurfaceSubmesh=surfaceSubmesh;
            var renderer=GetComponent<MeshRenderer>();var materials=renderer!=null?renderer.sharedMaterials:null;
            cutSurfaceMaterial=mesh!=null && surfaceSubmesh>=0 && surfaceSubmesh<mesh.subMeshCount && materials!=null && surfaceSubmesh<materials.Length?materials[surfaceSubmesh]:null;
            savedCutSurfaceMaterialId=StoneCutMaterialReferences.IdFor(cutSurfaceMaterial);
            CutSurfaceCenter = center; CutSurfaceNormal = normal; CutSurfaceRadius = radius; Volume = volume;
            oreSocket = new GameObject("GeneratedCutSurface").transform;
            oreSocket.SetParent(transform, false);
            var rotation = Quaternion.FromToRotation(Vector3.up, normal);
            var offset = Vector2.ClampMagnitude(settings.oreSurfaceOffset, radius * .25f);
            oreSocket.localPosition = center + rotation * new Vector3(offset.x, settings.oreNormalOffset, offset.y);
            oreSocket.localRotation = rotation * Quaternion.Euler(settings.oreEulerOffset);
            Initialize(ore, sourceId, profile);
            if (oreVisual == null) return;
            // Bounds in the socket's coordinates account for prefab pivot, child transforms and authored scale.
            var bounds = TidyUpFramework.Core.TidyBoundsUtility.CalculateLocalBounds(oreSocket);
            float projectedRadius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
            float scale = (radius - offset.magnitude) * settings.oreRadiusFraction / Mathf.Max(projectedRadius, 1e-6f);
            oreVisual.transform.localScale *= scale;
            oreVisual.transform.localPosition -= bounds.center * scale;
        }

        void OnDestroy()
        {
            // Placement/drop feedback may still be animating a piece when a snapshot replaces it.
            TidyUpFramework.Core.TidyTweenRunner.Kill(transform);
            StoneMeshCutResult.DestroyMesh(ownedMesh);
            StoneMeshCutResult.DestroyMesh(ownedPhysics);
        }

        public void Initialize(OreDefinition ore, string sourceStoneId = null, StoneCutProfile sourceProfile = null)
        {
            if (oreVisual != null)
            {
                oreVisual.SetActive(false);
                Destroy(oreVisual);
            }
            SourceStoneId = sourceStoneId;
            SourceProfile = sourceProfile;
            ContainedOre = ore;
            ResetChipping();
            if (ore == null)
                return;

            if (oreSocket == null || ore.RawOreVisualPrefab == null)
            {
                Debug.LogWarning($"'{name}' contains {ore.DisplayName}, but the Ore Socket or ore visual prefab is missing.", this);
                return;
            }

            var visual = Instantiate(ore.RawOreVisualPrefab, oreSocket);
            oreVisual = visual;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            // Keep the artist-authored prefab scale. OreSocket controls the final size/pose.
            foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            // The ore is currently only a visual part of this cut half.
            // A later polishing system will operate on this same object.
        }
    }
}
