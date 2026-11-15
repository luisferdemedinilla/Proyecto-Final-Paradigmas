using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class FlagGoalTools
{
 public const string Art="Assets/_Project/Art/Items/HelloMario/Goal/";
 public const string Originals="Assets/_Project/Prefabs/Items/HelloMario/Goal/";
 [MenuItem("Tools/Flag Goal/Audit Assets")]
 public static void Audit(){Directory.CreateDirectory("Temp/FlagGoalAudit");var rows=new System.Collections.Generic.List<object>();foreach(var name in new[]{"Goal Pole","Goal Flag"}){var go=AssetDatabase.LoadAssetAtPath<GameObject>(Originals+name+".prefab");rows.Add(new{name,bounds=HelloMarioPlayableBuilder.BoundsOf(go).ToString(),transforms=go.GetComponentsInChildren<Transform>(true).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t,go.transform),position=t.localPosition.ToString(),scale=t.localScale.ToString()}),renderers=go.GetComponentsInChildren<Renderer>(true).Select(r=>new{r.name,r.enabled,active=r.gameObject.activeSelf})});}
 foreach(var path in new[]{Art+"Animations/GoalHigh.fbx",Art+"Animations/GoalLow.fbx",Art+"AnimFlag/Wait.fbx","Assets/_Project/Art/Characters/Shared/HelloMario/Animations/Source/GoalPoleWait.fbx","Assets/_Project/Art/Characters/Shared/HelloMario/Animations/Source/GoalPoleFall.fbx","Assets/_Project/Art/Characters/Shared/HelloMario/Animations/Source/GoalPose.fbx"}){var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));rows.Add(new{path,clip.length,bindings=AnimationUtility.GetCurveBindings(clip).Select(b=>new{b.path,b.propertyName,start=AnimationUtility.GetEditorCurve(clip,b).Evaluate(0),end=AnimationUtility.GetEditorCurve(clip,b).Evaluate(clip.length),min=AnimationUtility.GetEditorCurve(clip,b).keys.Min(k=>k.value),max=AnimationUtility.GetEditorCurve(clip,b).keys.Max(k=>k.value)})});}
 File.WriteAllText("Temp/FlagGoalAudit/assets.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));}

 public const string PrefabPath="Assets/_Project/Prefabs/Items/Gameplay/FlagGoal.prefab";
 static AnimationClip ModelClip(string sourcePath,GameObject model,string destination,bool loop)
 {
  var source=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));var bindings=AnimationUtility.GetCurveBindings(source);var clip=new AnimationClip{name=Path.GetFileNameWithoutExtension(destination),frameRate=30};
  foreach(var path in bindings.Select(b=>b.path).Distinct()){
   var target=string.IsNullOrEmpty(path)?model.transform:model.transform.Find(path);if(target==null)throw new System.Exception("Unbound goal animation: "+path);
   float Read(string property,float t,float fallback){var b=bindings.FirstOrDefault(x=>x.path==path && x.propertyName==property);return b.type==null?fallback:AnimationUtility.GetEditorCurve(source,b).Evaluate(t);}
   Quaternion Q(float t)=>new Quaternion(Read("m_LocalRotation.x",t,0),Read("m_LocalRotation.y",t,0),Read("m_LocalRotation.z",t,0),Read("m_LocalRotation.w",t,1));
   var curves=Enumerable.Range(0,10).Select(x=>new AnimationCurve()).ToArray();int frames=Mathf.CeilToInt(source.length*30);
   for(int f=0;f<=frames;f++){float t=source.length*f/frames;var q=target.localRotation*Quaternion.Inverse(Q(0))*Q(t);for(int axis=0;axis<4;axis++)curves[axis].AddKey(t,q[axis]);for(int axis=0;axis<3;axis++){curves[4+axis].AddKey(t,target.localPosition[axis]);string property="m_LocalScale."+"xyz"[axis];float first=Read(property,0,1);curves[7+axis].AddKey(t,target.localScale[axis]*(Mathf.Abs(first)<.001f?1:Read(property,t,1)/first));}}
   for(int axis=0;axis<4;axis++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[axis]),curves[axis]);for(int axis=0;axis<3;axis++){AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalPosition."+"xyz"[axis]),curves[4+axis]);AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalScale."+"xyz"[axis]),curves[7+axis]);}
  }
  clip.EnsureQuaternionContinuity();var options=AnimationUtility.GetAnimationClipSettings(clip);options.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,options);AssetDatabase.CreateAsset(clip,destination);return clip;
 }
 [MenuItem("Tools/Flag Goal/Create Prefab And Animations")]
 public static void Build()
 {
  if(EditorApplication.isPlaying || File.Exists(PrefabPath))throw new System.Exception("Exit Play Mode; inspect existing prefab before replacing");
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var reports=new System.Collections.Generic.List<object>();
  try{
   string folder=Art+"Animations/Gameplay";HelloMarioLibrary.Folder(folder);
   var root=new GameObject("FlagGoal");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
   var pole=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Originals+"Goal Pole.prefab"),scene);pole.name="Pole";pole.transform.SetParent(root.transform,false);pole.transform.localScale=Vector3.one*.65f;var bounds=HelloMarioPlayableBuilder.BoundsOf(pole);pole.transform.localPosition-=Vector3.up*bounds.min.y;
   var carriage=new GameObject("FlagCarriage").transform;carriage.SetParent(root.transform,false);carriage.localPosition=new Vector3(0,5.05f,0);
   var flag=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Originals+"Goal Flag.prefab"),scene);flag.name="Flag";flag.transform.SetParent(carriage,false);flag.transform.localScale=Vector3.one*.65f;
   var poleClip=ModelClip(Art+"Animations/GoalLow.fbx",pole,folder+"/PoleFinish.anim",false);var wave=ModelClip(Art+"AnimFlag/Wait.fbx",flag,folder+"/FlagWave.anim",true);
   var pc=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(folder+"/Pole.controller");pc.layers[0].stateMachine.AddState("Idle");pc.layers[0].stateMachine.AddState("Finish").motion=poleClip;
   var fc=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(folder+"/Flag.controller");fc.layers[0].stateMachine.AddState("Wave").motion=wave;
   var pa=pole.GetComponent<Animator>();if(pa==null)pa=pole.AddComponent<Animator>();pa.runtimeAnimatorController=pc;pa.applyRootMotion=false;
   var fa=flag.GetComponent<Animator>();if(fa==null)fa=flag.AddComponent<Animator>();fa.runtimeAnimatorController=fc;fa.applyRootMotion=false;
   foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=true;
   var trigger=root.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=new Vector3(0,2.7f,0);trigger.size=new Vector3(.25f,5.4f,1.2f);
   var goal=root.AddComponent<FlagGoal>();var so=new SerializedObject(goal);so.FindProperty("flagCarriage").objectReferenceValue=carriage;so.FindProperty("poleAnimator").objectReferenceValue=pa;var profiles=so.FindProperty("characters");profiles.arraySize=3;
   int index=0;foreach(var name in new[]{"Mario","Luigi","Toad"}){
    var player=PrefabUtility.LoadPrefabContents(HelloMarioPlayableBuilder.Playables+name+"Player.prefab");try{
     var profile=profiles.GetArrayElementAtIndex(index++);profile.FindPropertyRelative("controller").objectReferenceValue=player.GetComponent<Animator>().runtimeAnimatorController;
     var dest="Assets/_Project/Art/Characters/"+name+"/HelloMario/Animations/Goal";HelloMarioLibrary.Folder(dest);
     foreach(var pair in new[]{new[]{"hold","GoalPoleWait"},new[]{"slide","GoalPoleFall"},new[]{"celebrate","GoalPose"}}){var clip=HelloMarioPlayableBuilder.ConvertClip(player,player.transform.Find("Visual/Model").gameObject,"Idle",pair[1],dest+"/"+pair[1]+".anim",reports);clip.name=pair[1];var options=AnimationUtility.GetAnimationClipSettings(clip);options.loopTime=pair[0]!="celebrate";AnimationUtility.SetAnimationClipSettings(clip,options);profile.FindPropertyRelative(pair[0]).objectReferenceValue=clip;}
    }finally{PrefabUtility.UnloadPrefabContents(player);}
   }
   so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();Directory.CreateDirectory("Temp/FlagGoalAudit");File.WriteAllText("Temp/FlagGoalAudit/clips.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
  }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }

 [MenuItem("Tools/Flag Goal/Polish Goal Framing")]
 public static void Polish(){if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");var root=PrefabUtility.LoadPrefabContents(PrefabPath);try{var goal=root.GetComponent<FlagGoal>();var baseMesh=root.transform.Find("Pole/GoalPole__GoalPoleBrickMat00").gameObject;float height=HelloMarioPlayableBuilder.BoundsOf(baseMesh).max.y;var so=new SerializedObject(goal);so.FindProperty("landingHeight").floatValue=height;so.FindProperty("loweredFlagHeight").floatValue=height+.5f;so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);}finally{PrefabUtility.UnloadPrefabContents(root);}var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=FlagLevelSetup.ScenePath)throw new System.Exception("Expected flag level");var camera=UnityEngine.Object.FindAnyObjectByType<MarioFollowCamera>();var data=new SerializedObject(camera);data.FindProperty("followVertical").boolValue=false;data.ApplyModifiedPropertiesWithoutUndo();camera.transform.position=new Vector3(camera.transform.position.x,3.6f,camera.transform.position.z);var level=UnityEngine.Object.FindAnyObjectByType<FlagLevelController>();var panel=(GameObject)new SerializedObject(level).FindProperty("resultPanel").objectReferenceValue;var rect=panel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.59f,.48f);rect.anchorMax=new Vector2(.99f,.86f);rect.offsetMin=rect.offsetMax=Vector2.zero;var retry=panel.GetComponentInChildren<UnityEngine.UI.Button>(true).GetComponent<RectTransform>();retry.anchorMin=new Vector2(.1f,.08f);retry.anchorMax=new Vector2(.9f,.31f);retry.GetComponentInChildren<UnityEngine.UI.Text>(true).fontSize=20;UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
}
