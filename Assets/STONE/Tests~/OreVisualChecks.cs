using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using StoneGame.Cutting;
using TidyUpFramework.Objects;
using TidyUpFramework.Interaction;
using TidyUpFramework.Player;
using TidyUpFramework.Save;

public static class StoneOreVisualChecks
{
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
    public static async Task<object> Main()
    {
        if(!Application.isPlaying) throw new Exception("Run in Play mode.");
        var saw=UnityEngine.Object.FindAnyObjectByType<StoneSawMachine>();
        var slot=UnityEngine.Object.FindAnyObjectByType<StoneSawSlot>();
        var interactor=UnityEngine.Object.FindAnyObjectByType<TidyInteractor>();
        var controller=UnityEngine.Object.FindAnyObjectByType<TidyFirstPersonController>();
        var stone=UnityEngine.Object.FindObjectsByType<RawStone>().OrderBy(s=>s.name).First();
        var tidy=stone.GetComponent<TidyObject>();
        var profile=stone.CutProfile;
        var testProfile=UnityEngine.Object.Instantiate(profile);
        testProfile.hideFlags=HideFlags.HideAndDontSave;
        var slotRotation=slot.transform.rotation;
        var originalDuration=(float)saw.GetType().GetField("cutDuration",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(saw);
        var before=new TidyLevelSnapshot();
        TidyLevelSnapshotter.CaptureWorld(before,interactor,controller);
        var results=new List<object>();
        try
        {
            Set(saw,"cutDuration",.15f);
            foreach(var ore in profile.OreChances.Select(e=>e.ore))
            {
                TidyLevelSnapshotter.RestoreWorld(before,interactor,controller);
                await Task.Delay(60);
                var approach=slot.transform.position+new Vector3(0f,-slot.transform.position.y+.03f,-1.45f);
                controller.Teleport(approach,0f,25f);
                await Task.Delay(60);
                var direction=(slot.transform.position+Vector3.up*.08f-interactor.AimCamera.transform.position).normalized;
                controller.Teleport(approach,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-Mathf.Asin(direction.y)*Mathf.Rad2Deg);
                Set(stone,"cutProfile",testProfile);
                Set(testProfile,"containsOrePercent",100f);
                Set(testProfile,"oreChances",new List<OreChanceEntry>{new OreChanceEntry{ore=ore,chancePercent=100f}});
                slot.transform.rotation=Quaternion.Euler(0f,ore==profile.OreChances.Last().ore?0f:45f,0f);
                if(!slot.TryPlaceDirect(tidy,false) || !saw.TryStartCut()) throw new Exception("Cannot start ore cut: "+ore.name);
                var timeout=DateTime.UtcNow.AddSeconds(4);
                while(saw.IsCutting) { if(DateTime.UtcNow>timeout) throw new Exception("Cut stalled"); await Task.Delay(25); }
                var halves=UnityEngine.Object.FindObjectsByType<CutStoneHalf>().Where(h=>h.SourceStoneId==tidy.InstanceId).ToArray();
                if(halves.Length!=2) throw new Exception("Expected two halves");
                foreach(var half in halves)
                {
                    var socket=half.transform.Find("GeneratedCutSurface");
                    if(half.ContainedOre!=ore || socket.childCount!=1) throw new Exception("Ore metadata/visual mismatch");
                    var visual=socket.GetChild(0);
                    if(visual.GetComponent<MeshFilter>().sharedMesh!=ore.RawOreVisualPrefab.GetComponent<MeshFilter>().sharedMesh) throw new Exception("Wrong visual mesh");
                    if(Vector3.Distance(visual.position,socket.position)>.0001f) throw new Exception("Ore not at socket");
                    if(visual.GetComponentsInChildren<Collider>().Length!=0) throw new Exception("Ore collider present");
                }
                var expectedWidth=tidy.LocalBounds.size.x*Mathf.Abs(tidy.transform.lossyScale.x);
                if(halves.Any(h=>h.GeneratedMesh==null || h.CutSurfaceSubmesh!=h.GeneratedMesh.subMeshCount-1)) throw new Exception("Cut geometry metadata missing");
                bool overlapping=halves[0].GetComponent<TidyObject>().CalculateWorldBounds().Intersects(halves[1].GetComponent<TidyObject>().CalculateWorldBounds());
                if(overlapping) throw new Exception("Output halves overlap");
                results.Add(new {ore=ore.name,halves=halves.Length,prefabMatches=true,socketsAligned=true,overlap=overlapping});
                if(ore==profile.OreChances.Last().ore)
                {
                    var camera=interactor.AimCamera;
                    var savedPosition=camera.transform.localPosition;
                    var savedRotation=camera.transform.localRotation;
                    var oldTarget=camera.targetTexture;
                    var oldActive=RenderTexture.active;
                    var muted=new System.Collections.Generic.List<Renderer>();
                    foreach(var other in UnityEngine.Object.FindObjectsByType<TidyObject>())
                    {
                        if(halves.Any(h=>h.gameObject==other.gameObject)) continue;
                        if(Vector3.Distance(other.transform.position,slot.transform.position)>2f) continue;
                        foreach(var renderer in other.GetComponentsInChildren<Renderer>()) if(renderer.enabled) {muted.Add(renderer);renderer.enabled=false;}
                    }
                    var target=new RenderTexture(1280,720,24);
                    var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
                    try
                    {
                        camera.transform.position=slot.transform.position+new Vector3(.15f,1f,-1f);
                        camera.transform.LookAt(slot.transform.position);
                        camera.targetTexture=target;
                        await Task.Delay(80); camera.Render();
                        RenderTexture.active=target;
                        image.ReadPixels(new Rect(0,0,1280,720),0,0);
                        image.Apply();
                        System.IO.Directory.CreateDirectory("Temp/StoneCutting");
                        System.IO.File.WriteAllBytes("Temp/StoneCutting/ore-preview.png",image.EncodeToPNG());
                    }
                    finally
                    {
                        foreach(var renderer in muted) if(renderer!=null) renderer.enabled=true;
                        camera.transform.localPosition=savedPosition;
                        camera.transform.localRotation=savedRotation;
                        camera.targetTexture=oldTarget;
                        RenderTexture.active=oldActive;
                        target.Release();
                        UnityEngine.Object.Destroy(target);
                        UnityEngine.Object.Destroy(image);
                    }
                }
            }
            return new {results, screenshot="Temp/StoneCutting/ore-preview.png"};
        }
        finally
        {
            saw.CancelCut();
            slot.transform.rotation=slotRotation;
            Set(saw,"cutDuration",originalDuration);
            TidyLevelSnapshotter.RestoreWorld(before,interactor,controller);
            Set(stone,"cutProfile",profile);
            UnityEngine.Object.Destroy(testProfile);
        }
    }
}

