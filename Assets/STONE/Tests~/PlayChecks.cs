using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using StoneGame.Cutting;
using TidyUpFramework.Objects;
using TidyUpFramework.Placement;
using TidyUpFramework.Interaction;
using TidyUpFramework.Input;
using TidyUpFramework.Player;
using TidyUpFramework.Save;

public static class StonePlayChecks
{
    static readonly List<string> passed = new List<string>();
    static void Check(bool ok, string message) { if(!ok) throw new Exception(message); passed.Add(message); }
    static void Set(object owner, string field, object value) => owner.GetType().GetField(field, BindingFlags.NonPublic|BindingFlags.Instance).SetValue(owner,value);
    static async Task WaitUntil(Func<bool> predicate, string message, int milliseconds=6000)
    {
        var end=DateTime.UtcNow.AddMilliseconds(milliseconds);
        while(!predicate()) { if(DateTime.UtcNow>end) throw new Exception("Timeout: "+message); await Task.Delay(30); }
    }
    public static async Task<object> Main()
    {
        Check(Application.isPlaying,"Play mode active");
        var frame=Time.frameCount;
        await Task.Delay(100);
        Check(Time.frameCount>frame,"Frame counter advances");
        var saw=UnityEngine.Object.FindAnyObjectByType<StoneSawMachine>();
        var slot=UnityEngine.Object.FindAnyObjectByType<StoneSawSlot>();
        var interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();
        var input=UnityEngine.Object.FindAnyObjectByType<TidyInputReader>();
        var controller=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();
        var originals=UnityEngine.Object.FindObjectsByType<RawStone>().OrderBy(s=>s.name).ToArray();
        var originalProfiles=originals.Select(s=>s.CutProfile).ToArray();
        var originalProfile=originalProfiles[0];
        var testProfile=UnityEngine.Object.Instantiate(originalProfile);
        Set(testProfile,"leftHalfPrefab",null); Set(testProfile,"rightHalfPrefab",null);
        testProfile.hideFlags=HideFlags.HideAndDontSave;
        var previousBackground=UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior;
        UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
        var testKeyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("StoneCuttingChecks");
        var previousInputBehavior=UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
        UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        QueueKeyboard(new UnityEngine.InputSystem.LowLevel.KeyboardState());
        await Task.Delay(100);
        var playerPosition=controller.transform.position;
        var playerYaw=controller.Yaw;
        var playerPitch=controller.Pitch;
        var sourceSnapshot=new TidyLevelSnapshot();
        TidyLevelSnapshotter.CaptureWorld(sourceSnapshot,interactor,controller);
        try
        {
            var approach=slot.transform.position+new Vector3(0f,-slot.transform.position.y+0.03f,-1.45f);
            controller.Teleport(approach,0f,25f);
            await Task.Delay(120);
            var camera=interactor.AimCamera.transform;
            var direction=(slot.transform.position+Vector3.up*.08f-camera.position).normalized;
            var yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            var pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;
            controller.Teleport(approach,yaw,pitch);
            await Task.Delay(120);
            Physics.SyncTransforms();
            Check(interactor.TryFindAimed<StoneSawMachine>(out var aimed,out _) && aimed==saw,"Existing interactor aims at saw without occlusion");
            var first=originals[0]; var tidy=first.GetComponent<TidyObject>();
            Set(first,"cutProfile",testProfile);
            Set(testProfile,"containsOrePercent",0f);
            Check(first.TryGetOre(out var empty) && empty==null,"0% instance rolls empty");
            Set(testProfile,"containsOrePercent",100f);
            Check(first.TryGetOre(out empty) && empty==null,"Existing stone never rerolls after profile change");
            interactor.ForcePickUp(tidy);
            Check(interactor.HeldObject==tidy && tidy.State==TidyObjectState.Held,"Framework pickup remains functional");
            await Task.Delay(150);
            Check(Vector3.Distance(tidy.transform.position,interactor.AimCamera.transform.position)<2f,"Held stone follows existing carry system");
            interactor.ForceDrop();
            await WaitUntil(()=>tidy.State==TidyObjectState.Loose,"drop finishes");
            Check(interactor.HeldObject==null && tidy.State==TidyObjectState.Loose,"Framework drop remains functional");
            interactor.ForcePickUp(tidy);
            await Task.Delay(200);
            Check(interactor.CurrentCandidate.Target==slot && interactor.CurrentCandidate.IsValid,"Framework placement preview recognizes saw slot");
            input.TriggerVirtual(TidyInputAction.Interact);
            await WaitUntil(()=>tidy.State==TidyObjectState.Placed,"input commits placement");
            Check(interactor.HeldObject==null && slot.Occupants.Count==1,"Existing interact input places carried stone");
            Check(saw.TryStartCut(),"Timed cutting starts");
            Check(!saw.TryStartCut(),"Second cut blocked while busy");
            Check(!tidy.IsAvailableForPickup && !slot.AllowsRemoval(tidy),"Input stone locked during cutting");
            Check(!slot.Evaluate(TidyPlacementQuery.WithoutAim(originals[1].GetComponent<TidyObject>())).IsValid,"Second input blocked");
            Check(first.gameObject.activeInHierarchy && tidy.State==TidyObjectState.Placed,"Input retained until cut completes");
            await WaitUntil(()=>!saw.IsCutting,"empty cut completes");
            var halves=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Where(h=>h.SourceStoneId==tidy.InstanceId).ToArray();
            Check(first.IsProcessed && !first.gameObject.activeSelf && halves.Length==2 && slot.Occupants.Count==0,"Empty cut creates exactly two halves and releases slot");
            Check(halves.All(h=>!h.ContainsOre && h.GetComponentsInChildren<Renderer>().Length==1),"Empty halves have no ore visual");
            Check(halves.All(h=>h.GetComponent<TidyObject>().IsAvailableForPickup && h.GetComponent<Rigidbody>()!=null && h.GetComponent<Collider>().enabled),"Both halves support existing physics and interaction");
            Check(!slot.Evaluate(TidyPlacementQuery.WithoutAim(halves[0].GetComponent<TidyObject>())).IsValid,"Cut halves cannot be processed again");
            interactor.ForcePickUp(halves[0].GetComponent<TidyObject>());
            Check(interactor.HeldObject==halves[0].GetComponent<TidyObject>(),"Cut half can be picked up");
            interactor.ForceDrop();
            await Task.Delay(450);

            var second=originals[1]; var secondTidy=second.GetComponent<TidyObject>();
            Set(second,"cutProfile",testProfile);
            Check(slot.TryPlaceDirect(secondTidy,false),"Saw accepts next raw stone");
            Check(second.TryGetOre(out var ore) && ore!=null,"100% instance contains ore");
            for(int i=0;i<100;i++) CheckOre(second,ore);
            Check(saw.TryStartCut(),"Ore cut starts");
            await Task.Delay(150);
            saw.enabled=false;
            Check(!saw.IsCutting && second.gameObject.activeSelf && secondTidy.IsAvailableForPickup,"Disabling saw safely cancels and unlocks input");
            saw.enabled=true;
            QueueKeyboard(new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.T));
            await Task.Delay(200);
            if(!saw.IsCutting) throw new Exception("Key diagnostic: pressed="+UnityEngine.InputSystem.Keyboard.current.tKey.isPressed+" ready="+saw.IsReady+" enabled="+saw.isActiveAndEnabled+" aim="+(interactor.TryFindAimed<StoneSawMachine>(out var diagAim,out _) && diagAim==saw)+" directStart="+saw.TryStartCut()+" lock="+TidyUpFramework.Core.TidyControlLock.Locked);
            QueueKeyboard(new UnityEngine.InputSystem.LowLevel.KeyboardState());
            Check(saw.IsCutting,"T key starts cancelled cut through Input System without throw conflict");
            await WaitUntil(()=>!saw.IsCutting,"ore cut completes");
            var oreHalves=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Where(h=>h.SourceStoneId==secondTidy.InstanceId).ToArray();
            Check(oreHalves.Length==2 && oreHalves.All(h=>h.ContainedOre==ore && h.SourceProfile==testProfile && h.GeneratedMesh!=null),"Two generated ore halves preserve source/profile/ore metadata");
            Check(oreHalves.All(h=>StoneMeshTopology.ValidateClosed(h.GeneratedMesh,h.GeneratedMesh.bounds.size.magnitude*.00002f,out _,out _)),"Runtime meshes remain closed after spawning");
            Check(oreHalves.All(h=>h.GetComponent<MeshRenderer>().sharedMaterials.Length==h.GeneratedMesh.subMeshCount),"Runtime material slots match submesh indices");
            Check(oreHalves.All(h=>h.GetComponentsInChildren<Renderer>().Length==2),"Correct ore visual appears on both cut faces");
            Check(oreHalves.All(h=>h.transform.Find("GeneratedCutSurface").GetComponentsInChildren<Collider>().Length==0 && h.GetComponents<Collider>().Any(c=>c.enabled)),"Ore remains visual-only; compound piece colliders enabled");
            var afterCut=new TidyLevelSnapshot();
            TidyLevelSnapshotter.CaptureWorld(afterCut,interactor,controller);
            var json=JsonUtility.ToJson(afterCut);
            TidyLevelSnapshotter.RestoreWorld(sourceSnapshot,interactor,controller);
            await Task.Delay(100);
            Check(!UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Any(h=>h.SourceStoneId==tidy.InstanceId || h.SourceStoneId==secondTidy.InstanceId),"Restoring earlier save removes this test's dynamic halves");
            for(int p=0;p<originals.Length;p++) Set(originals[p],"cutProfile",originalProfiles[p]);
            TidyLevelSnapshotter.RestoreWorld(JsonUtility.FromJson<TidyLevelSnapshot>(json),interactor,controller);
            await Task.Delay(150);
            var restored=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Where(h=>h.SourceStoneId==secondTidy.InstanceId).ToArray();
            Check(restored.Length==2 && restored.All(h=>h.ContainedOre==ore && h.GeneratedMesh!=null),"Snapshot JSON roundtrip restores actual generated meshes without reroll or duplication");
            Check(restored.All(h=>StoneMeshTopology.ValidateClosed(h.GeneratedMesh,h.GeneratedMesh.bounds.size.magnitude*.00002f,out _,out _)),"Restored geometry remains closed");
            Check(!second.gameObject.activeSelf && secondTidy.State==TidyObjectState.Consumed,"Snapshot preserves consumed source");
            return new {passed,frames=Time.frameCount-frame,ore=ore.name,sourceSnapshotObjects=sourceSnapshot.Objects.Count,savedObjects=afterCut.Objects.Count};
        }
        finally
        {
            QueueKeyboard(new UnityEngine.InputSystem.LowLevel.KeyboardState());
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=previousInputBehavior;
            UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior=previousBackground;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(testKeyboard);
            saw.CancelCut();
            saw.enabled=true;
            TidyLevelSnapshotter.RestoreWorld(sourceSnapshot,interactor,controller);
            for(int p=0;p<originals.Length;p++) Set(originals[p],"cutProfile",originalProfiles[p]);
            UnityEngine.Object.Destroy(testProfile);
            controller.Teleport(playerPosition,playerYaw,playerPitch);
        }
    }
    static void QueueKeyboard(UnityEngine.InputSystem.LowLevel.KeyboardState state)
    {
        System.Action before = null;
        before = () => {
            if(UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType != UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic) return;
            UnityEngine.InputSystem.InputSystem.onBeforeUpdate -= before;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,state);
        };
        UnityEngine.InputSystem.InputSystem.onBeforeUpdate += before;
    }
    static void CheckOre(RawStone stone, OreDefinition expected)
    {
        if(!stone.TryGetOre(out var actual) || actual!=expected) throw new Exception("Ore rerolled");
    }
}





