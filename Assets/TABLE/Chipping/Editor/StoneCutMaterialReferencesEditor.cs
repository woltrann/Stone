using System;
using UnityEngine;
using UnityEditor;
using StoneGame.Cutting;
namespace StoneGame.Chipping.Editor
{
    [InitializeOnLoad]
    public static class StoneCutMaterialReferencesEditor
    {
        const string Folder="Assets/TABLE/Chipping/Resources",Path=Folder+"/TableCutMaterials.asset";
        static bool queued;
        static StoneCutMaterialReferencesEditor(){EditorApplication.delayCall+=SyncLoadedSaws;Undo.postprocessModifications+=Changed;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingEditMode)SyncLoadedSaws();};}
        static UndoPropertyModification[] Changed(UndoPropertyModification[] changes)
        {
            foreach(var change in changes)if(change.currentValue!=null && change.currentValue.target is StoneSawMachine && !queued){queued=true;EditorApplication.delayCall+=SyncLoadedSaws;break;}return changes;
        }
        public static void SyncLoadedSaws()
        {
            queued=false;bool needsFallback=false;
            foreach(var saw in Resources.FindObjectsOfTypeAll<StoneSawMachine>()){Register(saw.CutSurfaceMaterial);needsFallback|=saw.CutSurfaceMaterial==null;}
            // The existing slicer uses the source material when no dedicated cut material is assigned.
            if(needsFallback)foreach(var raw in Resources.FindObjectsOfTypeAll<RawStone>())foreach(var renderer in raw.GetComponentsInChildren<MeshRenderer>(true))foreach(var material in renderer.sharedMaterials)Register(material);
        }
        public static void Register(Material material)
        {
            if(material==null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material,out string guid,out long localId))return;
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/TABLE/Chipping","Resources");
            var library=AssetDatabase.LoadAssetAtPath<StoneCutMaterialReferences>(Path);
            if(library==null){library=ScriptableObject.CreateInstance<StoneCutMaterialReferences>();AssetDatabase.CreateAsset(library,Path);}
            if(library.Register(guid+":"+localId,material)){EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);}
        }
    }
}
