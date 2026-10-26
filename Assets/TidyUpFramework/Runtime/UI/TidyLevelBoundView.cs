using TidyUpFramework.Core;
using TidyUpFramework.Levels;
using UnityEngine;

namespace TidyUpFramework.UI
{
    public abstract class TidyLevelBoundView : MonoBehaviour
    {
        protected TidyLevelManager LevelManager { get; private set; }

        protected virtual void Start()
        {
            if (!TidyServices.TryGet(out TidyLevelManager manager))
            {
                return;
            }

            LevelManager = manager;
            LevelManager.LevelStarted += HandleLevelStarted;
            if (LevelManager.Context != null)
            {
                Bind(LevelManager);
            }
        }

        protected virtual void OnDestroy()
        {
            if (LevelManager == null)
            {
                return;
            }

            LevelManager.LevelStarted -= HandleLevelStarted;
            if (LevelManager.Context != null)
            {
                Unbind(LevelManager);
            }
        }

        void HandleLevelStarted(TidyLevelManager manager)
        {
            Bind(manager);
        }

        protected abstract void Bind(TidyLevelManager manager);

        protected abstract void Unbind(TidyLevelManager manager);
    }
}
