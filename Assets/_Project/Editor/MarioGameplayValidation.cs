using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class MarioGameplayValidation
{
    const string Session="MarioGameplay.Validation";
    static Keyboard keyboard;
    static MarioPlayerController player;
    static Rigidbody body;
    static Animator animator;
    static Camera camera;
    static GameObject roof;
    static Key[] keys=Array.Empty<Key>();
    static int phase,firstFrame;
    static float began;
    static double deadline;
    static Vector3 phasePosition,spawn,cameraStart;
    static bool sawFall;
    static readonly List<object> results=new List<object>();
    static void Check(bool ok,string error) {if(!ok) throw new Exception(error);}
    [InitializeOnLoadMethod]
    static void Subscribe() {EditorApplication.playModeStateChanged-=State;EditorApplication.playModeStateChanged+=State;}

    [MenuItem("Tools/Mario Test/Validate Keyboard Gameplay")]
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
            Application.runInBackground=true;player=GameObject.Find("Actors/Mario").GetComponent<MarioPlayerController>();body=player.GetComponent<Rigidbody>();animator=player.GetComponent<Animator>();camera=Camera.main;
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();phase=0;began=Time.time;deadline=EditorApplication.timeSinceStartup+45;firstFrame=Time.frameCount;
            spawn=GameObject.Find("Markers/Mario Spawn").transform.position;phasePosition=body.position;cameraStart=camera.transform.position;sawFall=false;results.Clear();keys=Array.Empty<Key>();
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.ExitingPlayMode) Cleanup();
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            var path=SessionState.GetString(Session+".Start","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);SessionState.SetBool(Session,false);
        }
    }
    static void Next(params Key[] input) {phase++;keys=input;began=Time.time;phasePosition=body.position;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Gameplay test timed out");
            keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
            sawFall|=animator.GetCurrentAnimatorStateInfo(0).IsName("Fall");
            float[] times={0.7f,0.35f,0.25f,0.3f,0.25f,1.9f,0.35f,0.3f,0.3f,0.3f,0.3f,0.3f,0.3f,2f};
            if(Time.time-began<times[phase]) return;
            Check(Mathf.Abs(body.position.z)<0.001f,"Player left XY plane");
            string name="";
            switch(phase)
            {
                case 0: name="Idle grounded";Check(player.IsGrounded,"Ground detection failed");Next(Key.D);break;
                case 1: name="D walks right";Check(body.position.x>phasePosition.x+0.4f && animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"Walk input/animation failed");Next(Key.D,Key.LeftShift);break;
                case 2: name="Shift runs";Check(body.linearVelocity.x>4.5f && animator.GetCurrentAnimatorStateInfo(0).IsName("Run"),"Run input/animation failed");Next();break;
                case 3: name="Release brakes";Check(Mathf.Abs(body.linearVelocity.x)<0.1f,"Braking failed");Next(Key.W);break;
                case 4: name="W jumps";Check(body.position.y>phasePosition.y+0.4f && !player.IsGrounded && animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"),"Jump failed");Next();break;
                case 5: name="Fall and land";Check(sawFall && player.IsGrounded,"Fall/landing animation failed");Next(Key.S);break;
                case 6:
                    name="S crouches and camera follows";
                    Check(player.IsCrouching && player.GetComponent<CapsuleCollider>().height<1.4f && animator.GetCurrentAnimatorStateInfo(0).IsName("Crouch"),"Crouch failed");
                    Check(Mathf.Abs(camera.transform.position.x-(body.position.x+2))<0.4f && Mathf.Abs(camera.transform.position.x-cameraStart.x)>0.3f,"Camera follow failed");
                    Capture("crouch");
                    roof=GameObject.CreatePrimitive(PrimitiveType.Cube);roof.name="Temporary test ceiling";roof.transform.position=body.position+Vector3.up*1.5f;roof.transform.localScale=new Vector3(2,0.2f,2);Physics.SyncTransforms();Next();break;
                case 7: name="Ceiling prevents standing";Check(player.IsCrouching,"Player stood inside the ceiling");UnityEngine.Object.Destroy(roof);roof=null;Next();break;
                case 8: name="Stand restores capsule";Check(!player.IsCrouching && Mathf.Abs(player.GetComponent<CapsuleCollider>().height-1.68f)<0.001f,"Stand failed");Next(Key.A);break;
                case 9:
                    name="A walks left";Check(body.position.x<phasePosition.x-0.3f && player.transform.Find("Visual").forward.x<-.9f,"Left movement/facing failed");
                    var data=new SerializedObject(player);data.FindProperty("rightKey").intValue=(int)Key.RightArrow;data.ApplyModifiedProperties();Next(Key.D);break;
                case 10: name="Rebinding disables old key";Check(Mathf.Abs(body.linearVelocity.x)<0.1f,"Old D binding still moves player");Next(Key.RightArrow);break;
                case 11: name="Inspector rebind moves player";Check(body.linearVelocity.x>2.8f,"Rebound key failed");Capture("moving");Next(Key.R);break;
                case 12: name="R respawns";Check(Mathf.Abs(body.position.x-spawn.x)<0.05f,"R respawn failed");Next();body.position=new Vector3(10,.4f,0);body.linearVelocity=Vector3.zero;break;
                case 13: name="Falling respawns";Check(Mathf.Abs(body.position.x-spawn.x)<0.05f && player.IsGrounded,"Automatic fall respawn failed");phase++;break;
            }
            results.Add(new {name,passed=true,position=body.position.ToString("F3"),frames=Time.frameCount-firstFrame});
            if(phase==14) Finish(true,null);
        }
        catch(Exception e) {Finish(false,e.Message);}
    }
    static void Capture(string name)
    {
        var rt=new RenderTexture(1200,800,24);rt.Create();var old=RenderTexture.active;
        try
        {
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=rt});
            RenderTexture.active=rt;var image=new Texture2D(1200,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,800),0,0);image.Apply();
            Directory.CreateDirectory("Temp/MarioGameplayAudit");File.WriteAllBytes("Temp/MarioGameplayAudit/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        }
        finally {RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static void Finish(bool passed,string error)
    {
        Directory.CreateDirectory("Temp/MarioGameplayAudit");File.WriteAllText("Temp/MarioGameplayAudit/results.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,results},Newtonsoft.Json.Formatting.Indented));
        Cleanup();if(passed) Debug.Log("Mario gameplay: 14 keyboard/physics/animation/camera checks passed.");else Debug.LogError("Mario gameplay test: "+error);EditorApplication.isPlaying=false;
    }
    static void Cleanup()
    {
        EditorApplication.update-=Tick;if(keyboard!=null && keyboard.added) InputSystem.RemoveDevice(keyboard);keyboard=null;
        if(roof!=null) UnityEngine.Object.Destroy(roof);roof=null;
    }
}
