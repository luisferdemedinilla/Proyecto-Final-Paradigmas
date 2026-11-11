using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class MarioAirSpinValidation
{
    const string Session="MarioAirSpin.Validation";
    static Keyboard keyboard;
    static InputSettings.BackgroundBehavior previousBackgroundBehavior;
    static bool inputSettingsChanged;
    static MarioPlayerController player;
    static Rigidbody body;
    static GameObject floor;
    static Key[] keys=Array.Empty<Key>();
    static int phase,firstFrame;
    static float began,beforeFall,nextSample;
    static Quaternion rootRest;
    static Transform spinRoot;
    static double deadline;
    static readonly List<object> results=new List<object>();
    static void Check(bool ok,string message) {if(!ok) throw new Exception(message);}
    [InitializeOnLoadMethod]
    static void Subscribe() {EditorApplication.playModeStateChanged-=State;EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Mario Test/Validate Air Spin")]
    public static void Run()
    {
        Check(!EditorApplication.isPlaying,"Exit Play Mode first");
        SessionState.SetString(Session+".Start",EditorSceneManager.playModeStartScene==null?"":AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Session,true);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(MarioTestSceneSetup.ScenePath);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Session,false)) return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            previousBackgroundBehavior=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;inputSettingsChanged=true;
            Application.runInBackground=true;player=GameObject.Find("Actors/Mario").GetComponent<MarioPlayerController>();body=player.GetComponent<Rigidbody>();
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary mobility test floor";floor.transform.position=new Vector3(-40,-.5f,0);floor.transform.localScale=new Vector3(50,1,6);
            body.position=new Vector3(-40,.1f,0);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
            spinRoot=player.GetComponentsInChildren<Transform>().Single(t=>t.name=="skl_root" || t.name=="AllRoot");rootRest=spinRoot.localRotation;
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();phase=0;nextSample=0;began=Time.time;deadline=EditorApplication.timeSinceStartup+75;firstFrame=Time.frameCount;
            results.Clear();keys=Array.Empty<Key>();InputSystem.onBeforeUpdate-=QueueInput;InputSystem.onBeforeUpdate+=QueueInput;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.ExitingPlayMode) Cleanup();
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            var path=SessionState.GetString(Session+".Start","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);SessionState.SetBool(Session,false);
        }
    }
    static void Next(params Key[] input) {phase++;keys=input;began=Time.time;}
    static void QueueInput()
    {
        if(keyboard==null || InputState.currentUpdateType!=InputUpdateType.Dynamic) return;
        keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Test timed out, phase="+phase);

            float elapsed=Time.time-began;
            if(phase==3 && elapsed>=nextSample) {results.Add(new {elapsed,player.IsAirSpinning,normalized=player.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime,angle=Quaternion.Angle(rootRest,spinRoot.localRotation),velocity=body.linearVelocity.y});nextSample=elapsed+.05f;}
            if(phase==3 && elapsed>1) throw new Exception("Spin animation did not advance");
            Check(Mathf.Abs(body.position.z)<.001f,"Movement left XY plane");
            if(phase==0 && elapsed>.6f) {Check(player.IsGrounded,"Not grounded");Next(Key.W);}
            else if(phase==1 && elapsed>.35f) {Check(!player.IsAirSpinning && body.linearVelocity.y>0,"Holding initial jump triggered spin");Next();}
            else if(phase==2 && body.linearVelocity.y< -3f) {beforeFall=body.linearVelocity.y;Next(Key.W);}
            else if(phase==3 && player.IsAirSpinning && player.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime>.25f)
            {
                Check(player.IsAirSpinning,"Second press did not spin");
                Check(body.linearVelocity.y>=-1.7f && body.linearVelocity.y<=0,"Fall not slowed without upward impulse: "+body.linearVelocity.y);
                Check(player.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("AirSpin"),"AirSpin state not playing");
                float angle=Quaternion.Angle(rootRest,spinRoot.localRotation);Check(angle>30,"Skeleton is not rotating: "+angle+" elapsed="+elapsed+" normalized="+player.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime+" speed="+player.GetComponent<Animator>().GetFloat("AirSpinSpeed"));
                results.Add(new{name="Descending spin",beforeFall,slowed=body.linearVelocity.y,angle});Capture();Next();
            }
            else if(phase==4 && elapsed>.07f) Next(Key.W);
            else if(phase==5 && elapsed>.35f)
            {
                Check(!player.IsAirSpinning,"Repeated press extended spin");Check(body.linearVelocity.y< -1.7f,"Gravity did not resume");
                Check(Quaternion.Angle(rootRest,spinRoot.localRotation)<.1f,"Root rotation not restored");results.Add(new{name="One spin per jump; normal fall restored",passed=true});Next();
            }
            else if(phase==6 && elapsed>.5f && player.IsGrounded) Next(Key.Space);
            else if(phase==7 && elapsed>.12f) Next();
            else if(phase==8 && elapsed>.07f) Next(Key.Space);
            else if(phase==9 && elapsed>.12f)
            {
                Check(player.IsAirSpinning,"Landing did not restore spin, or alternate jump key failed");
                results.Add(new{name="Landing resets availability; Space activates ascending spin",passed=true});Next(Key.R);
            }
            else if(phase==10 && elapsed>.15f)
            {
                Check(!player.IsAirSpinning && Quaternion.Angle(rootRest,spinRoot.localRotation)<.1f,"Respawn did not cancel spin");
                Next();body.position=new Vector3(-40,8,0);body.linearVelocity=Vector3.zero;
            }
            else if(phase==11 && elapsed>.3f) Next(Key.W);
            else if(phase==12 && elapsed>.15f)
            {
                Check(!player.IsAirSpinning,"Falling without jumping allowed spin");
                results.Add(new{name="Respawn cancels spin; falling without jump cannot spin",passed=true});
                Check(Time.frameCount-firstFrame>100,"Frames did not advance");Finish(true,null);
            }
        }
        catch(Exception e) {Finish(false,e.ToString());}
    }
    static void Capture()
    {
        var camera=Camera.main;var rt=new RenderTexture(1200,800,24);rt.Create();var old=RenderTexture.active;
        try {UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(1200,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,800),0,0);image.Apply();Directory.CreateDirectory("Temp/MarioAirSpinAudit");File.WriteAllBytes("Temp/MarioAirSpinAudit/spin.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
        finally {RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static void Finish(bool passed,string error)
    {
        Directory.CreateDirectory("Temp/MarioAirSpinAudit");File.WriteAllText("Temp/MarioAirSpinAudit/results.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,frames=Time.frameCount-firstFrame,results},Newtonsoft.Json.Formatting.Indented));
        Cleanup();if(passed) Debug.Log("Mario air spin validation passed");else Debug.LogError(error);EditorApplication.isPlaying=false;
    }
    static void Cleanup()
    {
        if(inputSettingsChanged){InputSystem.settings.backgroundBehavior=previousBackgroundBehavior;inputSettingsChanged=false;}
        InputSystem.onBeforeUpdate-=QueueInput;EditorApplication.update-=Tick;if(keyboard!=null && keyboard.added) InputSystem.RemoveDevice(keyboard);keyboard=null;if(floor!=null) UnityEngine.Object.Destroy(floor);floor=null;
    }
}
