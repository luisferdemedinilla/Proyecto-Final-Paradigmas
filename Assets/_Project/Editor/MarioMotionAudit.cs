using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class MarioMotionAudit
{
 [MenuItem("Tools/Mario Demo/Audit Motion Poses")]
 public static void Audit()
 {
  Directory.CreateDirectory("Temp/MarioMotionAudit");
  foreach(var name in new[]{"Run","GroundPoundStart","GroundPoundFall","GroundPoundLand"})
  {
   var preview=new PreviewRenderUtility();
   try {
    var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HelloMarioPlayableBuilder.Playables+"MarioPlayer.prefab"));preview.AddSingleGO(root);
    UnityEngine.Object.DestroyImmediate(root.GetComponent<MarioPlayerController>());UnityEngine.Object.DestroyImmediate(root.GetComponent<Rigidbody>());root.GetComponent<Animator>().runtimeAnimatorController=null;
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HelloMarioPlayableBuilder.AnimFolder("Mario")+name+".anim");
    for(int f=0;f<3;f++){clip.SampleAnimation(root,clip.length*(f+.25f)/3);MarioAnimationValidation.ConfigureCamera(preview,65);var image=MarioAnimationValidation.Render(preview,384);File.WriteAllBytes("Temp/MarioMotionAudit/"+name+f+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
   }finally{preview.Cleanup();}
  }
  var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(HelloMarioPlayableBuilder.AnimFolder("Mario")+"MarioGameplay.controller");
  File.WriteAllText("Temp/MarioMotionAudit/controller.json",Newtonsoft.Json.JsonConvert.SerializeObject(controller.layers[0].stateMachine.states.Select(s=>new{s.state.name,motion=AssetDatabase.GetAssetPath(s.state.motion),transitions=s.state.transitions.Select(t=>new{target=t.destinationState.name,conditions=t.conditions.Select(c=>new{c.parameter,c.mode,c.threshold})})}),Newtonsoft.Json.Formatting.Indented));
 }
}
