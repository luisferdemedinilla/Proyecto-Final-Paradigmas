using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class BrickTools
{
 public const string Art="Assets/_Project/Art/Items/HelloMario/Block/";
 public const string Originals="Assets/_Project/Prefabs/Items/HelloMario/Block/";
 [MenuItem("Tools/Brick/Audit Assets")]
 public static void Audit()
 {
  Directory.CreateDirectory("Temp/BrickAudit");var rows=new System.Collections.Generic.List<object>();
  foreach(var name in new[]{"Brick","Brick Break"}){var go=AssetDatabase.LoadAssetAtPath<GameObject>(Originals+name+".prefab");rows.Add(new{name,transforms=go.GetComponentsInChildren<Transform>(true).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t,go.transform),position=t.localPosition.ToString(),scale=t.localScale.ToString()}),renderers=go.GetComponentsInChildren<Renderer>(true).Select(r=>new{r.name,type=r.GetType().Name,materials=r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m))})});}
  foreach(var name in new[]{"Break","BreakHard","Reaction","ReactionHipDrop","Wait"}){var path=Art+"Animations/"+name+".fbx";var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview"))){rows.Add(new{name,clip.length,bindings=AnimationUtility.GetCurveBindings(clip).Select(b=>new{b.path,b.propertyName,first=AnimationUtility.GetEditorCurve(clip,b).Evaluate(0),last=AnimationUtility.GetEditorCurve(clip,b).Evaluate(clip.length)}),scale=((ModelImporter)AssetImporter.GetAtPath(path)).globalScale});}}
  File.WriteAllText("Temp/BrickAudit/assets.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
 }

 public const string Gameplay="Assets/_Project/Prefabs/Items/Gameplay/Brick.prefab";
 [MenuItem("Tools/Brick/Create Playable Prefab")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
  if(AssetDatabase.LoadAssetAtPath<GameObject>(Gameplay)!=null)throw new System.Exception("Brick gameplay prefab already exists; inspect before updating");
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
  try{
   var root=new GameObject("Brick");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
   var intact=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Originals+"Brick.prefab"),scene);intact.name="Intact";intact.transform.SetParent(root.transform,false);
   var fragments=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Originals+"Brick Break.prefab"),scene);fragments.name="Fragments";fragments.transform.SetParent(root.transform,false);
   var source=AssetDatabase.LoadAllAssetsAtPath(Art+"Animations/Break.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
   var bindings=AnimationUtility.GetCurveBindings(source);var clip=new AnimationClip{name="BrickBreak",frameRate=60};
   float Read(string path,string property,float t,float fallback){var binding=bindings.FirstOrDefault(b=>b.path==path && b.propertyName==property);if(binding.type==null)return fallback;return AnimationUtility.GetEditorCurve(source,binding).Evaluate(t);}
   Quaternion Rotation(string path,float t){return new Quaternion(Read(path,"m_LocalRotation.x",t,0),Read(path,"m_LocalRotation.y",t,0),Read(path,"m_LocalRotation.z",t,0),Read(path,"m_LocalRotation.w",t,1));}
   int movingPositionCurves=0;
   foreach(var b in bindings.Where(b=>b.propertyName.StartsWith("m_LocalPosition"))){var c=AnimationUtility.GetEditorCurve(source,b);if(c.keys.Max(k=>k.value)-c.keys.Min(k=>k.value)>.001f)movingPositionCurves++;}
   foreach(var bone in fragments.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("BlockBrick") && !t.name.Contains("__")))
   {
    string path=AnimationUtility.CalculateTransformPath(bone,fragments.transform);var position=bone.localPosition;var rotation=bone.localRotation;var curves=Enumerable.Range(0,10).Select(i=>new AnimationCurve()).ToArray();
    int index=int.Parse(bone.name.Substring("BlockBrick".Length));var outward=new Vector3(Mathf.Abs(position.x)>.05f?Mathf.Sign(position.x):index%2==0?-1:1,0,position.z>=0?1:-1);
    for(int f=0;f<=88;f++){
     float t=source.length*f/88;var q=rotation*Quaternion.Inverse(Rotation(path,0))*Rotation(path,t);
     var p=position+new Vector3(outward.x*(1.8f+index*.09f)*t,3.4f*t-4.9f*t*t,outward.z*.45f*t);
     float shrink=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(source.length-.3f,source.length,t));
     for(int axis=0;axis<3;axis++)curves[axis].AddKey(t,p[axis]);for(int axis=0;axis<4;axis++)curves[3+axis].AddKey(t,q[axis]);for(int axis=0;axis<3;axis++)curves[7+axis].AddKey(t,bone.localScale[axis]*shrink);
    }
    for(int axis=0;axis<3;axis++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalPosition."+"xyz"[axis]),curves[axis]);
    for(int axis=0;axis<4;axis++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[axis]),curves[3+axis]);
    for(int axis=0;axis<3;axis++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalScale."+"xyz"[axis]),curves[7+axis]);
   }
   clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
   var folder=Art+"Animations/Gameplay";HelloMarioLibrary.Folder(folder);AssetDatabase.CreateAsset(clip,folder+"/BrickBreak.anim");
   var controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(folder+"/BrickBreak.controller");controller.layers[0].stateMachine.AddState("Break").motion=clip;
   var animator=fragments.GetComponent<Animator>();if(animator==null)animator=fragments.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   foreach(var skin in fragments.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
   fragments.SetActive(false);var bounds=HelloMarioPlayableBuilder.BoundsOf(intact);var collider=root.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;
   var brick=root.AddComponent<BreakableBrick>();var so=new SerializedObject(brick);so.FindProperty("intact").objectReferenceValue=intact;so.FindProperty("fragments").objectReferenceValue=fragments;so.FindProperty("destructionAnimator").objectReferenceValue=animator;so.FindProperty("destructionDuration").floatValue=clip.length;so.ApplyModifiedPropertiesWithoutUndo();
   PrefabUtility.SaveAsPrefabAsset(root,Gameplay);AssetDatabase.SaveAssets();Directory.CreateDirectory("Temp/BrickAudit");File.WriteAllText("Temp/BrickAudit/build.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{source.length,movingPositionCurves,colliderSize=collider.size.ToString(),clipBindings=AnimationUtility.GetCurveBindings(clip).Length}));
  }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
  Adapt();
 }

 [MenuItem("Tools/Brick/Adapt And Preview Destruction")]
 public static void Adapt()
 {
  var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"Animations/Gameplay/BrickBreak.anim");
  var source=AssetDatabase.LoadAllAssetsAtPath(Art+"Animations/Break.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Originals+"Brick Break.prefab");
  foreach(var binding in AnimationUtility.GetCurveBindings(source).Where(b=>b.path.StartsWith("AllRoot/BlockBrick") && b.propertyName.StartsWith("m_LocalPosition")))
  {
   var bone=prefab.transform.Find(binding.path);var curve=AnimationUtility.GetEditorCurve(source,binding);int axis="xyz".IndexOf(binding.propertyName.Last());var keys=curve.keys;float start=curve.Evaluate(0);
   for(int i=0;i<keys.Length;i++){keys[i].value=bone.localPosition[axis]+(keys[i].value-start)*.4f;keys[i].inTangent*=.4f;keys[i].outTangent*=.4f;}
   AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys));
  }
  EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();Directory.CreateDirectory("Temp/BrickAudit");
  for(int f=0;f<3;f++){
   var preview=new PreviewRenderUtility();try{var root=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(root);clip.SampleAnimation(root,f*.25f);preview.camera.transform.position=new Vector3(0,2,-6);preview.camera.transform.LookAt(new Vector3(0,.7f,0));preview.camera.fieldOfView=40;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=30;preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.2f,.23f,.28f);preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(45,20,0);preview.ambientColor=Color.gray;foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;var image=MarioAnimationValidation.Render(preview,512);File.WriteAllBytes("Temp/BrickAudit/break-"+f+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}finally{preview.Cleanup();}
  }
 }

 [MenuItem("Tools/Brick/Create Hard Brick Prefab")]
 public static void BuildHard()
 {
  if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
  const string path="Assets/_Project/Prefabs/Items/Gameplay/BrickHard.prefab";
  if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)throw new System.Exception("BrickHard already exists; inspect before replacing");
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
  try{
   var root=new GameObject("BrickHard");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Originals+"Brick Hard.prefab"),scene);model.name="Visual";model.transform.SetParent(root.transform,false);
   if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new System.Exception("Source already has a collider");
   var bounds=HelloMarioPlayableBuilder.BoundsOf(root);if(bounds.size.sqrMagnitude<.01f)throw new System.Exception("Invalid model bounds");
   var box=root.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;box.isTrigger=false;
   PrefabUtility.SaveAsPrefabAsset(root,path);AssetDatabase.SaveAssets();
   var saved=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   if(saved.GetComponentsInChildren<Collider>(true).Length!=1 || saved.GetComponent<BoxCollider>().isTrigger || saved.GetComponentsInChildren<BreakableBrick>(true).Length!=0 || saved.GetComponentsInChildren<Rigidbody>(true).Length!=0)throw new System.Exception("Unexpected hard block components");
   Directory.CreateDirectory("Temp/BrickAudit");File.WriteAllText("Temp/BrickAudit/hard.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{path,verified=true,center=box.center.ToString(),size=box.size.ToString(),colliders=1}));
  }finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 }
}
