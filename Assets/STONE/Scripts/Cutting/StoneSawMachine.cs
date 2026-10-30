using System;
using System.Collections;
using TidyUpFramework.Core;
using TidyUpFramework.Interaction;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace StoneGame.Cutting
{
    [DisallowMultipleComponent]
    public sealed class StoneSawMachine : MonoBehaviour
    {
        [Header("Existing Tidy Up placement system")]
        [SerializeField] TidySnapSlot cuttingSlot;
        [Header("Interaction")]
        [SerializeField] Transform player;
        [SerializeField] TidyInteractor interactor;
        [SerializeField] Key cuttingKey = Key.T;
        [SerializeField, Min(0.1f)] float interactionDistance = 2.5f;
        [Header("Saw visual animation (optional)")]
        [SerializeField] Transform sawBlade;
        [SerializeField] Vector3 bladeLocalCutOffset = new Vector3(0f, -0.4f, 0f);
        [SerializeField, Min(0.1f)] float cutDuration = 2f;
        [SerializeField] float bladeSpinDegreesPerSecond = 900f;
        [Header("Two finished pieces (surface points)")]
        [SerializeField] Transform leftOutputPoint;
        [SerializeField] Transform rightOutputPoint;
        [Header("Runtime mesh slicing")]
        [SerializeField, Tooltip("Local X/right axis is the cutting plane normal; local YZ is the blade surface.")]
        Transform cuttingPlane;
        [SerializeField] Material cutSurfaceMaterial;
        [SerializeField] StoneMeshCutSettings meshSettings = new StoneMeshCutSettings();
        public Material CutSurfaceMaterial => cutSurfaceMaterial;
        public StoneMeshCutSettings MeshSettings => meshSettings;
        public string LastCutError { get; private set; }
        public double LastCutMilliseconds { get; private set; }
        [Header("Future VFX / audio hooks")]
        [SerializeField] UnityEvent onCutStarted = new UnityEvent();
        [SerializeField] UnityEvent onCutCompleted = new UnityEvent();
        [SerializeField] UnityEvent onCutCancelled = new UnityEvent();
        [SerializeField] TMPro.TMP_Text statusLabel;

        Coroutine routine;
        RawStone activeStone;
        Vector3 bladeStart;
        Quaternion bladeRotation;
        public bool IsCutting { get; private set; }
        public float Progress { get; private set; }
        public bool IsReady => !IsCutting && cuttingSlot != null && cuttingSlot.Occupants.Count == 1 &&
            cuttingSlot.Occupants[0] != null && cuttingSlot.Occupants[0].State == TidyObjectState.Placed &&
            cuttingSlot.Occupants[0].GetComponent<RawStone>() != null;

        bool IsInRange()
        {
            var origin = player != null ? player : (Camera.main != null ? Camera.main.transform : null);
            return cuttingSlot != null && origin != null &&
                Vector3.Distance(origin.position, cuttingSlot.transform.position) <= interactionDistance;
        }

        void Awake()
        {
            if (interactor == null) interactor = FindAnyObjectByType<TidyInteractor>();
        }

        void Update()
        {
            if (Keyboard.current != null && cuttingKey != Key.None && Keyboard.current[cuttingKey].wasPressedThisFrame)
                TryStartCut();
        }

        public bool TryStartCut()
        {
            if (!isActiveAndEnabled || !IsReady || leftOutputPoint == null || rightOutputPoint == null ||
                !(cuttingSlot is StoneSawSlot) || TidyControlLock.IsLocked(TidyControlChannel.Interaction)) return false;
            if (!IsInRange()) return false;
            // Reuse the framework's occlusion-aware aim and reach, with an empty hand.
            if (interactor == null || interactor.CarriedCount != 0 || interactor.IsInspecting ||
                !interactor.TryFindAimed<StoneSawMachine>(out var aimed, out _) || aimed != this) return false;
            var stone = cuttingSlot.Occupants[0].GetComponent<RawStone>();
            if (stone.IsProcessed || stone.CutProfile == null) return false;
            if (cuttingPlane == null)
            {
                LastCutError = "Assign a CuttingPlane Transform to the saw (its local X axis is the plane normal).";
                Debug.LogWarning(LastCutError, this);
                return false;
            }
            if (!stone.CutProfile.ValidateCut(out var error))
            {
                Debug.LogError($"Saw '{name}': {error}", this);
                return false;
            }
            if (!stone.TryGetOre(out var ore)) return false;
            LastCutError = null;
            IsCutting = true;
            Progress = 0f;
            activeStone = stone;
            if (sawBlade != null) { bladeStart = sawBlade.localPosition; bladeRotation = sawBlade.localRotation; }
            routine = StartCoroutine(CuttingRoutine(stone, ore));
            onCutStarted.Invoke();
            return true;
        }

        IEnumerator CuttingRoutine(RawStone stone, OreDefinition ore)
        {
            var original = stone.GetComponent<TidyObject>();
            var profile = stone.CutProfile;
            float elapsed = 0f;
            while (elapsed < cutDuration)
            {
                if (stone == null || original.CurrentTarget != cuttingSlot || original.State != TidyObjectState.Placed ||
                    cuttingSlot.Occupants.Count != 1)
                { Finish(false); yield break; }
                elapsed += Time.deltaTime;
                Progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, cutDuration));
                if (sawBlade != null)
                {
                    sawBlade.localPosition = bladeStart + bladeLocalCutOffset * Mathf.Sin(Progress * Mathf.PI);
                    sawBlade.Rotate(Vector3.forward, bladeSpinDegreesPerSecond * Time.deltaTime, Space.Self);
                }
                yield return null;
            }
            CutStoneHalf left = null, right = null;
            StoneMeshCutResult result = null;
            bool completed = false;
            try
            {
                if (stone == null || original.CurrentTarget != cuttingSlot || original.State != TidyObjectState.Placed)
                    throw new InvalidOperationException("The input stone was removed before cutting finished.");
                var watch = System.Diagnostics.Stopwatch.StartNew();
                if (!StoneRuntimeMeshSlicer.TrySlice(original, cuttingPlane.position, StoneRuntimeMeshSlicer.PlaneNormal(cuttingPlane), cutSurfaceMaterial,
                    meshSettings, out result, out var cutError)) throw new InvalidOperationException(cutError);
                float totalVolume = result.NegativeVolume + result.PositiveVolume;
                left = SpawnGenerated(result.Negative, result.Materials, result.NegativeSurfaceCenter, result.Normal,
                    result.NegativeRadius, result.NegativeVolume, leftOutputPoint, original, profile, "left", ore,
                    result.NegativeVolume / totalVolume,result.CutSurfaceSubmesh);
                result.Negative = null; // Mesh ownership transferred to the piece.
                right = SpawnGenerated(result.Positive, result.Materials, result.PositiveSurfaceCenter, -result.Normal,
                    result.PositiveRadius, result.PositiveVolume, rightOutputPoint, original, profile, "right", ore,
                    result.PositiveVolume / totalVolume,result.CutSurfaceSubmesh);
                result.Positive = null;
                var separationAxis = rightOutputPoint.position - leftOutputPoint.position;
                StoneCutPieceFactory.SeparateOutputs(left, right, separationAxis, meshSettings.pieceSeparation);
                // Commit only after BOTH complete results exist; errors leave the original intact.
                stone.CompleteCut(left, right);
                watch.Stop(); LastCutMilliseconds = watch.Elapsed.TotalMilliseconds;
                completed = true;
            }
            catch (Exception exception)
            {
                Discard(left);
                Discard(right);
                LastCutError = exception.Message;
                Debug.LogWarning($"Stone cut for '{original.name}' cancelled; original preserved: {exception.Message}", this);
            }
            finally { result?.Dispose(); }
            Finish(completed);
        }

        CutStoneHalf SpawnGenerated(Mesh mesh, Material[] materials, Vector3 center, Vector3 normal,
            float radius, float volume, Transform point, TidyObject original, StoneCutProfile profile,
            string suffix, OreDefinition ore, float massFraction,int cutSurfaceSubmesh)
        {
            var half = StoneCutPieceFactory.Create(mesh, materials, center, normal, radius, volume,
                point.position, point.rotation, original, profile, suffix, ore, meshSettings, massFraction,cutSurfaceSubmesh);
            var tidy = half.GetComponent<TidyObject>();
            half.transform.position = tidy.PivotPositionForSurface(point.position, point.rotation);
            return half;
        }

        internal static CutStoneHalf CreateHalf(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale,
            TidyObject original, StoneCutProfile profile, string suffix, OreDefinition ore)
        {
            GameObject instance = null;
            try
            {
                instance = Instantiate(prefab, position, rotation);
                instance.transform.localScale = scale;
                var tidy = instance.GetComponent<TidyObject>();
                if (profile.CutPieceTidyDefinition != null) tidy.AssignDefinition(profile.CutPieceTidyDefinition);
                tidy.AssignInstanceId($"{original.InstanceId}_{suffix}");
                var half = instance.GetComponent<CutStoneHalf>();
                half.Initialize(ore, original.InstanceId, profile);
                tidy.RefreshCachedComponents();
                tidy.MarkLoose();
                return half;
            }
            catch
            {
                if (instance != null)
                {
                    TidyObjectRegistry.Unregister(instance.GetComponent<TidyObject>());
                    instance.SetActive(false);
                    Destroy(instance);
                }
                throw;
            }
        }

        static void Discard(CutStoneHalf half)
        {
            if (half == null) return;
            TidyObjectRegistry.Unregister(half.GetComponent<TidyObject>());
            half.gameObject.SetActive(false);
            Destroy(half.gameObject);
        }

        public void CancelCut(RawStone stone = null)
        {
            if (!IsCutting || (stone != null && activeStone != stone)) return;
            if (routine != null) StopCoroutine(routine);
            Finish(false);
        }

        void Finish(bool completed)
        {
            if (sawBlade != null) { sawBlade.localPosition = bladeStart; sawBlade.localRotation = bladeRotation; }
            IsCutting = false;
            activeStone = null;
            routine = null;
            Progress = completed ? 1f : 0f;
            if (completed) onCutCompleted.Invoke(); else onCutCancelled.Invoke();
        }

        void OnDisable()
        {
            CancelCut();
            if (statusLabel != null) statusLabel.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (statusLabel == null) return;
            bool visible = interactor != null && interactor.CarriedCount == 0 &&
                IsInRange() &&
                !TidyControlLock.IsLocked(TidyControlChannel.Interaction) &&
                interactor.TryFindAimed<StoneSawMachine>(out var aimed, out _) && aimed == this;
            statusLabel.gameObject.SetActive(visible);
            if (visible) statusLabel.text = IsCutting ? $"Kesiliyor... %{Progress * 100f:0}" :
                IsReady ? $"[{cuttingKey}] Taşı kes" : "Ham taşı kesim noktasına yerleştir";
        }
    }
}
