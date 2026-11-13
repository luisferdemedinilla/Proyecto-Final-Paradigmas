using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class BrickValidation
{
 static Keyboard keyboard;static InputSettings.BackgroundBehavior background;static MarioPlayerController player;static Rigidbody body;static BreakableBrick brick;static float began;static int phase,startFrame;static double deadline;static Key[] keys=Array.Empty<Key>();static Vector3 fragmentStart;static List<string> results=new List<string>();
 static List<object> impacts=new List<object>();
 public static void Impact(Collision c){var p=c.collider.GetComponentInParent<MarioPlayerController>();if(p!=null)impacts.Add(new{p.GroundPoundPhase,p.PrePhysicsVerticalSpeed,y=p.transform.position.y,normal=c.GetContact(0).normal.ToString(),feet=p.GetComponent<Collider>().bounds.min.y,top=brick.GetComponent<Collider>().bounds.max.y});}
 const string Session="Brick.Validation";
 [InitializeOnLoadMethod]static void Subscribe(){EditorApplication.playModeStateChanged+=State;}
 [MenuItem("Tools/Brick/Validate Collisions")]
 public static void Run(){if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");SessionState.SetBool(Session,true);EditorApplication.isPlaying=true;}
 static void State(PlayModeStateChange change){if(!SessionState.GetBool(Session,false))return;if(change==PlayModeStateChange.EnteredPlayMode){foreach(var level in UnityEngine.Object.FindObjectsByType<DemoLevelController>())level.enabled=false;foreach(var enemy in UnityEngine.Object.FindObjectsByType<GoombaEnemy>())enemy.gameObject.SetActive(false);foreach(var p in UnityEngine.Object.FindObjectsByType<MarioPlayerController>())p.gameObject.SetActive(false);var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,9.5f,0);floor.transform.localScale=new Vector3(30,1,4);var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HelloMarioPlayableBuilder.Playables+"MarioPlayer.prefab"),new Vector3(0,10.05f,0),Quaternion.identity);player=go.GetComponent<MarioPlayerController>();body=go.GetComponent<Rigidbody>();MakeBrick(new Vector3(0,12.1f,0));background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();phase=0;began=Time.time;startFrame=Time.frameCount;deadline=EditorApplication.timeSinceStartup+60;results.Clear();impacts.Clear();keys=Array.Empty<Key>();InputSystem.onBeforeUpdate+=Input;EditorApplication.update+=Tick;}if(change==PlayModeStateChange.ExitingPlayMode){EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Input;if(keyboard!=null){InputSystem.settings.backgroundBehavior=background;InputSystem.RemoveDevice(keyboard);keyboard=null;}}if(change==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Session,false);}
 static void MakeBrick(Vector3 position){if(brick!=null)UnityEngine.Object.Destroy(brick.gameObject);brick=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BrickTools.Gameplay),position,Quaternion.identity).GetComponent<BreakableBrick>();brick.gameObject.AddComponent<BrickCollisionProbe>();fragmentStart=brick.transform.Find("Fragments/AllRoot/BlockBrick00").localPosition;}
 static void Teleport(Vector3 p,Vector3 v){body.position=p;player.transform.position=p;body.linearVelocity=v;Physics.SyncTransforms();}
 static void Input(){if(keyboard!=null && InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}}
 static void Next(params Key[] input){phase++;began=Time.time;keys=input;}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message+" phase="+phase);}
 static void Tick(){EditorApplication.QueuePlayerLoopUpdate();try{float elapsed=Time.time-began;Check(EditorApplication.timeSinceStartup<deadline && elapsed<6,"Timeout");
 if(phase==0 && elapsed>.5f){Check(player.IsGrounded,"Player not grounded");Next(Key.W);}
 else if(phase==1 && brick.IsBroken){Check(!brick.GetComponent<Collider>().enabled,"Broken brick still collides");results.Add("Upward head hit breaks brick and removes collision");Next();}
 else if(phase==2 && elapsed>.25f){Check(Vector3.Distance(fragmentStart,brick.transform.Find("Fragments/AllRoot/BlockBrick00").localPosition)>.05f,"Destruction fragments do not move");results.Add("Destruction animation moves fragments");Next();}
 else if(phase==3 && elapsed>1.5f){Check(!brick.transform.Find("Fragments").gameObject.activeSelf,"Fragments not hidden");brick.ResetBlock();Check(!brick.IsBroken && brick.GetComponent<Collider>().enabled && brick.transform.Find("Intact").gameObject.activeSelf,"Reset failed");results.Add("Fragments finish and ResetBlock restores brick");MakeBrick(new Vector3(3,10,0));Teleport(new Vector3(3,12.8f,0),Vector3.down*3);Next();}
 else if(phase==4 && elapsed>1){Check(!brick.IsBroken && player.IsGrounded,"Ordinary landing broke brick");results.Add("Ordinary landing does not break brick");Teleport(new Vector3(1.5f,10.02f,0),Vector3.zero);Next(Key.D);}
 else if(phase==5 && elapsed>.8f){Check(!brick.IsBroken && body.position.x<2.3f,"Side collision broke brick");results.Add("Side contact does not break brick");Teleport(new Vector3(0,10.02f,0),Vector3.zero);Next();}
 else if(phase==6 && elapsed>.3f)Next(Key.W);
 else if(phase==7 && elapsed>.2f){Teleport(new Vector3(3,14,0),body.linearVelocity);Next();}
 else if(phase==8 && elapsed>.06f)Next(Key.S);
 else if(phase==9 && brick.IsBroken){results.Add("Ground pound breaks brick from above");Check(Time.frameCount-startFrame>100,"Frames not advancing");Finish(true,null);}
 }catch(Exception e){Finish(false,e.ToString());}}
 static void Finish(bool passed,string error){Directory.CreateDirectory("Temp/BrickAudit");File.WriteAllText("Temp/BrickAudit/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,results,impacts},Newtonsoft.Json.Formatting.Indented));if(!passed)Debug.LogError(error);EditorApplication.isPlaying=false;}
}

public sealed class BrickCollisionProbe:MonoBehaviour{void OnCollisionEnter(Collision c){BrickValidation.Impact(c);}void OnCollisionStay(Collision c){if(c.collider.GetComponentInParent<MarioPlayerController>()?.GroundPoundPhase>0)BrickValidation.Impact(c);}}
