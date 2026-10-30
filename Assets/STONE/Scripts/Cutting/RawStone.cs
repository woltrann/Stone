using System;
using TidyUpFramework.Objects;
using TidyUpFramework.Save;
using UnityEngine;

namespace StoneGame.Cutting
{
    [DisallowMultipleComponent, RequireComponent(typeof(TidyObject))]
    public sealed class RawStone : MonoBehaviour, ITidyObjectSnapshotParticipant, ITidyObjectSnapshotRecovery
    {
        [SerializeField] StoneCutProfile cutProfile;
        [SerializeField, HideInInspector] bool hasRolled;
        [SerializeField, HideInInspector] OreDefinition rolledOre;
        bool processed;
        CutStoneHalf leftHalf, rightHalf;

        public StoneCutProfile CutProfile => cutProfile;
        public bool IsProcessed => processed;
        public bool RestoreFailed { get; private set; }

        public bool TryGetOre(out OreDefinition ore)
        {
            ore = null;
            if (cutProfile == null)
            {
                Debug.LogError($"RawStone on '{name}' needs a Stone Cut Profile.", this);
                return false;
            }
            if (!hasRolled)
            {
                if (!cutProfile.TryRollOre(out rolledOre)) return false;
                hasRolled = true;
            }
            ore = rolledOre;
            return true;
        }

        internal void CompleteCut(CutStoneHalf left, CutStoneHalf right)
        {
            leftHalf = left;
            rightHalf = right;
            processed = true;
            var tidy = GetComponent<TidyObject>();
            if (tidy.CurrentTarget != null) tidy.CurrentTarget.Release(tidy);
            tidy.MarkConsumed();
            // Retain an invisible source record for the existing level snapshot system.
            transform.SetParent(null, true);
            gameObject.SetActive(false);
        }

        [Serializable]
        sealed class SavedState
        {
            public bool rolled, processed;
            public string oreName;
            public Vector3 leftPosition, rightPosition, leftScale, rightScale;
            public Quaternion leftRotation, rightRotation;
            public StoneMeshSnapshot leftMesh, rightMesh;
            public int meshVersion;
            public StoneMeshCutSettings meshSettings;
        }

        public string CaptureObjectState()
        {
            var data = new SavedState { rolled = hasRolled, processed = processed, oreName = rolledOre != null ? rolledOre.name : null };
            if (leftHalf != null)
            {
                data.leftPosition = leftHalf.transform.position;
                data.leftRotation = leftHalf.transform.rotation;
                data.leftScale = leftHalf.transform.lossyScale;
                data.leftMesh = StoneMeshSnapshot.Capture(leftHalf);
            }
            if (rightHalf != null)
            {
                data.rightPosition = rightHalf.transform.position;
                data.rightRotation = rightHalf.transform.rotation;
                data.rightScale = rightHalf.transform.lossyScale;
                data.rightMesh = StoneMeshSnapshot.Capture(rightHalf);
            }
            if (data.leftMesh != null)
            {
                data.meshVersion = 1;
                data.meshSettings = FindAnyObjectByType<StoneSawMachine>()?.MeshSettings ?? new StoneMeshCutSettings();
            }
            return JsonUtility.ToJson(data);
        }

