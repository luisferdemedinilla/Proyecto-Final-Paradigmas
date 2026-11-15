using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
[DisallowMultipleComponent]
public sealed class FlagGoal:MonoBehaviour, ILevelResettable
{
 [SerializeField] Transform flagCarriage;
 [SerializeField] Animator poleAnimator;
 [Serializable] public class CharacterClips{public RuntimeAnimatorController controller;public AnimationClip hold,slide,celebrate;}
 [SerializeField] CharacterClips[] characters;
 AnimationClip holdClip,slideClip,celebrateClip;
 [SerializeField,Min(.1f)] float slideDuration=1.8f;
 [SerializeField] float loweredFlagHeight=.9f;
 [SerializeField] float landingHeight=.9f;
 public event Action<MarioPlayerController> Grabbed;
 public event Action Completed;
 public bool IsFinishing {get;private set;}
 public bool HasCompleted {get;private set;}
 public float FlagHeight=>flagCarriage.localPosition.y;
 RuntimeAnimatorController previousController;
 Vector3 flagStart,grabPosition;MarioPlayerController player;Rigidbody body;Animator animator;PlayableGraph graph;AnimationClipPlayable motion;float began;int phase;
 void Awake(){flagStart=flagCarriage.localPosition;}
 void OnTriggerEnter(Collider other){var p=other.GetComponentInParent<MarioPlayerController>();if(p!=null && p.enabled && !IsFinishing && !HasCompleted)Begin(p);}
 void Begin(MarioPlayerController p)
 {
  player=p;body=p.GetComponent<Rigidbody>();animator=p.GetComponent<Animator>();
  var clips=Array.Find(characters,c=>c.controller==animator.runtimeAnimatorController);if(clips==null)return;
  holdClip=clips.hold;slideClip=clips.slide;celebrateClip=clips.celebrate;IsFinishing=true;phase=0;began=Time.time;
  player.enabled=false;previousController=animator.runtimeAnimatorController;animator.runtimeAnimatorController=null;body.linearVelocity=Vector3.zero;body.isKinematic=true;animator.speed=1;animator.applyRootMotion=false;
  var visual=player.transform.Find("Visual");visual.rotation=Quaternion.Euler(0,90,0);
  Play(holdClip);graph.Evaluate(0);
  grabPosition=body.position;grabPosition.y=Mathf.Max(transform.position.y+landingHeight,grabPosition.y);grabPosition.z=transform.position.z;
  var hand=visual.Find("Model/AllRoot/JointRoot/Spine1/Spine2/ShoulderL/ArmL1/ArmL2/HandL");
  grabPosition.x+=transform.position.x-(hand==null?player.transform.position.x+.45f:hand.position.x);
  MovePlayer(grabPosition);Grabbed?.Invoke(player);
 }
 void Play(AnimationClip clip){if(graph.IsValid())graph.Destroy();graph=PlayableGraph.Create("Flag goal character");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);motion=AnimationClipPlayable.Create(graph,clip);motion.SetApplyFootIK(false);var output=AnimationPlayableOutput.Create(graph,"Character",animator);output.SetSourcePlayable(motion);graph.Play();}
 void MovePlayer(Vector3 position){body.position=position;player.transform.position=position;}
 void Update()
 {
  if(!IsFinishing)return;float elapsed=Time.time-began;
  if(phase==0 && elapsed>=.25f){phase=1;began=Time.time;elapsed=0;Play(slideClip);poleAnimator.Play("Finish",0,0);}
  if(phase==1){float t=Mathf.Clamp01(elapsed/slideDuration);flagCarriage.localPosition=Vector3.Lerp(flagStart,new Vector3(flagStart.x,loweredFlagHeight,flagStart.z),t);MovePlayer(new Vector3(grabPosition.x,Mathf.Lerp(grabPosition.y,transform.position.y+landingHeight,t),grabPosition.z));if(t>=1){phase=2;began=Time.time;Play(celebrateClip);MovePlayer(transform.position+Vector3.right*.9f);player.transform.Find("Visual").rotation=Quaternion.Euler(0,180,0);HasCompleted=true;Completed?.Invoke();}}
  if(graph.IsValid()){graph.Evaluate(Time.deltaTime);if(phase==2 && Time.time-began>=celebrateClip.length){motion.SetTime(celebrateClip.length);graph.Evaluate(0);IsFinishing=false;}}
 }
 public void ResetForLevel()
 {
  if(graph.IsValid())graph.Destroy();IsFinishing=HasCompleted=false;phase=0;flagCarriage.localPosition=flagStart;poleAnimator.Rebind();poleAnimator.Update(0);
  if(animator!=null){animator.runtimeAnimatorController=previousController;animator.Rebind();animator.Update(0);}previousController=null;player=null;body=null;animator=null;
 }
 void OnDestroy(){if(graph.IsValid())graph.Destroy();}
}
