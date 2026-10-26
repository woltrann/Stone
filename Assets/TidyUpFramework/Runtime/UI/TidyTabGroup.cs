using System;
using System.Collections.Generic;
using TidyUpFramework.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TidyUpFramework.UI
{
    [Serializable]
    public sealed class TidyTab
    {
        public Button Button;
        public GameObject Page;
        public Graphic Indicator;
    }

    [AddComponentMenu("Tidy Up Framework/UI/Tab Group")]
    public sealed class TidyTabGroup : MonoBehaviour
    {
        [SerializeField] List<TidyTab> tabs = new List<TidyTab>();
        [SerializeField] Color activeColor = new Color(1f, 0.85f, 0.55f);
        [SerializeField] Color inactiveColor = new Color(1f, 1f, 1f, 0.25f);

        public int ActiveIndex { get; private set; } = -1;

        public void Configure(IEnumerable<TidyTab> newTabs)
        {
            tabs = new List<TidyTab>(newTabs);
        }

        public void Select(int index)
        {
            if (index < 0 || index >= tabs.Count)
            {
                return;
            }

            ActiveIndex = index;
            for (var tabIndex = 0; tabIndex < tabs.Count; tabIndex++)
            {
                var tab = tabs[tabIndex];
                var active = tabIndex == index;
                if (tab.Page != null)
                {
                    tab.Page.SetActive(active);
                }

                if (tab.Indicator != null)
                {
                    tab.Indicator.color = active ? activeColor : inactiveColor;
                }
            }

            if (tabs[index].Page != null)
            {
                TidyTweenRunner.Punch(tabs[index].Page.transform, Vector3.one, 0.02f, 0.2f);
            }
        }

        void Start()
        {
            for (var index = 0; index < tabs.Count; index++)
            {
                var captured = index;
                if (tabs[index].Button != null)
                {
                    tabs[index].Button.onClick.AddListener(() => Select(captured));
                }
            }

            Select(0);
        }
    }
}