        public void RestoreObjectState(string state)
        {
            foreach (var saw in FindObjectsByType<StoneSawMachine>()) saw.CancelCut(this);
            RemoveHalf(leftHalf);
            RemoveHalf(rightHalf);
            leftHalf = rightHalf = null;
            processed = false;
            hasRolled = false;
            rolledOre = null;
            RestoreFailed = false;
            if (string.IsNullOrEmpty(state)) return; // Backward compatible with old saves.
            SavedState data;
            try { data = JsonUtility.FromJson<SavedState>(state); }
            catch (Exception exception)
            {
                RestoreFailed = true;
                Debug.LogWarning($"Invalid stone snapshot on '{name}': {exception.Message}. Source retained.", this);
                return;
            }
            if (data == null) { RestoreFailed = true; return; }
            hasRolled = data.rolled;
            if (!string.IsNullOrEmpty(data.oreName) && cutProfile != null)
            {
                foreach (var entry in cutProfile.OreChances)
                    if (entry?.ore != null && entry.ore.name == data.oreName) { rolledOre = entry.ore; break; }
                if (rolledOre == null) Debug.LogWarning($"Saved ore '{data.oreName}' is no longer in profile '{cutProfile.name}'.", this);
            }
            if (!data.processed) return;
            // A previously rolled result must not depend on today's probability distribution.
            if (cutProfile == null)
            {
                RestoreFailed = true;
                Debug.LogWarning($"Cannot restore cut stone '{name}': assign its StoneCutProfile. Source retained.", this);
                return;
            }
            var original = GetComponent<TidyObject>();
            try
            {
                if (data.meshVersion >= 1)
                {
                    if (data.leftMesh == null || !data.leftMesh.HasGeometry || data.rightMesh == null || !data.rightMesh.HasGeometry)
                        throw new InvalidOperationException("Saved runtime geometry is incomplete.");
                    var settings = data.meshSettings ?? new StoneMeshCutSettings();
                    float total = data.leftMesh.volume + data.rightMesh.volume;
                    leftHalf = RestoreGenerated(data.leftMesh, data.leftPosition, data.leftRotation, original, "left", settings, data.leftMesh.volume / total);
                    rightHalf = RestoreGenerated(data.rightMesh, data.rightPosition, data.rightRotation, original, "right", settings, data.rightMesh.volume / total);
                    leftHalf.transform.localScale = data.leftScale;
                    rightHalf.transform.localScale = data.rightScale;
                }
                else
                {
                    // Old prefab-based snapshots remain loadable; new cuts never take this path.
                    leftHalf = StoneSawMachine.CreateHalf(cutProfile.LeftHalfPrefab, data.leftPosition, data.leftRotation, data.leftScale, original, cutProfile, "left", rolledOre);
                    rightHalf = StoneSawMachine.CreateHalf(cutProfile.RightHalfPrefab, data.rightPosition, data.rightRotation, data.rightScale, original, cutProfile, "right", rolledOre);
                }
                processed = true;
            }
            catch (Exception exception)
            {
                RemoveHalf(leftHalf);
                RemoveHalf(rightHalf);
                leftHalf = rightHalf = null;
                RestoreFailed = true;
                Debug.LogWarning($"Saved cut on '{name}' could not be rebuilt; source retained. {exception.Message}", this);
            }
        }

        CutStoneHalf RestoreGenerated(StoneMeshSnapshot data, Vector3 position, Quaternion rotation,
            TidyObject original, string suffix, StoneMeshCutSettings settings, float massFraction)
        {
            var sourceMaterials = StoneRuntimeMeshSlicer.SourceMaterials(original);
            var materials = new Material[data.submeshes.Length];
            var saw = FindAnyObjectByType<StoneSawMachine>();
            // Version-1 generated saves used RockMeshSlicer's cap=input.subMeshCount contract.
            // New saves carry the explicit slot and stable material asset identity.
            int surfaceSlot=data.surfaceMetadataVersion>=1?data.cutSurfaceSubmesh:data.submeshes.Length-1;
            var savedSurface=data.surfaceMetadataVersion>=1?StoneCutMaterialReferences.Resolve(data.cutSurfaceMaterialId):null;
            for (int i=0; i<materials.Length; i++)
            {
                string savedName = data.materialNames != null && i < data.materialNames.Length ? data.materialNames[i] : null;
                if(i==surfaceSlot && data.surfaceMetadataVersion>=1){materials[i]=savedSurface;continue;}
                if (i < sourceMaterials.Length && sourceMaterials[i] != null && sourceMaterials[i].name == savedName)
                    materials[i] = sourceMaterials[i];
                else
                {
                    foreach (var material in sourceMaterials)
                        if (material != null && material.name == savedName) { materials[i] = material; break; }
                    if (materials[i] == null && saw != null && saw.CutSurfaceMaterial != null && saw.CutSurfaceMaterial.name == savedName)
                        materials[i] = saw.CutSurfaceMaterial;
                    if(materials[i]==null)materials[i]=StoneCutMaterialReferences.ResolveLegacyName(savedName);
                    if (materials[i] == null && i!=surfaceSlot) materials[i] = sourceMaterials.Length > 0 ? sourceMaterials[0] : null;
                }
            }
            var mesh = data.ToMesh();
            try
            {
                return StoneCutPieceFactory.Create(mesh, materials, data.surfaceCenter, data.surfaceNormal, data.surfaceRadius,
                    data.volume, position, rotation, original, cutProfile, suffix, rolledOre, settings, massFraction,surfaceSlot);
            }
            catch { StoneMeshCutResult.DestroyMesh(mesh); throw; }
        }

        static void RemoveHalf(CutStoneHalf half)
        {
            if (half == null) return;
            var tidy = half.GetComponent<TidyObject>();
            if (tidy.CurrentTarget != null) tidy.CurrentTarget.Release(tidy);
            TidyObjectRegistry.Unregister(tidy);
            half.gameObject.SetActive(false);
            Destroy(half.gameObject);
        }
    }
}
