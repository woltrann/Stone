using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StoneGame.Computers;
public static class PCInputProbe
{
 public static async Task<object> Main()
 {
  var pc=UnityEngine.Object.FindAnyObjectByType<InteractiveComputer>();pc.AbortImmediately();
  var keyboard=InputSystem.AddDevice<Keyboard>("PCInputProbe");
  var old=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  var bg=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
  bool press=false;int dynamic=0,edit=0,pressed=0;var samples=new List<string>();
  Action before=()=>{if(InputState.currentUpdateType!=InputUpdateType.Dynamic){edit++;return;}dynamic++;InputSystem.QueueStateEvent(keyboard,press?new KeyboardState(Key.E):new KeyboardState());};
  Action after=()=>{if(press && samples.Count<12){pressed++;if(samples.Count<12)samples.Add("type="+InputState.currentUpdateType+" down="+keyboard.eKey.isPressed+" enabled="+keyboard.enabled+" press "+Time.frameCount+" current="+Keyboard.current.name+" currentE="+Keyboard.current.eKey.wasPressedThisFrame+" target="+pc.CanTarget(out _)+" state="+pc.State);}};
  InputSystem.onBeforeUpdate+=before;InputSystem.onAfterUpdate+=after;
  try {await Task.Delay(150);press=true;await Task.Delay(150);press=false;await Task.Delay(1100);return new {dynamic,edit,pressed,samples,state=pc.State.ToString(),target=pc.CanTarget(out _)};}
  finally{InputSystem.onBeforeUpdate-=before;InputSystem.onAfterUpdate-=after;InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=bg;InputSystem.settings.editorInputBehaviorInPlayMode=old;pc.AbortImmediately();}
 }
}

