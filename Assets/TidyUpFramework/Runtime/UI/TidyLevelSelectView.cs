using System.Collections.Generic;
using TidyUpFramework.Levels;
using TidyUpFramework.Settings;
using TMPro;
using UnityEngine;

namespace TidyUpFramework.UI
{
    [AddComponentMenu("Tidy Up Framework/UI/Level Select View")]
    public sealed class TidyLevelSelectView : TidyUIScreen
    {
        [SerializeField] TidyLevelCardView cardPrefab;
        [SerializeField] RectTransform container;
        [SerializeField] TMP_Text totalStarsLabel;
        [SerializeField, Tooltip("Optional catalog. Defaults to the framework settings catalog.")] TidyLevelCatalog catalogOverride;

        readonly List<TidyLevelCardView> cards = new List<TidyLevelCardView>();

        protected override void OnShown()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            foreach (var card in cards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            cards.Clear();
            var catalog = catalogOverride != null ? catalogOverride : TidyFramework.Catalog;
            if (catalog == null || cardPrefab == null)
            {
                return;
            }

            cardPrefab.gameObject.SetActive(false);
            var parent = container != null ? container : (RectTransform)transform;
            var progress = TidyFramework.Save;
            for (var index = 0; index < catalog.Levels.Count; index++)
            {
                var level = catalog.Levels[index];
                if (level == null)
                {
                    continue;
                }

                var card = Instantiate(cardPrefab, parent);
                card.gameObject.SetActive(true);
                card.Bind(level, progress, index);
                card.Selected += TidyFramework.LoadLevel;
                cards.Add(card);
            }

            if (totalStarsLabel != null && progress != null)
            {
                totalStarsLabel.text = progress.TotalStars + " / " + catalog.Levels.Count * 3;
            }

            var firstInteractable = cards.Find(card => card.Button != null && card.Button.interactable);
            if (firstInteractable != null)
            {
                FirstSelected = firstInteractable.Button.gameObject;
                SelectFirst();
            }
        }
    }
}
