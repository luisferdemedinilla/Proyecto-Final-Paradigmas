using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class MarioDemoValidation
{
 const string Session="MarioDemo.Validation";
 static Keyboard keyboard;static InputSettings.BackgroundBehavior background;
 static DemoLevelController level;static MarioPlayerController player;static Rigidbody body;static GoombaEnemy enemy;static int phase,frame;
 static Quaternion coinInitialRotation;static float began,enemyX;static double deadline;static Key[] keys=Array.Empty<Key>();static CollectibleCoin ordinary;
 static readonly List<object> results=new List<object>();
 [InitializeOnLoadMethod]static void Subscribe(){EditorApplication.playModeStateChanged-=State;EditorApplication.playModeStateChanged+=State;}
 [MenuItem("Tools/Mario Demo/Validate Playable Level")]
 public static void Run(){if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");SessionState.SetString(Session+".Start",EditorSceneManager.playModeStartScene==null?"":AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetBool(Session,true);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(MarioDemoSetup.ScenePath);EditorApplication.isPlaying=true;}
 static void State(PlayModeStateChange state)
 {
  if(!SessionState.GetBool(Session,false))return;
  if(state==PlayModeStateChange.EnteredPlayMode)
  {
   level=UnityEngine.Object.FindAnyObjectByType<DemoLevelController>();player=level.Player;body=player.GetComponent<Rigidbody>();enemy=level.Enemy;
   coinInitialRotation=level.GoalCoin.transform.Find("Visual").localRotation;
   background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;
   keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();keys=Array.Empty<Key>();phase=0;began=Time.time;deadline=EditorApplication.timeSinceStartup+90;frame=Time.frameCount;results.Clear();
   InputSystem.onBeforeUpdate+=Input;EditorApplication.update+=Tick;
  }
  if(state==PlayModeStateChange.ExitingPlayMode)Cleanup();
  if(state==PlayModeStateChange.EnteredEditMode){var path=SessionState.GetString(Session+".Start","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);SessionState.SetBool(Session,false);}
 }
 static void Input(){if(keyboard!=null && InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message+" (phase "+phase+")");}
 static void Next(params Key[] input){phase++;keys=input;began=Time.time;}
 static void Record(string name){results.Add(new{name,passed=true,frames=Time.frameCount-frame});}
 static void Teleport(Vector3 position,Vector3 velocity){body.position=position;body.linearVelocity=velocity;Physics.SyncTransforms();}
 static void Tick()
 {
  if(!EditorApplication.isPlaying)return;EditorApplication.QueuePlayerLoopUpdate();
  try
  {
   Check(EditorApplication.timeSinceStartup<deadline,"Timed out");float elapsed=Time.time-began;Check(Mathf.Abs(body.position.z)<.001f,"Player left XY plane");
   if(phase==0 && elapsed>.6f){Check(player.IsGrounded && level.State==DemoLevelController.LevelState.Playing,"Spawn failed");Check(Quaternion.Angle(coinInitialRotation,level.GoalCoin.transform.Find("Visual").localRotation)>20,"Coin did not rotate");var gold=level.GoalCoin.GetComponentInChildren<Renderer>().sharedMaterial.GetColor("_BaseColor");Check(gold.r>.8f && gold.g>.4f && gold.b<.2f,"Coin is not gold");Record("Gold coin rotates");Capture("level-start");Record("Playable spawn and UI");Next(Key.S);}
   else if(phase==1 && elapsed>.15f){Check(player.IsCrouching && !player.IsGroundPounding,"Grounded crouch became pound");Record("S on ground crouches");Next();}
   else if(phase==2 && elapsed>.2f)Next(Key.W);
   else if(phase==3 && elapsed>.3f){Check(!player.IsGrounded,"Jump failed");Next();}
   else if(phase==4 && elapsed>.06f)Next(Key.S);
   else if(phase==5 && player.GroundPoundPhase==1){Check(player.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("GroundPoundStart"),"Preparation animation missing");Record("Airborne crouch starts preparation");Next();}
   else if(phase==6 && player.GroundPoundPhase==2){Check(player.PrePhysicsVerticalSpeed<=-17,"Pound not descending fast");Capture("ground-pound");Record("Fast vertical pound");Next();}
   else if(phase==7 && elapsed>.7f){Check(player.IsGrounded && !player.IsGroundPounding,"Pound did not land/reset");Record("Pound landing and recovery");Teleport(new Vector3(15,.08f,0),Vector3.zero);enemyX=enemy.transform.position.x;Next();}
   else if(phase==8 && elapsed>.35f){Check(enemy.IsChasing && enemy.transform.position.x>enemyX+.1f,"Goomba did not chase");Check(enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Chase"),"Goomba chase animation missing");Record("Animated Goomba chases");Teleport(new Vector3(19.5f,.02f,0),Vector3.zero);enemy.GetComponent<Rigidbody>().position=new Vector3(16.5f,.05f,0);phase=80;began=Time.time;keys=Array.Empty<Key>();}
   else if(phase==80 && elapsed>1.3f){Check(enemy.transform.position.x>18 && enemy.IsChasing && !enemy.IsBlocked,"Goomba stopped at patrol boundary");Record("Goomba chases beyond patrol boundary");Teleport(new Vector3(20.1f,.02f,0),Vector3.zero);enemy.GetComponent<Rigidbody>().position=new Vector3(19.05f,.02f,0);phase=82;began=Time.time;}
   else if(phase==82 && elapsed>.25f){Check(enemy.transform.position.x>19.25f && !enemy.IsBlocked,"Goomba stopped at seam beside wall");Record("Goomba crosses seam beside wall");level.RestartLevel();Teleport(new Vector3(7,.02f,0),Vector3.zero);enemy.GetComponent<Rigidbody>().position=new Vector3(10.1f,.02f,0);phase=81;began=Time.time;}
   else if(phase==81 && elapsed>.7f){Check(enemy.IsBlocked && enemy.transform.position.x>9.7f,"Goomba walked into the gap");Record("Goomba detects real gap");enemy.RestartEnemy();Teleport(new Vector3(13,.02f,0),Vector3.zero);phase=9;began=Time.time;keys=new[]{Key.D};}
   else if(phase==9 && level.State==DemoLevelController.LevelState.Lost){Check(!enemy.IsDefeated && !player.enabled,"Side contact did not lose");Capture("defeat");Record("Side contact loses level");level.RestartLevel();Next();}
   else if(phase==10 && elapsed>.3f){Check(level.State==DemoLevelController.LevelState.Playing && player.enabled && !level.GoalCoin.Collected && !enemy.IsDefeated,"Restart failed");Record("Restart restores level");var top=enemy.GetComponent<Collider>().bounds.max.y;Teleport(new Vector3(enemy.transform.position.x,top+.65f,0),Vector3.down*5);Next();}
   else if(phase==11 && enemy.IsDefeated){Check(level.State==DemoLevelController.LevelState.Playing && body.linearVelocity.y>0,"Stomp did not bounce safely");Capture("stomp");Record("Descending stomp defeats Goomba and bounces");Next();}
   else if(phase==12 && elapsed>.7f){Check(!enemy.transform.Find("Visual").gameObject.activeSelf,"Defeated Goomba not removed");Teleport(new Vector3(19,.02f,0),Vector3.zero);var coin=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MarioDemoSetup.CoinPath),new Vector3(19,.8f,0),Quaternion.identity);ordinary=coin.GetComponent<CollectibleCoin>();Next();}
   else if(phase==13 && elapsed>.3f){Check(ordinary.Collected && level.State==DemoLevelController.LevelState.Playing,"Ordinary coin incorrectly completes level");Record("Ordinary coin does not win");Teleport(new Vector3(25,2.02f,0),Vector3.zero);Next();}
   else if(phase==14 && level.State==DemoLevelController.LevelState.Won){Check(level.GoalCoin.Collected,"Goal was not collected");Capture("victory");Record("Designated goal coin wins");level.RestartLevel();Next();}
   else if(phase==15 && elapsed>.3f){Next(Key.W);}
   else if(phase==16 && elapsed>.2f){Teleport(new Vector3(enemy.transform.position.x+.5f,3,0),body.linearVelocity);Next();}
   else if(phase==17 && elapsed>.06f)Next(Key.S);
   else if(phase==18 && enemy.IsDefeated){Check(level.State==DemoLevelController.LevelState.Playing,"Pound kill caused defeat");Record("Ground pound defeats Goomba");level.RestartLevel();Next();}
   else if(phase==19 && elapsed>.3f){Teleport(new Vector3(7.1f,.02f,0),Vector3.right*6);Next(Key.D);}
   else if(phase==20 && body.position.x>8.3f && body.position.x<9.1f){Check(!player.IsGrounded,"Ground support persisted over a real gap");Record("Real gap removes grounded support");Next();}
   else if(phase==21 && level.State==DemoLevelController.LevelState.Lost){Record("Falling through gap loses level");var message=(UnityEngine.UI.Text)new SerializedObject(level).FindProperty("resultMessage").objectReferenceValue;Check(message.text.Contains("precipicio"),"Wrong fall defeat message");Next(Key.R);}
   else if(phase==22 && elapsed>.12f)Next();
   else if(phase==23 && elapsed>.7f){Check(level.State==DemoLevelController.LevelState.Playing && player.IsGrounded && body.position.y>-.1f,"Keyboard restart repeated fall defeat: state="+level.State+" body="+body.position+" transform="+player.transform.position+" grounded="+player.IsGrounded);Record("R restarts after falling without repeated defeat");Teleport(new Vector3(8.5f,-7,0),Vector3.zero);Next();}
   else if(phase==24 && level.State==DemoLevelController.LevelState.Lost){var panel=(GameObject)new SerializedObject(level).FindProperty("resultPanel").objectReferenceValue;panel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();Next();}
   else if(phase==25 && elapsed>.7f){Check(level.State==DemoLevelController.LevelState.Playing && player.IsGrounded && body.position.y>-.1f,"Button restart repeated fall defeat");Record("Retry button restarts after falling without repeated defeat");Check(Time.frameCount-frame>100,"No real frame advancement");Finish(true,null);}
   Check(elapsed<5,"Scenario did not complete");
  }
  catch(Exception e){Finish(false,e.ToString());}
 }
 public static void Capture(string name)
 {
  var camera=Camera.main;var rt=new RenderTexture(1280,720,24);rt.Create();var previous=RenderTexture.active;
  try{UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("Temp/MarioDemoAudit");File.WriteAllBytes("Temp/MarioDemoAudit/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
  finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
 }
 static void Finish(bool passed,string error){Directory.CreateDirectory("Temp/MarioDemoAudit");File.WriteAllText("Temp/MarioDemoAudit/gameplay.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,results},Newtonsoft.Json.Formatting.Indented));Cleanup();if(passed)Debug.Log("Mario demo gameplay validation passed");else Debug.LogError(error);EditorApplication.isPlaying=false;}
 static void Cleanup(){EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Input;if(keyboard!=null){InputSystem.settings.backgroundBehavior=background;if(keyboard.added)InputSystem.RemoveDevice(keyboard);keyboard=null;}if(ordinary!=null)UnityEngine.Object.Destroy(ordinary.gameObject);}
}
