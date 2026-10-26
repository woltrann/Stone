using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Levels
{
    [CreateAssetMenu(menuName = "Tidy Up Framework/Level Catalog", fileName = "LevelCatalog", order = 31)]
    public sealed class TidyLevelCatalog : ScriptableObject
    {
        [SerializeField, Tooltip("Levels in the order they appear in Level Select.")]
        List<TidyLevelDefinition> levels = new List<TidyLevelDefinition>();

        [SerializeField]
        string mainMenuSceneName = "TidyUp_MainMenu";

        public IReadOnlyList<TidyLevelDefinition> Levels => levels;
        public string MainMenuSceneName => mainMenuSceneName;

        public TidyLevelDefinition FindById(string levelId)
        {
            foreach (var level in levels)
            {
                if (level != null && level.LevelId == levelId)
                {
                    return level;
                }
            }

            return null;
        }

        public TidyLevelDefinition FindByScene(string sceneName)
        {
            foreach (var level in levels)
            {
                if (level != null && level.SceneName == sceneName)
                {
                    return level;
                }
            }

            return null;
        }

        public TidyLevelDefinition NextAfter(TidyLevelDefinition level)
        {
            var index = levels.IndexOf(level);
            return index >= 0 && index + 1 < levels.Count ? levels[index + 1] : null;
        }

        public void Configure(IEnumerable<TidyLevelDefinition> newLevels, string mainMenu)
        {
            levels = new List<TidyLevelDefinition>(newLevels);
            mainMenuSceneName = mainMenu;
        }
    }
}
