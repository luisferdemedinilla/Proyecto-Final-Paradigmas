using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
public static class FlagLevelSetup
{
 public const string ScenePath="Assets/_Project/Scenes/MarioFlagLevel.unity";
 static Transform Group(string name,Scene scene,Transform parent=null){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);if(parent!=null)go.transform.SetParent(parent,false);return go.transform;}
 static GameObject Instance(string path,Scene scene,Transform parent){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);go.transform.SetParent(parent,false);return go;}
 static void Set(UnityEngine.Object target,string name,UnityEngine.Object value)=>MarioDemoSetup.Set(target,name,value);
 static void Number(UnityEngine.Object target,string name,float value){var so=new SerializedObject(target);so.FindProperty(name).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static UnityEngine.UI.Text Label(string name,Transform parent,string text,int size,Vector2 min,Vector2 max,Color color)
 {
  var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Text));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=new Vector2(18,4);rt.offsetMax=new Vector2(-18,-4);
  var label=go.GetComponent<UnityEngine.UI.Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=size;label.color=color;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;
 }
 static GameObject Panel(string name,Transform parent,Vector2 min,Vector2 max,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;go.GetComponent<UnityEngine.UI.Image>().color=color;return go;}

 [MenuItem("Tools/Flag Goal/Create Level")]
 public static void Build()
 {
  if(EditorApplication.isPlaying || File.Exists(ScenePath))throw new Exception("Exit Play Mode; inspect existing scene before replacing");
  var oldScenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  var environment=Group("Environment",scene);var actors=Group("Actors",scene);var items=Group("Items",scene);var system=Group("Level",scene);
  var names=new[]{"BlockEmpty","Brick","BrickHard","BlockQuestion"};var heights=new[]{0f,.8f,.2f,1f};
  for(int platform=0;platform<4;platform++){
   var parent=Group("Platform "+(platform+1)+" - "+names[platform]+" (8)",scene,environment);
   for(int i=0;i<8;i++){
    var block=Instance(QuestionBlockTools.Folder+names[platform]+".prefab",scene,parent);block.name=names[platform]+" "+(i+1);var box=block.GetComponent<BoxCollider>();float scale=1/box.size.x;block.transform.localScale=Vector3.one*scale;block.transform.position=new Vector3(platform*10+i,heights[platform]-(box.center.y+box.size.y/2)*scale,0);
   }
  }
  var spawn=Group("Player Spawn",scene,system);spawn.position=new Vector3(1,.15f,0);
  var p=Instance(HelloMarioPlayableBuilder.Playables+"MarioPlayer.prefab",scene,actors);p.name="Mario";p.transform.position=spawn.position;var player=p.GetComponent<MarioPlayerController>();Set(player,"spawnPoint",spawn);var playerData=new SerializedObject(player);playerData.FindProperty("respawnWhenFalling").boolValue=false;playerData.ApplyModifiedPropertiesWithoutUndo();
  var enemy=Instance(MarioDemoSetup.EnemyPath,scene,actors);enemy.transform.position=new Vector3(5,.1f,0);var goomba=enemy.GetComponent<GoombaEnemy>();Set(goomba,"target",player);Number(goomba,"patrolLeft",.3f);Number(goomba,"patrolRight",6.7f);
  var coin=Instance(MarioDemoSetup.CoinPath,scene,items);coin.name="Optional Coin";coin.transform.position=new Vector3(3,1,0);
  var goal=Instance(FlagGoalTools.PrefabPath,scene,items);goal.transform.position=new Vector3(36,1,0);
  var cameras=Group("Cameras",scene);var cameraObject=Group("Main Camera",scene,cameras).gameObject;cameraObject.tag="MainCamera";var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.46f,.75f,.94f);camera.orthographic=true;camera.orthographicSize=4.5f;camera.transform.position=new Vector3(3,2,-25);cameraObject.AddComponent<AudioListener>();var follow=cameraObject.AddComponent<MarioFollowCamera>();Set(follow,"target",p.transform);
  var light=Group("Sun",scene).gameObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(45,-35,0);RenderSettings.ambientLight=new Color(.68f,.72f,.8f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
  var level=system.gameObject.AddComponent<FlagLevelController>();Set(level,"player",player);Set(level,"coin",coin.GetComponent<CollectibleCoin>());Set(level,"goal",goal.GetComponent<FlagGoal>());
  var canvasObject=Group("HUD",scene).gameObject;var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=10;
  var scaler=canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
  var header=Panel("Objective",canvas.transform,new Vector2(0,.88f),Vector2.one,new Color(.035f,.09f,.18f,.93f));Label("Title",header.transform,"META  ·  Alcanza el mástil y baja la bandera",27,Vector2.zero,Vector2.one,Color.white);
  var footer=Panel("Controls",canvas.transform,Vector2.zero,new Vector2(1,.09f),new Color(.035f,.09f,.18f,.88f));Label("Controls Text",footer.transform,"A / D  mover     W / ESPACIO  saltar     S en el aire  golpe hacia abajo",20,Vector2.zero,Vector2.one,Color.white);
  var result=Panel("Result",canvas.transform,new Vector2(.2f,.28f),new Vector2(.8f,.72f),new Color(.025f,.055f,.12f,.97f));var title=Label("Result Title",result.transform,"",34,new Vector2(0,.64f),new Vector2(1,.93f),new Color(1,.8f,.15f));var message=Label("Result Message",result.transform,"",23,new Vector2(0,.36f),new Vector2(1,.65f),Color.white);
  var buttonObject=Panel("Retry",result.transform,new Vector2(.27f,.08f),new Vector2(.73f,.31f),new Color(.15f,.5f,.9f));var button=buttonObject.AddComponent<UnityEngine.UI.Button>();Label("Retry Text",buttonObject.transform,"REINTENTAR  ·  R",22,Vector2.zero,Vector2.one,Color.white);UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick,level.RestartLevel);
  Set(level,"resultPanel",result);Set(level,"resultTitle",title);Set(level,"resultMessage",message);result.SetActive(false);
  var events=Group("EventSystem",scene).gameObject;events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
  EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();foreach(var old in oldScenes)if(old.IsValid()&&!old.isDirty)EditorSceneManager.CloseScene(old,true);
 }

 [MenuItem("Tools/Flag Goal/Repair Level Text")]
 public static void RepairText(){var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || EditorApplication.isPlaying)throw new Exception("Expected flag scene in Edit Mode");foreach(var root in scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))if(text.text.Contains("\u00c3")||text.text.Contains("\u00c2")){Undo.RecordObject(text,"Repair text encoding");text.text=System.Text.Encoding.UTF8.GetString(System.Text.Encoding.GetEncoding(1252).GetBytes(text.text));EditorUtility.SetDirty(text);}EditorSceneManager.SaveScene(scene);}
}
