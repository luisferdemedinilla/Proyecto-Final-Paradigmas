using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
public static class FlagLevelValidation
{
 const string Session="FlagLevel.Validation";
 static float jumpReleaseAt;static bool[] jumped=new bool[3];
 static GoombaEnemy testedGoomba;
 static Keyboard keyboard;static InputSettings.BackgroundBehavior background;static FlagLevelController level;static MarioPlayerController player;static Rigidbody body;static float began,flagStart;static int phase,frame,completed;static double deadline;static Key[] keys=Array.Empty<Key>();static List<string> results=new List<string>();static BreakableBrick brick;static QuestionBlock question;
 [InitializeOnLoadMethod]static void Subscribe(){EditorApplication.playModeStateChanged+=State;}
 [MenuItem("Tools/Flag Goal/Validate Level")]
 public static void Run(){if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");SessionState.SetString(Session+".Start",EditorSceneManager.playModeStartScene==null?"":AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(FlagLevelSetup.ScenePath);SessionState.SetBool(Session,true);EditorApplication.isPlaying=true;}
 static void State(PlayModeStateChange change){if(!SessionState.GetBool(Session,false))return;if(change==PlayModeStateChange.EnteredPlayMode){level=UnityEngine.Object.FindAnyObjectByType<FlagLevelController>();player=level.Player;body=player.GetComponent<Rigidbody>();background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();jumped=new bool[3];jumpReleaseAt=-100;phase=0;began=Time.time;deadline=EditorApplication.timeSinceStartup+120;frame=Time.frameCount;completed=0;level.Goal.Completed+=()=>completed++;flagStart=level.Goal.FlagHeight;results.Clear();keys=Array.Empty<Key>();InputSystem.onBeforeUpdate+=Input;EditorApplication.update+=Tick;}if(change==PlayModeStateChange.ExitingPlayMode){EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Input;if(keyboard!=null){InputSystem.settings.backgroundBehavior=background;InputSystem.RemoveDevice(keyboard);keyboard=null;}}if(change==PlayModeStateChange.EnteredEditMode){var path=SessionState.GetString(Session+".Start","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);SessionState.SetBool(Session,false);}}
 static void Input(){if(keyboard!=null && InputState.currentUpdateType==InputUpdateType.Dynamic){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}}
 static void Next(params Key[] input){phase++;began=Time.time;keys=input;}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message+" phase="+phase+" x="+body.position.x+" y="+body.position.y);}
 static void Teleport(Vector3 p,Vector3 v){body.position=p;player.transform.position=p;body.linearVelocity=v;Physics.SyncTransforms();}
 static void Tick(){EditorApplication.QueuePlayerLoopUpdate();try{float elapsed=Time.time-began;Check(EditorApplication.timeSinceStartup<deadline && elapsed<20,"Timeout");Check(Mathf.Abs(body.position.z)<.001f,"Player left XY plane");
 if(phase==0 && elapsed>.6f){Check(level.Enemies.Count>0,"Expected a Goomba in the flag level");testedGoomba=level.Enemies.OrderBy(enemy=>Mathf.Abs(enemy.transform.position.x-5f)).First();Check(player.IsGrounded,"Spawn unsupported");var platforms=GameObject.Find("Environment").transform;Check(platforms.childCount==4 && Enumerable.Range(0,4).All(i=>platforms.GetChild(i).childCount==8),"Expected four platforms of eight blocks");results.Add("Four platforms of eight gameplay blocks");Capture("start");Teleport(new Vector3(3,.05f,0),Vector3.zero);Next();}
 else if(phase==1 && elapsed>.2f){Check(level.Coin.Collected && level.State==FlagLevelController.LevelState.Playing,"Coin changed win condition");results.Add("Optional coin collects without ending level");var x=testedGoomba.transform.position.x;Teleport(new Vector3(x-.9f,.02f,0),Vector3.zero);Next(Key.D);}
 else if(phase==2 && level.State==FlagLevelController.LevelState.Lost){Check(level.Enemies.All(enemy=>enemy.GetComponent<Rigidbody>().isKinematic),"A Goomba did not stop on loss");results.Add("Goomba side contact loses and stops all Goombas");Next(Key.R);}
 else if(phase==3 && elapsed>.15f)Next();
 else if(phase==4 && elapsed>.5f){Check(level.State==FlagLevelController.LevelState.Playing && player.IsGrounded,"R did not restart");Check(level.Enemies.All(enemy=>!enemy.IsDefeated && !enemy.GetComponent<Rigidbody>().isKinematic),"A Goomba did not restart");results.Add("Keyboard retry restores the player and all Goombas");Teleport(new Vector3(testedGoomba.transform.position.x,testedGoomba.GetComponent<Collider>().bounds.max.y+.7f,0),Vector3.down*5);Next();}
 else if(phase==5 && testedGoomba.IsDefeated){Check(level.State==FlagLevelController.LevelState.Playing,"Stomp lost level");results.Add("Stomp still defeats Goomba");foreach(var enemy in level.Enemies)if(enemy!=testedGoomba)enemy.gameObject.SetActive(false);Teleport(new Vector3(1,.05f,0),Vector3.zero);Next();}
 else if(phase==6 && elapsed>.4f)Next(Key.D);
 else if(phase==7){float x=body.position.x;for(int gap=0;gap<3;gap++)if(!jumped[gap] && player.IsGrounded && x>=6f+gap*10 && x<7.5f+gap*10){jumped[gap]=true;jumpReleaseAt=Time.time+.32f;}keys=Time.time<jumpReleaseAt?new[]{Key.D,Key.W}:new[]{Key.D};Check(level.State!=FlagLevelController.LevelState.Lost,"Traversal fell into gap");if(level.State==FlagLevelController.LevelState.Finishing){Check(!player.enabled,"Controls still active at goal");results.Add("Continuous keyboard traversal reaches pole across all gaps");Capture("grab");Next();}}
 else if(phase==8 && elapsed>.8f){Check(level.Goal.FlagHeight<flagStart-.5f && !level.Goal.HasCompleted,"Flag did not descend gradually");Capture("descending");results.Add("Flag descends while character holds pole");Next();}
 else if(phase==9 && level.State==FlagLevelController.LevelState.Won){Check(level.Goal.FlagHeight<2f && completed==1,"Goal completion duplicated or flag not lowered");Capture("victory");results.Add("Lowered flag completes level once");Next();}
 else if(phase==10 && elapsed>.7f){foreach(var enemy in level.Enemies)if(enemy!=testedGoomba)enemy.gameObject.SetActive(true);level.RestartLevel();Check(Mathf.Abs(level.Goal.FlagHeight-flagStart)<.001f && !level.Goal.HasCompleted,"Flag not restored");Next();}
 else if(phase==11 && elapsed>.5f){Check(player.IsGrounded && level.State==FlagLevelController.LevelState.Playing,"Restart from goal failed");results.Add("Goal retry restores gameplay controller and flag");brick=UnityEngine.Object.FindObjectsByType<BreakableBrick>().OrderBy(b=>Mathf.Abs(b.transform.position.x-12)).First();Next(Key.W);}
 else if(phase==12 && elapsed>.2f){Teleport(new Vector3(brick.transform.position.x,4,0),body.linearVelocity);Next();}
 else if(phase==13 && elapsed>.06f)Next(Key.S);
 else if(phase==14 && brick.IsBroken){level.RestartLevel();Check(!brick.IsBroken,"Restart did not restore brick");results.Add("Broken bricks restore on retry");Next();}
 else if(phase==15 && elapsed>.5f){question=UnityEngine.Object.FindObjectsByType<QuestionBlock>().OrderBy(q=>Mathf.Abs(q.transform.position.x-32)).First();Next(Key.W);}
 else if(phase==16 && elapsed>.2f){Teleport(new Vector3(question.transform.position.x,4,0),body.linearVelocity);Next();}
 else if(phase==17 && elapsed>.06f)Next(Key.S);
 else if(phase==18 && question.IsEmpty){Check(level.State==FlagLevelController.LevelState.Playing,"Question coin ended level");level.RestartLevel();Check(!question.IsEmpty && question.SpawnedContent==null,"Question block not reset");results.Add("Question block and its content restore on retry");Next();}
 else if(phase==19 && elapsed>.4f){Teleport(new Vector3(8.5f,-7,0),Vector3.zero);Next();}
 else if(phase==20 && level.State==FlagLevelController.LevelState.Lost){Next(Key.R);}
 else if(phase==21 && elapsed>.15f)Next();
 else if(phase==22 && elapsed>.6f){Check(level.State==FlagLevelController.LevelState.Playing && player.IsGrounded,"Fall retry loop");results.Add("Falling loses and retry succeeds");Check(Time.frameCount-frame>100,"No frame advancement");Finish(true,null);}
 }catch(Exception e){Finish(false,e.ToString());}}
 static void Capture(string name){MarioDemoValidation.Capture("flag-"+name);}
 static void Finish(bool passed,string error){Directory.CreateDirectory("Temp/FlagGoalAudit");File.WriteAllText("Temp/FlagGoalAudit/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,results,frames=Time.frameCount-frame},Newtonsoft.Json.Formatting.Indented));if(!passed)Debug.LogError(error);EditorApplication.isPlaying=false;}
}
