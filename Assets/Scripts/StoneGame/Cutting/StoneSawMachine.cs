using System.Collections;
using TidyUpFramework.Core;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StoneGame.Cutting
{
    [DisallowMultipleComponent]
    public sealed class StoneSawMachine : MonoBehaviour
    {
        [Header("Existing Tidy Up placement system")]
        [SerializeField] TidySnapSlot cuttingSlot;

        [Header("Interaction")]
        [SerializeField, Tooltip("Optional. Defaults to Camera.main for proximity checks.")]
        Transform player;
        [SerializeField] Key cuttingKey = Key.F;
        [SerializeField, Min(0.1f)] float interactionDistance = 2.5f;

        [Header("Saw visual animation (optional)")]
        [SerializeField] Transform sawBlade;
        [SerializeField] Vector3 bladeLocalCutOffset = new Vector3(0f, -0.4f, 0f);
        [SerializeField, Min(0.1f)] float cutDuration = 2f;
        [SerializeField] float bladeSpinDegreesPerSecond = 900f;

        [Header("Two finished pieces")]
        [SerializeField] Transform leftOutputPoint;
        [SerializeField] Transform rightOutputPoint;

        bool cutting;

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current[cuttingKey].wasPressedThisFrame)
                TryStartCut();
        }

        // Public entry point for a future UI button or interaction prompt.
        public bool TryStartCut()
        {
            if (cutting || cuttingSlot == null || leftOutputPoint == null || rightOutputPoint == null)
                return false;

            if (TidyControlLock.IsLocked(TidyControlChannel.Interaction))
                return false;

            var proximityOrigin = player != null ? player : (Camera.main != null ? Camera.main.transform : null);
            if (proximityOrigin == null || Vector3.Distance(proximityOrigin.position, transform.position) > interactionDistance)
                return false;

            if (cuttingSlot.Occupants.Count != 1)
                return false;

            var tidyObject = cuttingSlot.Occupants[0];
            if (tidyObject == null || tidyObject.State != TidyObjectState.Placed)
                return false;

            var stone = tidyObject.GetComponent<RawStone>();
            if (stone == null || stone.CutProfile == null)
                return false;

            var profile = stone.CutProfile;
            if (!IsValidPiecePrefab(profile.LeftHalfPrefab) || !IsValidPiecePrefab(profile.RightHalfPrefab))
            {
                Debug.LogError("Both half prefabs must have TidyObject and CutStoneHalf components.", this);
                return false;
            }

            if (!stone.TryGetOre(out var ore))
                return false;

            StartCoroutine(CuttingRoutine(tidyObject, profile, ore));
            return true;
        }

        static bool IsValidPiecePrefab(GameObject prefab)
        {
            return prefab != null && prefab.GetComponent<TidyObject>() != null && prefab.GetComponent<CutStoneHalf>() != null;
        }

        IEnumerator CuttingRoutine(TidyObject original, StoneCutProfile profile, OreDefinition ore)
        {
            cutting = true;
            Vector3 bladeStart = sawBlade != null ? sawBlade.localPosition : Vector3.zero;
            float elapsed = 0f;

            while (elapsed < cutDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / cutDuration);
                if (sawBlade != null)
                {
                    float downwardAmount = Mathf.Sin(progress * Mathf.PI);
                    sawBlade.localPosition = bladeStart + bladeLocalCutOffset * downwardAmount;
                    sawBlade.Rotate(Vector3.forward, bladeSpinDegreesPerSecond * Time.deltaTime, Space.Self);
                }
                yield return null;
            }

            if (sawBlade != null)
                sawBlade.localPosition = bladeStart;

            if (original != null)
            {
                // Release the original from the target BEFORE destroying it.
                // Otherwise the TidySnapSlot remains occupied forever.
                cuttingSlot.Release(original);
                original.MarkConsumed();

                SpawnHalf(profile.LeftHalfPrefab, leftOutputPoint, original, profile.CutPieceTidyDefinition, "left", ore);
                SpawnHalf(profile.RightHalfPrefab, rightOutputPoint, original, profile.CutPieceTidyDefinition, "right", null);
                Destroy(original.gameObject);

                Debug.Log(ore == null
                    ? "Cut complete: empty rock."
                    : $"Cut complete: found {ore.DisplayName}!");
            }

            cutting = false;
        }

        static void SpawnHalf(GameObject prefab, Transform outputPoint, TidyObject original,
            TidyObjectDefinition cutDefinition, string suffix, OreDefinition ore)
        {
            var instance = Instantiate(prefab, outputPoint.position, outputPoint.rotation);

            // Cut prefabs are authored in the same base dimensions as their raw stone.
            // This also transfers scaling applied to raw scene instances.
            instance.transform.localScale = Vector3.Scale(instance.transform.localScale, original.transform.lossyScale);

            var tidyObject = instance.GetComponent<TidyObject>();
            if (cutDefinition != null)
                tidyObject.AssignDefinition(cutDefinition);
            tidyObject.AssignInstanceId($"{original.InstanceId}_{suffix}");
            instance.GetComponent<CutStoneHalf>().Initialize(ore);
            tidyObject.RefreshCachedComponents();
            tidyObject.MarkLoose();
        }
    }
}
