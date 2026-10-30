using System.IO;
using TidyUpFramework.Core;
using TidyUpFramework.Save;
using TidyUpFramework.Settings;
using UnityEngine;
namespace StoneGame.Economy
{
    [DefaultExecutionOrder(-40),DisallowMultipleComponent]
    public sealed class StoneEconomyBootstrap : MonoBehaviour
    {
        [SerializeField] EconomySettings settings;
        // Can be overridden on an isolated test instance; the production scene leaves it empty.
        [SerializeField,HideInInspector] string storagePathOverride;
        EconomyService service;
        bool ownsService;
        TidySaveService frameworkSave;
        TidyProgressData observedProgress;
        public EconomyService Service=>service;
        public EconomySettings Settings=>settings;
        public string StoragePath{get;private set;}
        void Awake()
        {
            if(TidyServices.TryGet(out service))return;
            if(settings==null){Debug.LogWarning("[Stone Economy] EconomySettings atanmamış.",this);return;}
            string folder=TidyFramework.Settings!=null?TidyFramework.Settings.SaveFolderName:"TidyUpSaves";
            StoragePath=string.IsNullOrEmpty(storagePathOverride)?Path.Combine(Application.persistentDataPath,folder,"stone-market.json"):storagePathOverride;
            service=new EconomyService(new EconomyFileStore(StoragePath),settings.startingBalance);ownsService=true;
            TidyServices.Register(service);DontDestroyOnLoad(gameObject);
            frameworkSave=TidyFramework.Save;
            TidyFramework.LevelRestarting+=RestartLevel;
            if(frameworkSave!=null){observedProgress=frameworkSave.Progress;frameworkSave.ProgressChanged+=ProgressChanged;}
            if(!service.IsReady)Debug.LogWarning("[Stone Economy] "+service.LoadError,this);
        }
        void ProgressChanged(TidyProgressData progress)
        {
            // Framework ResetProgress replaces the progress object. Ordinary autosaves keep its identity.
            if(ReferenceEquals(progress,observedProgress))return;
            observedProgress=progress;
            if(!service.StartNewGame(settings.startingBalance,out var error))Debug.LogWarning("[Stone Economy] Yeni oyun kaydı başarısız: "+error,this);
        }
        void OnDestroy()
        {
            TidyFramework.LevelRestarting-=RestartLevel;
            if(ownsService)service?.FlushPendingWrites();
            if(frameworkSave!=null)frameworkSave.ProgressChanged-=ProgressChanged;
            if(ownsService)TidyServices.Unregister(service);
        }
        void Update(){service?.PumpWrites();}
        bool RestartLevel()
        {
            if(service.StartNewGame(settings.startingBalance,out var error))return true;
            Debug.LogWarning("[Stone Economy] Yeniden başlatma iptal edildi; başlangıç kaydı yazılamadı: "+error,this);
            return false;
        }
        void OnApplicationQuit(){if(ownsService)service?.FlushPendingWrites();}
    }
}
