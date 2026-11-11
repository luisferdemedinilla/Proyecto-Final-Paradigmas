using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class MarioMotionValidation
{
 const string KeyName="Mario.Motion.Validation";
 static int characterIndex;static readonly string[] Characters={"Mario","Luigi","Toad"};static readonly List<string> results=new List<string>();
 static Keyboard keyboard;static InputSettings.BackgroundBehavior background;static MarioPlayerController player;static Rigidbody body;static Animator animator;static float start;static int frame;static readonly List<object> rows=new List<object>();static Key[] keys=Array.Empty<Key>();
 [InitializeOnLoadMethod]static void Init(){EditorApplication.playModeStateChanged+=State;}
 [MenuItem("Tools/Mario Demo/Trace Motion")]
 public static void Run(){SessionState.SetBool(KeyName+".Blocks",false);SessionState.SetBool(KeyName,true);EditorApplication.isPlaying=true;}
 [MenuItem("Tools/Mario Demo/Trace Block Motion")] public static void RunBlocks(){SessionState.SetBool(KeyName+".Blocks",true);SessionState.SetBool(KeyName,true);EditorApplication.isPlaying=true;}
 static void State(PlayModeStateChange state){if(!SessionState.GetBool(KeyName,false))return;if(state==PlayModeStateChange.EnteredPlayMode){foreach(var l in UnityEngine.Object.FindObjectsByType<DemoLevelController>())l.enabled=false;foreach(var e in UnityEngine.Object.FindObjectsByType<GoombaEnemy>())e.gameObject.SetActive(false);foreach(var old in UnityEngine.Object.FindObjectsByType<MarioPlayerController>())old.gameObject.SetActive(false);characterIndex=0;results.Clear();Spawn();if(!SessionState.GetBool(KeyName+".Blocks",false)){var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,9.5f,0);floor.transform.localScale=new Vector3(100,1,4);body.position=new Vector3(0,10.05f,0);body.linearVelocity=Vector3.zero;}background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();start=Time.time;frame=-1;rows.Clear();InputSystem.onBeforeUpdate+=Input;EditorApplication.update+=Tick;}if(state==PlayModeStateChange.ExitingPlayMode){EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Input;if(keyboard!=null){InputSystem.settings.backgroundBehavior=background;InputSystem.RemoveDevice(keyboard);keyboard=null;}}if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(KeyName,false);}
 static void Spawn(){var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HelloMarioPlayableBuilder.Playables+Characters[characterIndex]+"Player.prefab"),SessionState.GetBool(KeyName+".Blocks",false)?new Vector3(11,.05f,0):new Vector3(0,10.05f,0),Quaternion.identity);player=go.GetComponent<MarioPlayerController>();body=go.GetComponent<Rigidbody>();animator=go.GetComponent<Animator>();}
 static void Input(){if(keyboard!=null && InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}}
 static void Tick(){EditorApplication.QueuePlayerLoopUpdate();if(frame==Time.frameCount)return;frame=Time.frameCount;float t=Time.time-start;
 keys=t<.5f?Array.Empty<Key>():t<2.5f?new[]{Key.D}:t<2.8f?new[]{Key.D,Key.W}:t<2.9f?Array.Empty<Key>():t<3f?new[]{Key.W}:t<3.1f?Array.Empty<Key>():t<3.6f?new[]{Key.S}:t<4.5f?Array.Empty<Key>():new[]{Key.D};
 var s=animator.GetCurrentAnimatorStateInfo(0);rows.Add(new{t,x=body.position.x,y=body.position.y,vx=body.linearVelocity.x,vy=body.linearVelocity.y,ground=player.IsGrounded,pound=player.GroundPoundPhase,state=new[]{"Idle","Walk","Run","Jump","Fall","Crouch","AirSpin","GroundPoundStart","GroundPoundFall","GroundPoundLand"}.FirstOrDefault(n=>s.IsName(n)),s.normalizedTime,clips=animator.GetCurrentAnimatorClipInfo(0).Select(c=>new{name=c.clip.name,c.weight})});
 if(t>6){Directory.CreateDirectory("Temp/MarioMotionAudit");File.WriteAllText("Temp/MarioMotionAudit/trace-"+(SessionState.GetBool(KeyName+".Blocks",false)?"blocks-":"")+Characters[characterIndex]+".json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));UnityEngine.Object.Destroy(player.gameObject);characterIndex++;if(characterIndex<Characters.Length){Spawn();start=Time.time;rows.Clear();keys=Array.Empty<Key>();}else EditorApplication.isPlaying=false;}
 }
}
