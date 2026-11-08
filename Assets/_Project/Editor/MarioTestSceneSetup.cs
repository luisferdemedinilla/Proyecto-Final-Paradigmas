using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;

public static class MarioTestSceneSetup
{
    public const string ScenePath="Assets/_Project/Scenes/MarioTest.unity";
    const string Art="Assets/_Project/Art/Environment/MarioTest";
    const string Prefab="Assets/_Project/Prefabs/Characters/Mario.prefab";
    const string Session="MarioTest.Validation";
    static Rigidbody player;
    static int phase, firstFrame;
    static float phaseStart;
    static double deadline;
    static readonly List<object> results=new List<object>();

    static void Check(bool ok,string error) {if(!ok) throw new InvalidOperationException(error);}
    static void Folder(string path)
    {
        var parts=path.Split('/');var parent=parts[0];
        foreach(var part in parts.Skip(1)) {if(!AssetDatabase.IsValidFolder(parent+"/"+part)) AssetDatabase.CreateFolder(parent,part);parent+="/"+part;}
    }
    static Transform Group(string name,Scene scene,Transform parent=null)
    {
        var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);if(parent!=null) go.transform.SetParent(parent,false);return go.transform;
    }
    static Material Material(string name,Color color)
    {
        var path=Art+"/"+name+".mat";Check(!File.Exists(path),"Existing material: "+path);
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",0.1f);AssetDatabase.CreateAsset(m,path);return m;
    }
    static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material,bool collision=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;SceneManager.MoveGameObjectToScene(go,parent.gameObject.scene);
        go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
        if(!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        go.isStatic=true;return go;
    }
    [MenuItem("Tools/Mario Test/Create Test Scene")]
    public static void CreateTestScene()
    {
        Check(!EditorApplication.isPlaying,"Exit Play Mode before creating the scene");
        Check(!File.Exists(ScenePath),"MarioTest already exists; not overwriting it");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);Check(prefab!=null,"Mario prefab missing");
        var animators=prefab.GetComponentsInChildren<Animator>(true);Check(animators.Length==1,"Expected one existing Animator");
        Check(animators[0].runtimeAnimatorController!=null,"Missing Mario controller");
        var controller=animators[0].runtimeAnimatorController as AnimatorController;Check(controller!=null,"Expected AnimatorController");
        foreach(var n in new[]{"Idle","Walk","Run","Jump","Fall"}) Check(controller.layers[0].stateMachine.states.Any(s=>s.state.name==n),"Missing state "+n);
        Check(prefab.GetComponentsInChildren<Rigidbody>(true).Length<=1,"Multiple existing rigidbodies require review");
        Check(prefab.GetComponentsInChildren<Collider>(true).Length<=1,"Multiple existing colliders require review");
        Check(prefab.GetComponentsInChildren<Rigidbody2D>(true).Length==0 && prefab.GetComponentsInChildren<Collider2D>(true).Length==0,"Do not mix 2D and 3D physics");
        Folder("Assets/_Project/Scenes");Folder(Art);
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var groundMat=Material("Ground",new Color(0.35f,0.23f,0.14f));
            var topMat=Material("Grass",new Color(0.3f,0.65f,0.29f));
            var platformMat=Material("Platform",new Color(0.25f,0.48f,0.68f));
            var stepMat=Material("Steps",new Color(0.76f,0.53f,0.27f));
            var wallMat=Material("Walls",new Color(0.49f,0.39f,0.57f));
            var markerMat=Material("SpawnMarker",new Color(0.93f,0.77f,0.3f));
            var world=Group("Environment",scene);
            var grounds=Group("Ground",scene,world);var platforms=Group("Platforms",scene,world);
            var steps=Group("Steps",scene,world);var walls=Group("Walls",scene,world);var details=Group("Visual Details",scene,world);
            float[][] stretches={new[]{-3f,9f},new[]{11f,18f},new[]{20f,24f}};
            for(int i=0;i<stretches.Length;i++)
            {
                float left=stretches[i][0],right=stretches[i][1];
                Box("Ground "+(i+1),grounds,new Vector3((left+right)/2,-0.6f,0),new Vector3(right-left,1.2f,2.4f),groundMat);
                Box("Grass "+(i+1),details,new Vector3((left+right)/2,-0.045f,0),new Vector3(right-left,0.09f,2.42f),topMat,false);
            }
            for(int i=0;i<4;i++) {float h=(i+1)*0.35f;Box("Step "+(i+1),steps,new Vector3(2+i,h/2,0),new Vector3(1,h,2),stepMat);}
            Box("Platform Low",platforms,new Vector3(7,2.0f,0),new Vector3(2,0.4f,2),platformMat);
            Box("Platform Over Gap",platforms,new Vector3(10,3.0f,0),new Vector3(1.4f,0.4f,2),platformMat);
            Box("Platform High",platforms,new Vector3(15.5f,2.9f,0),new Vector3(3,0.4f,2),platformMat);
            Box("Platform Finish",platforms,new Vector3(21,1.6f,0),new Vector3(1.8f,0.4f,2),platformMat);
            Box("Wall Start",walls,new Vector3(-3.25f,1.5f,0),new Vector3(0.5f,3,2.4f),wallMat);
            Box("Wall Middle",walls,new Vector3(14,1,0),new Vector3(0.7f,2,2.4f),wallMat);
            Box("Wall Finish",walls,new Vector3(24.25f,1.75f,0),new Vector3(0.5f,3.5f,2.4f),wallMat);
            Box("Start Pad",details,new Vector3(-1.5f,0.012f,0),new Vector3(1.3f,0.024f,2.45f),markerMat,false);
            var actors=Group("Actors",scene);var markers=Group("Markers",scene);
            var spawn=Group("Mario Spawn",scene,markers);spawn.position=new Vector3(-1.5f,0.15f,0);
            var gapA=Group("Gap 1 (x 9 to 11)",scene,markers);gapA.position=new Vector3(10,0,0);
            var gapB=Group("Gap 2 (x 18 to 20)",scene,markers);gapB.position=new Vector3(19,0,0);
            var mario=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);mario.name="Mario";mario.transform.SetParent(actors,false);mario.transform.position=spawn.position;mario.transform.rotation=Quaternion.Euler(0,90,0);
            var existing=mario.GetComponentsInChildren<Collider>(true);CapsuleCollider capsule;
            if(existing.Length==0) capsule=mario.AddComponent<CapsuleCollider>();else {capsule=existing[0] as CapsuleCollider;Check(capsule!=null && capsule.gameObject==mario,"Existing collider requires review");}
            capsule.direction=1;capsule.center=new Vector3(0,0.84f,0);capsule.height=1.68f;capsule.radius=0.32f;capsule.isTrigger=false;
            var physPath=Art+"/MarioTest.physicMaterial";Check(!File.Exists(physPath),"Existing physics material");
            var phys=new PhysicsMaterial("MarioTest") {dynamicFriction=0.15f,staticFriction=0.15f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};
            AssetDatabase.CreateAsset(phys,physPath);capsule.sharedMaterial=phys;
            var rb=mario.GetComponent<Rigidbody>();if(rb==null) rb=mario.AddComponent<Rigidbody>();
            rb.mass=1;rb.useGravity=true;rb.isKinematic=false;rb.linearDamping=0;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            rb.constraints=RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
            var animator=mario.GetComponent<Animator>();animator.applyRootMotion=false;
            var cameras=Group("Cameras",scene);var cameraGo=Group("Main Camera",scene,cameras).gameObject;
            cameraGo.tag="MainCamera";cameraGo.transform.position=new Vector3(10.5f,2.8f,-25);cameraGo.transform.rotation=Quaternion.identity;
            var camera=cameraGo.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8.3f;camera.nearClipPlane=0.1f;camera.farClipPlane=80;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.49f,0.73f,0.88f);cameraGo.AddComponent<AudioListener>();
            var lighting=Group("Lighting",scene);var sun=Group("Directional Light",scene,lighting).gameObject;sun.transform.rotation=Quaternion.Euler(45,-35,0);
            var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.shadows=LightShadows.Soft;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(0.65f,0.7f,0.76f);RenderSettings.sun=light;
            Check(mario.GetComponentsInChildren<Rigidbody>(true).Length==1 && mario.GetComponentsInChildren<Collider>(true).Length==1,"Duplicate physics components");
            Check(PrefabUtility.GetCorrespondingObjectFromSource(mario)==prefab,"Prefab instance link lost");
            Check(EditorSceneManager.SaveScene(scene,ScenePath),"Scene save failed");AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Temp/MarioTestAudit");
            File.WriteAllText("Temp/MarioTestAudit/setup.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {scene=ScenePath,prefabComponents=prefab.GetComponentsInChildren<Component>(true).Select(c=>c.GetType().Name).Distinct().ToArray(),controller=AssetDatabase.GetAssetPath(controller),states=controller.layers[0].stateMachine.states.Select(s=>s.state.name).ToArray(),cubeColliders=world.GetComponentsInChildren<BoxCollider>().Length,rigidbodies=1,playerColliders=1,rootMotion=false,constraints=rb.constraints.ToString(),cameraSize=camera.orthographicSize},Newtonsoft.Json.Formatting.Indented));
        }
        finally {SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        Debug.Log("MarioTest created; source prefab and existing scenes preserved.");
    }

    [MenuItem("Tools/Mario Test/Correct Material Extension")]
    public static void CorrectMaterialExtension()
    {
        var error=AssetDatabase.MoveAsset(Art+"/MarioTest.physicsMaterial",Art+"/MarioTest.physicMaterial");
        Check(string.IsNullOrEmpty(error),error);
    }

    [InitializeOnLoadMethod]
    static void Subscribe()
    {
        EditorApplication.playModeStateChanged-=OnPlayState;EditorApplication.playModeStateChanged+=OnPlayState;
        if(SessionState.GetBool(Session,false) && EditorApplication.isPlaying) BeginChecks();
    }
    [MenuItem("Tools/Mario Test/Run Physics Validation")]
    public static void RunValidation()
    {
        Check(!EditorApplication.isPlaying,"Already in Play Mode");
        Check(File.Exists(ScenePath),"Create MarioTest first");
        SessionState.SetString(Session+".PreviousStart",EditorSceneManager.playModeStartScene==null?"":AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Session,true);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorApplication.isPlaying=true;
    }
    static void OnPlayState(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Session,false)) return;
        if(state==PlayModeStateChange.EnteredPlayMode) BeginChecks();
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update-=Tick;
            var previous=SessionState.GetString(Session+".PreviousStart","");EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(previous)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            SessionState.SetBool(Session,false);
        }
    }
    static void BeginChecks()
    {
        Application.runInBackground=true;phase=0;phaseStart=Time.time;firstFrame=Time.frameCount;deadline=EditorApplication.timeSinceStartup+45;results.Clear();
        player=GameObject.Find("Actors/Mario")?.GetComponent<Rigidbody>();
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void Teleport(Vector3 position,Vector3 velocity)
    {
        player.position=position;player.linearVelocity=velocity;player.angularVelocity=Vector3.zero;player.WakeUp();Physics.SyncTransforms();phaseStart=Time.time;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Play Mode is not advancing");
            Check(player!=null,"Test Mario not found");
            if(Time.time-phaseStart<(phase==0?5f:1.1f)) return;
            Check(Time.frameCount>firstFrame+5,"No advancing game frames");
            Check(Mathf.Abs(player.position.z)<0.001f,"Mario moved out of XY plane");
            Check(Quaternion.Angle(player.rotation,Quaternion.Euler(0,90,0))<0.1f,"Mario rotated despite constraints");
            if(phase==0) {Check(Mathf.Abs(player.position.y)<0.07f,"Spawn did not settle on floor");CaptureLevel();}
            if(phase==1) Check(Mathf.Abs(player.position.y-0.7f)<0.07f,"Step collision failed");
            if(phase==2) Check(Mathf.Abs(player.position.y-2.2f)<0.07f,"Platform collision failed");
            if(phase==3) Check(player.position.x>12.1f && player.position.x<13.4f,"Wall collision failed");
            if(phase==4) Check(player.position.y<-1,"Gap did not allow falling");
            results.Add(new{test=new[]{"Spawn settles","Step supports Mario","Platform supports Mario","Wall blocks X and Z stays locked","Gap allows falling"}[phase],position=player.position.ToString("F4"),frames=Time.frameCount-firstFrame,passed=true});
            phase++;
            if(phase==1) Teleport(new Vector3(3,1.5f,0),Vector3.zero);
            else if(phase==2) Teleport(new Vector3(7,3.1f,0),Vector3.zero);
            else if(phase==3) {Teleport(new Vector3(12,0.1f,0),new Vector3(6,0,4));player.AddTorque(Vector3.one*5,ForceMode.Impulse);}
            else if(phase==4) Teleport(new Vector3(10,0.4f,0),Vector3.zero);
            else Finish(true,null);
        }
        catch(Exception e) {Finish(false,e.Message);}
    }
    static void CaptureLevel()
    {
        var camera=Camera.main;
        var target=new RenderTexture(1600,900,24);target.Create();
        var previous=RenderTexture.active;
        try
        {
            var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=target};
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,request);
            RenderTexture.active=target;
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            File.WriteAllBytes("Temp/MarioTestAudit/level.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        }
        finally {RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
    static void Finish(bool passed,string error)
    {
        EditorApplication.update-=Tick;Directory.CreateDirectory("Temp/MarioTestAudit");
        File.WriteAllText("Temp/MarioTestAudit/physics-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed,error,results},Newtonsoft.Json.Formatting.Indented));
        if(passed) Debug.Log("MarioTest: five real-time physics checks passed.");else Debug.LogError("MarioTest validation: "+error);
        EditorApplication.isPlaying=false;
    }
}
