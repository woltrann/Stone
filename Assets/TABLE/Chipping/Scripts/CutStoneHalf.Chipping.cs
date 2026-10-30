using System;
using UnityEngine;
using TidyUpFramework.Save;
namespace StoneGame.Cutting
{
    public sealed partial class CutStoneHalf : ITidyObjectSnapshotParticipant
    {
        [SerializeField,Min(1)] int requiredHits=6;
        [SerializeField,HideInInspector] int currentHits;
        public int RequiredHits=>Mathf.Max(1,requiredHits);
        public int CurrentHits=>currentHits;
        public float ChippingProgress=>(float)currentHits/RequiredHits;
        public bool IsChippingCompleted=>currentHits>=RequiredHits;
        public bool CanChip=>!IsChippingCompleted && HasChippingSurface;
        public bool HasChippingSurface=>cutSurfaceMaterial!=null && cutSurfaceSubmesh>=0 && ownedMesh!=null && cutSurfaceSubmesh<ownedMesh.subMeshCount && stoneGeometryRenderers!=null && stoneGeometryRenderers.Length>0;
        public string ChippingConfigurationError=>HasChippingSurface?null:"Taşın gerçek kesit materyali veya kesit submesh bilgisi bulunamadı. Kesim verilerini kontrol edin.";
        public event Action<CutStoneHalf> ChippingChanged;
        [SerializeField,Tooltip("Only the stone mesh renderers. Do not include ore visuals, UI or effects.")]
        Renderer[] stoneGeometryRenderers;
        Material[][] originalMaterials;
        void ResetChipping()
        {
            currentHits=0;requiredHits=SourceProfile!=null?SourceProfile.ChippingRequiredHits:6;
            if(stoneGeometryRenderers==null || stoneGeometryRenderers.Length==0){var root=GetComponent<MeshRenderer>();stoneGeometryRenderers=root!=null?new Renderer[]{root}:Array.Empty<Renderer>();}
            originalMaterials=new Material[stoneGeometryRenderers.Length][];
            for(int i=0;i<stoneGeometryRenderers.Length;i++)if(stoneGeometryRenderers[i]!=null)originalMaterials[i]=stoneGeometryRenderers[i].sharedMaterials;
        }
        public bool TryChip()
        {
            if(!CanChip)return false;
            currentHits=Mathf.Min(RequiredHits,currentHits+1);ApplyChippingAppearance();
            if(gameObject.activeInHierarchy && TidyUpFramework.Core.TidyServices.TryGet<TidyUpFramework.Levels.TidyLevelManager>(out var level))level.MarkCustomStateDirty();
            ChippingChanged?.Invoke(this);return true;
        }
        void ApplyChippingAppearance()
        {
            if(!HasChippingSurface)return;
            for(int r=0;r<stoneGeometryRenderers.Length;r++)
            {
                var renderer=stoneGeometryRenderers[r];if(renderer==null)continue;
                if(oreVisual!=null && renderer.transform.IsChildOf(oreVisual.transform))continue;
                if(renderer.GetComponent<MeshFilter>()==null || renderer.GetComponentInParent<CutStoneHalf>()!=this)continue;
                if(IsChippingCompleted)
                {var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)materials[i]=cutSurfaceMaterial;renderer.sharedMaterials=materials;}
            }
        }
        void RestoreOriginalAppearance()
        {if(originalMaterials==null)return;for(int i=0;i<stoneGeometryRenderers.Length;i++)if(stoneGeometryRenderers[i]!=null && originalMaterials[i]!=null)stoneGeometryRenderers[i].sharedMaterials=originalMaterials[i];}
        [Serializable]sealed class ChippingSave{public int version=2,requiredHits,currentHits,surfaceSubmesh;public bool completed;public string oreName,surfaceMaterialId;}
        public string CaptureObjectState()=>JsonUtility.ToJson(new ChippingSave{requiredHits=RequiredHits,currentHits=currentHits,completed=IsChippingCompleted,oreName=ContainedOre!=null?ContainedOre.name:null,surfaceMaterialId=CutSurfaceMaterialId,surfaceSubmesh=cutSurfaceSubmesh});
        public void RestoreObjectState(string json)
        {
            if(string.IsNullOrEmpty(json)){currentHits=0;RestoreOriginalAppearance();return;}
            try
            {
                var data=JsonUtility.FromJson<ChippingSave>(json);
                if(data==null || (data.version!=1 && data.version!=2) || data.requiredHits<1 || data.currentHits<0 || data.currentHits>data.requiredHits || data.completed!=(data.currentHits==data.requiredHits))
                    throw new FormatException("Vuruş verisi geçersiz.");
                if(data.version>=2){cutSurfaceSubmesh=data.surfaceSubmesh;savedCutSurfaceMaterialId=data.surfaceMaterialId;cutSurfaceMaterial=StoneCutMaterialReferences.Resolve(savedCutSurfaceMaterialId);}
                requiredHits=data.requiredHits;currentHits=data.currentHits;
                RestoreOriginalAppearance();ApplyChippingAppearance();
                if(!HasChippingSurface)Debug.LogWarning(ChippingConfigurationError,this);
                ChippingChanged?.Invoke(this);
            }
            catch(Exception ex){Debug.LogWarning("Yontma kaydı yüklenemedi: "+ex.Message,this);}
        }
    }
}
