using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class MarioMotionRepair
{
 [MenuItem("Tools/Mario Demo/Repair Character Motion")]
 public static void Repair()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");var report=new List<object>();
  foreach(var name in new[]{"Mario","Luigi","Toad"})
  {
   var path=HelloMarioPlayableBuilder.Playables+name+"Player.prefab";var root=PrefabUtility.LoadPrefabContents(path);
   try
   {
    var folder=HelloMarioPlayableBuilder.AnimFolder(name);var animator=root.GetComponent<Animator>();var controller=(AnimatorController)animator.runtimeAnimatorController;var machine=controller.layers[0].stateMachine;
    root.GetComponent<CapsuleCollider>().sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Art/Environment/MarioTest/MarioTest.physicMaterial");
    if(!controller.parameters.Any(p=>p.name=="Jumping"))controller.AddParameter("Jumping",AnimatorControllerParameterType.Bool);
    foreach(var transition in machine.anyStateTransitions.Concat(machine.states.SelectMany(s=>s.state.transitions)))
    {
     if(!transition.conditions.Any(c=>c.parameter=="GroundPounding"))transition.AddCondition(AnimatorConditionMode.IfNot,0,"GroundPounding");
     if(transition.destinationState.name=="Jump" && !transition.conditions.Any(c=>c.parameter=="Jumping"))transition.AddCondition(AnimatorConditionMode.If,0,"Jumping");
    }
    foreach(var transition in machine.anyStateTransitions.Where(t=>t.destinationState.name=="Fall"))
    {
     foreach(var condition in transition.conditions.Where(c=>c.parameter=="VerticalSpeed").ToArray())transition.RemoveCondition(condition);
     if(!transition.conditions.Any(c=>c.parameter=="Jumping"))transition.AddCondition(AnimatorConditionMode.IfNot,0,"Jumping");
    }
    var clip=HelloMarioPlayableBuilder.ConvertClip(root,root.transform.Find("Visual/Model").gameObject,"Run","Run",folder+"Run.anim",report);
    machine.states.Single(s=>s.state.name=="Run").state.motion=clip;
    AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Idle.anim").SampleAnimation(root,0);
    PrefabUtility.SaveAsPrefabAsset(root,path);EditorUtility.SetDirty(controller);
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  AssetDatabase.SaveAssets();MarioDemoSetup.AddPound();Directory.CreateDirectory("Temp/MarioMotionAudit");File.WriteAllText("Temp/MarioMotionAudit/repair.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
 }
}
