using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Unity.Profiling;
using StoneGame.Delivery;
using StoneGame.Economy;
public static class ThousandStoneProfile
{
 [Serializable]public class Metric{public string name;public int samples;public double median,p95,max,mean;}
 [Serializable]public class BodyStats{public int stones,awake,sleeping,kinematic,almostStill;public double maxSpeed;public float defaultSleepThreshold;public int solverIterations;}
 [Serializable]public class Report{public BodyStats bodies;public Metric[] metrics;public int frames;public string deliveryPhase;public double lastBackgroundWriteMs;}
 static double P(List<double> a,double q)=>a[Math.Min(a.Count-1,(int)(a.Count*q))];
 public static async Task<object> Main()
 {
  if(!Application.isPlaying)throw new Exception("Observe current Play Mode only");
  string[] names={"Main Thread","Physics.Simulate","Physics.FetchResults","Physics.UpdateRigidbodies","FixedBehaviourUpdate","BehaviourUpdate","LateBehaviourUpdate","GC.Collect","GC Allocated In Frame","Draw Calls Count","CPU Render Thread Frame Time"};
  var cat=new[]{ProfilerCategory.Internal,ProfilerCategory.Physics,ProfilerCategory.Physics,ProfilerCategory.Physics,new ProfilerCategory("PlayerLoop"),new ProfilerCategory("PlayerLoop"),new ProfilerCategory("PlayerLoop"),new ProfilerCategory("GC"),ProfilerCategory.Memory,ProfilerCategory.Render,ProfilerCategory.Render};
  var recorders=new ProfilerRecorder[names.Length];var values=new List<double>[names.Length+1];for(int i=0;i<values.Length;i++)values[i]=new List<double>(3000);
  for(int i=0;i<names.Length;i++)recorders[i]=ProfilerRecorder.StartNew(cat[i],names[i],1);
  int last=Time.frameCount,frames=0;
  EditorApplication.CallbackFunction sample=()=>{if(Time.frameCount==last)return;last=Time.frameCount;if(frames++<10)return;values[names.Length].Add(Time.unscaledDeltaTime*1000d);for(int i=0;i<names.Length;i++)if(recorders[i].Valid)values[i].Add(recorders[i].LastValue/(i==8||i==9?1d:1000000d));};
  EditorApplication.update+=sample;
  try{await Task.Delay(30000);}
  finally{EditorApplication.update-=sample;for(int i=0;i<recorders.Length;i++)recorders[i].Dispose();}
  var metrics=new List<Metric>();for(int i=0;i<values.Length;i++){var v=values[i];if(v.Count==0)continue;v.Sort();metrics.Add(new Metric{name=i<names.Length?names[i]:"Observed frame ms",samples=v.Count,median=P(v,.5),p95=P(v,.95),max=v[v.Count-1],mean=v.Average()});}
  var bodies=new BodyStats{defaultSleepThreshold=Physics.sleepThreshold,solverIterations=Physics.defaultSolverIterations};foreach(var stone in UnityEngine.Object.FindObjectsByType<DeliveredStoneInstance>()){bodies.stones++;var b=stone.GetComponent<Rigidbody>();if(b.isKinematic)bodies.kinematic++;else if(b.IsSleeping())bodies.sleeping++;else{bodies.awake++;float speed=b.linearVelocity.magnitude;bodies.maxSpeed=Math.Max(bodies.maxSpeed,speed);if(speed<.05f && b.angularVelocity.sqrMagnitude<.01f)bodies.almostStill++;}}
  var delivery=UnityEngine.Object.FindAnyObjectByType<StoneDeliveryManager>();var economy=UnityEngine.Object.FindAnyObjectByType<StoneEconomyBootstrap>().Service;
  var report=new Report{bodies=bodies,metrics=metrics.ToArray(),frames=frames,deliveryPhase=delivery.Phase.ToString(),lastBackgroundWriteMs=economy.LastBackgroundWriteMilliseconds};File.WriteAllText("Assets/PC/Documentation~/Thousand-stone-profile.json",JsonUtility.ToJson(report,true));return report;
 }
}


