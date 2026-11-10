using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class MarioMobilityValidation
{
    const string Session="MarioMobility.Validation";
    static Keyboard keyboard;
    static MarioPlayerController player;
    static Rigidbody body;
    static GameObject floor;
    static Key[] keys=Array.Empty<Key>();
    static int phase,firstFrame,jumpIndex;
    static float began,peak,slowSpeed,fastSpeed,restFoot;
    static double deadline;
    static readonly float[] holds={.04f,.15f,.35f,1f};
    static readonly List<float> heights=new List<float>();
    static readonly List<object> results=new List<object>();
    static void Check(bool ok,string message) {if(!ok) throw new Exception(message);}
    [InitializeOnLoadMethod]
    static void Subscribe() {EditorApplication.playModeStateChanged-=State;EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Tools/Mario Test/Validate Mobility Refinements")]
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
            Application.runInBackground=true;player=GameObject.Find("Actors/Mario").GetComponent<MarioPlayerController>();body=player.GetComponent<Rigidbody>();
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary mobility test floor";floor.transform.position=new Vector3(-40,-.5f,0);floor.transform.localScale=new Vector3(50,1,6);
            body.position=new Vector3(-40,.1f,0);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();phase=0;jumpIndex=0;began=Time.time;deadline=EditorApplication.timeSinceStartup+75;firstFrame=Time.frameCount;
            heights.Clear();results.Clear();keys=Array.Empty<Key>();EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.ExitingPlayMode) Cleanup();
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            var path=SessionState.GetString(Session+".Start","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);SessionState.SetBool(Session,false);
        }
    }
    static void Next(params Key[] input) {phase++;keys=input;began=Time.time;}
    // Evaluate skin matrices directly: verifies actual mesh geometry, not just bone positions.
    static float FootMinimum()
    {
        float min=float.PositiveInfinity;
        foreach(var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh=renderer.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var binds=mesh.bindposes;var bones=renderer.bones;
            if(weights.Length!=vertices.Length) continue;
            var matrices=binds.Select((b,i)=>bones[i].localToWorldMatrix*b).ToArray();
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];var v=vertices[i];Vector3 p=Vector3.zero;
                if(w.weight0>0) p+=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0;
                if(w.weight1>0) p+=matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1;
                if(w.weight2>0) p+=matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2;
                if(w.weight3>0) p+=matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                min=Mathf.Min(min,p.y);
            }
        }
        return min;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Test timed out");
            keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
            float elapsed=Time.time-began;
            Check(Mathf.Abs(body.position.z)<.001f,"Movement left XY plane");
            if(phase==0 && elapsed>.6f) {Check(player.IsGrounded,"Not grounded");restFoot=FootMinimum();Next(Key.D);}
            else if(phase==1 && elapsed>.2f) {slowSpeed=body.linearVelocity.x;Check(slowSpeed>.3f && slowSpeed<3,"Acceleration starts too abruptly");Next(Key.D);}
            else if(phase==2 && elapsed>.3f) {fastSpeed=body.linearVelocity.x;Check(fastSpeed>slowSpeed+.8f && fastSpeed<5.5f,"Speed did not increase gradually");Next(Key.D);}
            else if(phase==3 && elapsed>.4f) {Check(Mathf.Abs(body.linearVelocity.x-6)<.2f,"Maximum speed not reached: "+body.linearVelocity.x);results.Add(new{name="Acceleration",slowSpeed,fastSpeed,max=body.linearVelocity.x});Next();}
            else if(phase==4 && elapsed>.5f) {Check(Mathf.Abs(body.linearVelocity.x)<.05f,"Braking failed");Next(Key.S);}
            else if(phase==5 && elapsed>.5f)
            {
                float foot=FootMinimum();Check(player.IsCrouching,"Not crouching");Check(foot>-.025f && Mathf.Abs(foot-restFoot)<.035f,"Feet moved below floor: "+foot+" rest="+restFoot);
                results.Add(new{name="Crouch feet",restFoot,foot});Capture();Next();
            }
            else if(phase==6 && elapsed>.4f) {Check(!player.IsCrouching,"Not standing");peak=body.position.y;Next(Key.W);}
            else if(phase==7)
            {
                peak=Mathf.Max(peak,body.position.y);
                if(elapsed>=holds[jumpIndex]) keys=Array.Empty<Key>();
                if(elapsed>2.4f)
                {
                    Check(player.IsGrounded,"Did not land");heights.Add(peak);results.Add(new{name="Jump",held=holds[jumpIndex],height=peak});
                    jumpIndex++;if(jumpIndex<holds.Length) {phase=6;began=Time.time;keys=Array.Empty<Key>();} else
                    {
                        Check(heights[1]>heights[0]+.2f && heights[2]>heights[1]+.2f,"Jump heights are not progressive");
                        Check(Mathf.Abs(heights[3]-heights[2])<.25f,"Holding beyond maximum increased height");
                        Check(Time.frameCount-firstFrame>100,"Insufficient simulated frames");Finish(true,null);
                    }
                }
            }
        }
        catch(Exception e) {Finish(false,e.ToString());}
    }
    static void Capture()
    {
        var camera=Camera.main;var rt=new RenderTexture(1200,800,24);rt.Create();var old=RenderTexture.active;
        try {UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(1200,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,800),0,0);image.Apply();Directory.CreateDirectory("Temp/MarioMobilityAudit");File.WriteAllBytes("Temp/MarioMobilityAudit/crouch.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
        finally {RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static void Finish(bool passed,string error)
    {
        Directory.CreateDirectory("Temp/MarioMobilityAudit");File.WriteAllText("Temp/MarioMobilityAudit/results.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,frames=Time.frameCount-firstFrame,results},Newtonsoft.Json.Formatting.Indented));
        Cleanup();if(passed) Debug.Log("Mario mobility validation passed");else Debug.LogError(error);EditorApplication.isPlaying=false;
    }
    static void Cleanup()
    {
        EditorApplication.update-=Tick;if(keyboard!=null && keyboard.added) InputSystem.RemoveDevice(keyboard);keyboard=null;if(floor!=null) UnityEngine.Object.Destroy(floor);floor=null;
    }
}
