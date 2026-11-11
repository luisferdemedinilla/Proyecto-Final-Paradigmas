using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class MarioDemoSetup
{
 public const string ScenePath="Assets/_Project/Scenes/MarioFirstLevel.unity";
 public const string EnemyPath="Assets/_Project/Prefabs/Enemies/Gameplay/Goomba.prefab";
 public const string CoinPath="Assets/_Project/Prefabs/Items/Gameplay/GoldCoin.prefab";
 const string Art="Assets/_Project/Art/Environment/MarioFirstLevel";
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Set(UnityEngine.Object obj,string name,UnityEngine.Object value){var so=new SerializedObject(obj);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static void Number(UnityEngine.Object obj,string name,float value){var so=new SerializedObject(obj);so.FindProperty(name).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static void Bool(UnityEngine.Object obj,string name,bool value){var so=new SerializedObject(obj);so.FindProperty(name).boolValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 [MenuItem("Tools/Mario Demo/Add Ground Pound Animations")]
 public static void AddPound()
 {
  Check(!EditorApplication.isPlaying,"Exit Play Mode first");Directory.CreateDirectory("Temp/MarioDemoAudit");var reports=new List<object>();
  foreach(var name in new[]{"Mario","Luigi","Toad"})
  {
   var prefab=HelloMarioPlayableBuilder.Playables+name+"Player.prefab";var root=PrefabUtility.LoadPrefabContents(prefab);
   try
   {
    var model=root.transform.Find("Visual/Model").gameObject;
    var folder=HelloMarioPlayableBuilder.AnimFolder(name);var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+name+"Gameplay.controller");
    var machine=controller.layers[0].stateMachine;
    if(!controller.parameters.Any(p=>p.name=="GroundPounding"))controller.AddParameter("GroundPounding",AnimatorControllerParameterType.Bool);
    foreach(var n in new[]{"PoundStartSpeed","PoundLandSpeed"})if(!controller.parameters.Any(p=>p.name==n))controller.AddParameter(n,AnimatorControllerParameterType.Float);
    foreach(var t in machine.anyStateTransitions)if(!t.conditions.Any(c=>c.parameter=="GroundPounding"))t.AddCondition(AnimatorConditionMode.IfNot,0,"GroundPounding");
    var map=new Dictionary<string,string>{{"GroundPoundStart","HipDropStart"},{"GroundPoundFall","HipDrop"},{"GroundPoundLand","HipDropLand"}};
    foreach(var pair in map)
    {
     var clip=HelloMarioPlayableBuilder.ConvertClip(root,model,pair.Key,pair.Value,folder+pair.Key+".anim",reports);
     var options=AnimationUtility.GetAnimationClipSettings(clip);options.loopTime=pair.Key=="GroundPoundFall";AnimationUtility.SetAnimationClipSettings(clip,options);
     var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==pair.Key);
     if(state==null)
     {
      state=machine.AddState(pair.Key);state.writeDefaultValues=false;
      foreach(var target in new[]{"Idle","Fall"}){var next=machine.states.Single(s=>s.state.name==target).state;var t=state.AddTransition(next);t.hasExitTime=false;t.duration=.06f;t.AddCondition(AnimatorConditionMode.IfNot,0,"GroundPounding");t.AddCondition(target=="Idle"?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Grounded");}
     }
     state.motion=clip;
     if(pair.Key!="GroundPoundFall"){state.speedParameterActive=true;state.speedParameter=pair.Key=="GroundPoundStart"?"PoundStartSpeed":"PoundLandSpeed";Number(root.GetComponent<MarioPlayerController>(),pair.Key=="GroundPoundStart"?"groundPoundStartClipLength":"groundPoundLandClipLength",clip.length);}
    }
    AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"Idle.anim").SampleAnimation(root,0);
    PrefabUtility.SaveAsPrefabAsset(root,prefab);EditorUtility.SetDirty(controller);
   }
   finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  AssetDatabase.SaveAssets();File.WriteAllText("Temp/MarioDemoAudit/pound-clips.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));Debug.Log("Ground pound added to Mario, Luigi and Toad.");
 }
 static AnimationClip EnemyClip(GameObject go,string sourceName,string name,bool loop)
 {
  string source="Assets/_Project/Art/Characters/Enemies/HelloMario/Goomba/Animations/"+sourceName+".fbx";
  var original=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
  var clip=new AnimationClip{name=name,frameRate=30};
  foreach(var binding in AnimationUtility.GetCurveBindings(original))
  {
   if(binding.path==""||binding.path=="nw4f_root"||binding.path.StartsWith("nw4f_root/Kuribo"))continue;
   var target=go.transform.Find("Visual/"+binding.path);if(target==null)throw new Exception("Missing Goomba bone "+binding.path);
   if(binding.type!=typeof(Transform))continue;
   var curve=AnimationUtility.GetEditorCurve(original,binding);var keys=curve.keys;
   if(binding.propertyName.StartsWith("m_LocalPosition"))for(int i=0;i<keys.Length;i++){keys[i].value*=.4f;keys[i].inTangent*=.4f;keys[i].outTangent*=.4f;}
   if(binding.path=="nw4f_root/AllRoot" && binding.propertyName.StartsWith("m_LocalPosition"))curve=AnimationCurve.Constant(0,original.length,0);else curve=new AnimationCurve(keys);
   var b=binding;b.path="Visual/"+binding.path;AnimationUtility.SetEditorCurve(clip,b,curve);
  }
  clip.EnsureQuaternionContinuity();var options=AnimationUtility.GetAnimationClipSettings(clip);options.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,options);
  const string folder="Assets/_Project/Art/Characters/Enemies/HelloMario/Goomba/Animations/Gameplay";HelloMarioLibrary.Folder(folder);AssetDatabase.CreateAsset(clip,folder+"/"+name+".anim");return clip;
 }
 static GameObject Instantiate(string path,Scene scene,Transform parent=null){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;return go;}
 [MenuItem("Tools/Mario Demo/Create Gameplay Prefabs")]
 public static void Prefabs()
 {
  Check(!File.Exists(EnemyPath)&&!File.Exists(CoinPath),"Gameplay prefabs already exist; inspect before replacing");
  HelloMarioLibrary.Folder(Path.GetDirectoryName(EnemyPath));HelloMarioLibrary.Folder(Path.GetDirectoryName(CoinPath));
  var scene=EditorSceneManager.NewPreviewScene();
  try
  {
   var go=new GameObject("Goomba");SceneManager.MoveGameObjectToScene(go,scene);
   var visual=Instantiate("Assets/_Project/Prefabs/Enemies/HelloMario/Goomba/Goomba.prefab",scene,go.transform);visual.name="Visual";visual.transform.localScale=Vector3.one*.64f;
   var bounds=HelloMarioPlayableBuilder.BoundsOf(go);visual.transform.localPosition-=Vector3.up*bounds.min.y;bounds=HelloMarioPlayableBuilder.BoundsOf(go);
   var collider=go.AddComponent<BoxCollider>();collider.size=new Vector3(.7f,bounds.size.y*.9f,.7f);collider.center=new Vector3(0,collider.size.y/2,0);
   var body=go.AddComponent<Rigidbody>();body.constraints=RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
   var animator=go.AddComponent<Animator>();animator.applyRootMotion=false;
   var walk=EnemyClip(go,"Walk","Walk",true);var run=EnemyClip(go,"Run","Chase",true);var defeated=EnemyClip(go,"PressDown","Defeated",false);
   var controller=AnimatorController.CreateAnimatorControllerAtPath("Assets/_Project/Art/Characters/Enemies/HelloMario/Goomba/Animations/Gameplay/Goomba.controller");controller.AddParameter("Chasing",AnimatorControllerParameterType.Bool);
   var sm=controller.layers[0].stateMachine;var a=sm.AddState("Walk");a.motion=walk;a.speed=2;var b=sm.AddState("Chase");b.motion=run;var d=sm.AddState("Defeated");d.motion=defeated;d.speed=defeated.length/.45f;sm.defaultState=a;
   var ab=a.AddTransition(b);ab.hasExitTime=false;ab.duration=.1f;ab.AddCondition(AnimatorConditionMode.If,0,"Chasing");var ba=b.AddTransition(a);ba.hasExitTime=false;ba.duration=.1f;ba.AddCondition(AnimatorConditionMode.IfNot,0,"Chasing");
   animator.runtimeAnimatorController=controller;var enemy=go.AddComponent<GoombaEnemy>();Set(enemy,"visual",visual.transform);PrefabUtility.SaveAsPrefabAsset(go,EnemyPath);UnityEngine.Object.DestroyImmediate(go);
   var coin=new GameObject("GoldCoin");SceneManager.MoveGameObjectToScene(coin,scene);var cv=Instantiate("Assets/_Project/Prefabs/Items/HelloMario/Coin/Coin.prefab",scene,coin.transform);cv.name="Visual";
   var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Items/HelloMario/Coin/Materials/CoinMat00.mat");gold.SetColor("_BaseColor",new Color(1,.76f,.035f));gold.SetFloat("_Metallic",.3f);gold.SetFloat("_Smoothness",.5f);gold.SetColor("_EmissionColor",new Color(.16f,.09f,0));gold.EnableKeyword("_EMISSION");EditorUtility.SetDirty(gold);
   var trigger=coin.AddComponent<SphereCollider>();trigger.radius=.5f;trigger.isTrigger=true;var collectible=coin.AddComponent<CollectibleCoin>();Set(collectible,"visual",cv.transform);PrefabUtility.SaveAsPrefabAsset(coin,CoinPath);UnityEngine.Object.DestroyImmediate(coin);
   AssetDatabase.SaveAssets();Debug.Log("Animated Goomba and reusable golden coin prefabs created.");
  }
  finally{EditorSceneManager.ClosePreviewScene(scene);}
 }
 static Transform Group(string name,Scene scene,Transform parent=null){var g=new GameObject(name);SceneManager.MoveGameObjectToScene(g,scene);g.transform.SetParent(parent,false);return g.transform;}
 static Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.1f);AssetDatabase.CreateAsset(m,Art+"/"+name+".mat");return m;}
 static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 size,Material mat,bool solid=true){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
 static UnityEngine.UI.Text Label(string name,Transform parent,string text,int size,Vector2 min,Vector2 max,Color color)
 {
  var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Text));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(18,4);rt.offsetMax=new Vector2(-18,-4);
  var label=go.GetComponent<UnityEngine.UI.Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=size;label.color=color;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;
 }
 static GameObject Panel(string name,Transform parent,Vector2 min,Vector2 max,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;go.GetComponent<UnityEngine.UI.Image>().color=color;return go;}
 [MenuItem("Tools/Mario Demo/Create Playable Level")]
 public static void Level()
 {
  Check(!EditorApplication.isPlaying&&!File.Exists(ScenePath),"Level exists or Play Mode active; inspect first");
  HelloMarioLibrary.Folder(Art);var oldScenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  var environment=Group("Environment",scene);var blocks=Group("Blocks",scene,environment);var scenery=Group("Background",scene,environment);var actors=Group("Actors",scene);var items=Group("Items",scene);var system=Group("Level",scene);
  var grass=Mat("Grass",new Color(.24f,.65f,.15f));var dirt=Mat("Earth",new Color(.47f,.26f,.11f));var hill=Mat("Hills",new Color(.28f,.64f,.39f));var farHill=Mat("DistantHills",new Color(.42f,.75f,.63f));
  void Block(float x,float y)
  {
   var go=Instantiate("Assets/_Project/Prefabs/Items/HelloMario/Block/Brick.prefab",scene,blocks);go.name="Brick "+x+" "+y;go.transform.localScale=new Vector3(.96f,.96f,2.8f);go.transform.position=new Vector3(x,y-.49f,0);var box=go.AddComponent<BoxCollider>();box.center=new Vector3(0,.51f,0);box.size=new Vector3(1.04f,1.04f,1.04f);
  }
  for(int x=-3;x<=28;x++){if(x==8||x==9)continue;Block(x,-.5f);Cube("Grass Edge",blocks,new Vector3(x,.015f,0),new Vector3(1,.04f,2.94f),grass,false);}
  foreach(var x in new[]{4,5})Block(x,.5f);
  foreach(var x in new[]{21,22,23,24,25,26})Block(x,.5f);
  foreach(var x in new[]{23,24,25,26})Block(x,1.5f);
  Cube("Start Wall",blocks,new Vector3(-3.8f,1.2f,0),new Vector3(.4f,3.5f,3),dirt);
  Cube("End Wall",blocks,new Vector3(29,1.2f,0),new Vector3(.4f,3.5f,3),dirt);
  for(int i=0;i<6;i++){var h=GameObject.CreatePrimitive(PrimitiveType.Sphere);h.name="Hill "+i;h.transform.SetParent(scenery,false);h.transform.position=new Vector3(i*7-4,-2,8+i%2*4);h.transform.localScale=new Vector3(11,9+i%2*3,2);h.GetComponent<Renderer>().sharedMaterial=i%2==0?hill:farHill;UnityEngine.Object.DestroyImmediate(h.GetComponent<Collider>());}
  var spawn=Group("Player Spawn",scene,system);spawn.position=new Vector3(0,.12f,0);
  var p=Instantiate(HelloMarioPlayableBuilder.Playables+"MarioPlayer.prefab",scene,actors);p.name="Mario";p.transform.position=spawn.position;var player=p.GetComponent<MarioPlayerController>();Set(player,"spawnPoint",spawn);Bool(player,"respawnWhenFalling",false);
  var enemy=Instantiate(EnemyPath,scene,actors);enemy.transform.position=new Vector3(14,.1f,0);var goomba=enemy.GetComponent<GoombaEnemy>();Set(goomba,"target",player);
  var coin=Instantiate(CoinPath,scene,items);coin.name="Goal Coin";coin.transform.position=new Vector3(25,2.8f,0);
  var cameras=Group("Cameras",scene);var cameraObject=Group("Main Camera",scene,cameras).gameObject;cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.46f,.75f,.94f);camera.orthographic=true;camera.orthographicSize=4.5f;camera.transform.position=new Vector3(3,2.7f,-25);cameraObject.AddComponent<AudioListener>();var follow=cameraObject.AddComponent<MarioFollowCamera>();Set(follow,"target",p.transform);
  var light=Group("Sun",scene).gameObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(45,-35,0);RenderSettings.ambientLight=new Color(.68f,.72f,.8f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
  var level=system.gameObject.AddComponent<DemoLevelController>();Set(level,"player",player);Set(level,"goomba",goomba);Set(level,"goalCoin",coin.GetComponent<CollectibleCoin>());
  var canvasObject=Group("HUD",scene).gameObject;var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=10;
  var scaler=canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
  var header=Panel("Objective",canvas.transform,new Vector2(0,.88f),Vector2.one,new Color(.035f,.09f,.18f,.93f));Label("Title",header.transform,"PRIMER RETO  ·  Consigue la moneda dorada",27,Vector2.zero,Vector2.one,Color.white);
  var footer=Panel("Controls",canvas.transform,Vector2.zero,new Vector2(1,.09f),new Color(.035f,.09f,.18f,.88f));Label("Controls Text",footer.transform,"A / D  mover     W / ESPACIO  saltar     S en el aire  golpe hacia abajo",20,Vector2.zero,Vector2.one,Color.white);
  var result=Panel("Result",canvas.transform,new Vector2(.2f,.28f),new Vector2(.8f,.72f),new Color(.025f,.055f,.12f,.97f));var title=Label("Result Title",result.transform,"",34,new Vector2(0,.64f),new Vector2(1,.93f),new Color(1,.8f,.15f));var message=Label("Result Message",result.transform,"",23,new Vector2(0,.36f),new Vector2(1,.65f),Color.white);
  var buttonObject=Panel("Retry",result.transform,new Vector2(.27f,.08f),new Vector2(.73f,.31f),new Color(.15f,.5f,.9f));var button=buttonObject.AddComponent<UnityEngine.UI.Button>();Label("Retry Text",buttonObject.transform,"REINTENTAR  ·  R",22,Vector2.zero,Vector2.one,Color.white);UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,level.RestartLevel);
  Set(level,"resultPanel",result);Set(level,"resultTitle",title);Set(level,"resultMessage",message);result.SetActive(false);
  var events=Group("EventSystem",scene).gameObject;events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
  EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();foreach(var old in oldScenes)if(old.IsValid()&&!old.isDirty)EditorSceneManager.CloseScene(old,true);
  Debug.Log("MarioFirstLevel created and opened. The goal coin's win condition belongs only to its level controller.");
 }
}
