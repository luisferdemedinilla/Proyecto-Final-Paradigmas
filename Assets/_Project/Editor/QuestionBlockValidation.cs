using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class QuestionBlockValidation
{
 const string Session="QuestionBlock.Validation";
 static Keyboard keyboard;static InputSettings.BackgroundBehavior background;static MarioPlayerController player;static Rigidbody body;static QuestionBlock block;static GameObject content,empty;static float began;static double deadline;static int phase,startFrame;static Key[] keys=Array.Empty<Key>();static readonly List<string> results=new List<string>();
 [InitializeOnLoadMethod]static void Subscribe(){EditorApplication.playModeStateChanged+=State;}
 [MenuItem("Tools/Blocks/Validate Question And Empty")]
 public static void Run(){if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");SessionState.SetBool(Session,true);EditorApplication.isPlaying=true;}
 static void State(PlayModeStateChange change)
 {
  if(!SessionState.GetBool(Session,false))return;
  if(change==PlayModeStateChange.EnteredPlayMode)
  {
   foreach(var level in UnityEngine.Object.FindObjectsByType<DemoLevelController>())level.enabled=false;
   foreach(var enemy in UnityEngine.Object.FindObjectsByType<GoombaEnemy>())enemy.gameObject.SetActive(false);
   foreach(var p in UnityEngine.Object.FindObjectsByType<MarioPlayerController>())p.gameObject.SetActive(false);
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,9.5f,0);floor.transform.localScale=new Vector3(30,1,4);
   var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HelloMarioPlayableBuilder.Playables+"MarioPlayer.prefab"),new Vector3(0,10.05f,0),Quaternion.identity);player=go.GetComponent<MarioPlayerController>();body=go.GetComponent<Rigidbody>();
   MakeBlock(new Vector3(0,12.1f,0));background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();phase=0;began=Time.time;deadline=EditorApplication.timeSinceStartup+60;startFrame=Time.frameCount;results.Clear();keys=Array.Empty<Key>();InputSystem.onBeforeUpdate+=Input;EditorApplication.update+=Tick;
  }
  if(change==PlayModeStateChange.ExitingPlayMode){EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Input;if(keyboard!=null){InputSystem.settings.backgroundBehavior=background;InputSystem.RemoveDevice(keyboard);keyboard=null;}}
  if(change==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Session,false);
 }
 static void MakeBlock(Vector3 position){if(block!=null){block.ResetBlock();UnityEngine.Object.Destroy(block.gameObject);}block=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(QuestionBlockTools.Folder+"BlockQuestion.prefab"),position,Quaternion.identity).GetComponent<QuestionBlock>();}
 static void Teleport(Vector3 p,Vector3 v){body.position=p;player.transform.position=p;body.linearVelocity=v;Physics.SyncTransforms();}
 static void Input(){if(keyboard!=null && InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}}
 static void Next(params Key[] input){phase++;began=Time.time;keys=input;}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message+" phase="+phase);}
 static void AssertEmpty(){Check(block.IsEmpty && block.GetComponent<Collider>().enabled && block.transform.Find("EmptyVisual").gameObject.activeSelf && !block.transform.Find("QuestionVisual").gameObject.activeSelf,"Conversion to solid empty block failed");Check(block.SpawnedContent!=null && block.SpawnedContent.GetComponent<CollectibleCoin>()!=null,"Coin content missing");}
 static void Tick(){EditorApplication.QueuePlayerLoopUpdate();try{float elapsed=Time.time-began;Check(EditorApplication.timeSinceStartup<deadline && elapsed<6,"Timeout");
 if(phase==0 && elapsed>.5f){Check(player.IsGrounded,"Player not grounded");Next(Key.W);}
 else if(phase==1 && block.IsEmpty){AssertEmpty();content=block.SpawnedContent;results.Add("Head hit releases coin and leaves a solid empty block");Next();}
 else if(phase==2 && elapsed>1){Next(Key.W);}
 else if(phase==3 && elapsed>.5f){Check(block.SpawnedContent==content,"Repeated head hit released extra content");AssertEmpty();results.Add("Repeated hit does not release another item");content.GetComponent<CollectibleCoin>().Collect(player);Check(content.GetComponent<CollectibleCoin>().Collected,"Spawned coin cannot be collected");results.Add("Released coin can be collected");MakeBlock(new Vector3(3,10,0));Teleport(new Vector3(3,13,0),Vector3.down*3);Next();}
 else if(phase==4 && elapsed>1){Check(!block.IsEmpty && player.IsGrounded && block.SpawnedContent==null,"Ordinary landing activated block");results.Add("Ordinary landing keeps question block intact");Teleport(new Vector3(1.5f,10.02f,0),Vector3.zero);Next(Key.D);}
 else if(phase==5 && elapsed>.8f){Check(!block.IsEmpty && body.position.x<2.3f,"Side contact activated block");results.Add("Side contact does not activate block");Teleport(new Vector3(0,10.02f,0),Vector3.zero);Next();}
 else if(phase==6 && elapsed>.3f)Next(Key.W);
 else if(phase==7 && elapsed>.2f){Teleport(new Vector3(3,14,0),body.linearVelocity);Next();}
 else if(phase==8 && elapsed>.06f)Next(Key.S);
 else if(phase==9 && block.IsEmpty){AssertEmpty();results.Add("Ground pound releases coin and converts block to empty");Next();}
 else if(phase==10 && elapsed>.7f){AssertEmpty();Check(player.IsGrounded,"Player did not remain supported on empty block");results.Add("Empty replacement remains solid after ground pound");block.ResetBlock();Check(!block.IsEmpty && block.SpawnedContent==null,"ResetBlock failed");results.Add("ResetBlock restores question state and clears content");empty=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(QuestionBlockTools.Folder+"BlockEmpty.prefab"),new Vector3(6,10,0),Quaternion.identity);Check(empty.GetComponentsInChildren<Collider>(true).Length==1 && empty.GetComponent<BoxCollider>()!=null && !empty.GetComponent<BoxCollider>().isTrigger && empty.GetComponent<QuestionBlock>()==null && empty.GetComponent<BreakableBrick>()==null,"Unexpected empty block components");Teleport(new Vector3(6,13,0),Vector3.down*3);Next();}
 else if(phase==11 && elapsed>1){Check(player.IsGrounded && body.position.y>10.9f,"Standalone empty block does not support player");results.Add("Standalone BlockEmpty has one solid collider and supports player");Check(Time.frameCount-startFrame>100,"No frame advancement");Finish(true,null);}
 }catch(Exception e){Finish(false,e.ToString());}}
 static void Finish(bool passed,string error){Directory.CreateDirectory("Temp/QuestionBlockAudit");File.WriteAllText("Temp/QuestionBlockAudit/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,results,frames=Time.frameCount-startFrame},Newtonsoft.Json.Formatting.Indented));if(!passed)Debug.LogError(error);EditorApplication.isPlaying=false;}
}
