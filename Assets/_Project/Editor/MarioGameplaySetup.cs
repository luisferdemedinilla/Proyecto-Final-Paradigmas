using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class MarioGameplaySetup
{
    const string Animations="Assets/_Project/Art/Characters/Mario/Animations";
    public const string ControllerPath=Animations+"/MarioGameplay.controller";
    static void Check(bool ok,string message) {if(!ok) throw new Exception(message);}

    [MenuItem("Tools/Mario Test/Configure Player Controls")]
    public static void Configure()
    {
        Check(!EditorApplication.isPlaying,"Exit Play Mode first");
        var scene=SceneManager.GetSceneByPath(MarioTestSceneSetup.ScenePath);
        bool opened=!scene.IsValid() || !scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(MarioTestSceneSetup.ScenePath,OpenSceneMode.Additive);
        try
        {
            var roots=scene.GetRootGameObjects();
            var mario=roots.Single(r=>r.name=="Actors").transform.Find("Mario").gameObject;
            var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Single();
            Check(mario.GetComponents<Rigidbody>().Length==1 && mario.GetComponents<CapsuleCollider>().Length==1,"Unexpected player physics components");
            var animator=mario.GetComponent<Animator>();Check(animator!=null && animator.runtimeAnimatorController!=null,"Animator not configured");
            Directory.CreateDirectory("Temp/MarioGameplayAudit");
            File.WriteAllText("Temp/MarioGameplayAudit/scene-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene.path,scene.isDirty,playerPosition=mario.transform.position.ToString(),components=mario.GetComponents<Component>().Select(c=>c.GetType().Name).ToArray(),controller=AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),cameraPosition=camera.transform.position.ToString(),camera.orthographicSize},Newtonsoft.Json.Formatting.Indented));
            Check(!File.Exists(Animations+"/Crouch.anim") && !File.Exists(ControllerPath),"Gameplay animation assets already exist; inspect before updating");
            var crouch=CreateCrouch();
            Check(AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),ControllerPath),"Could not copy controller");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            controller.AddParameter("Crouching",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine;
            var states=machine.states.ToDictionary(s=>s.state.name,s=>s.state);
            var duck=machine.AddState("Crouch",new Vector3(260,400));duck.motion=crouch;duck.writeDefaultValues=false;
            foreach(var name in new[]{"Idle","Walk","Run"})
            {
                foreach(var transition in states[name].transitions) transition.AddCondition(AnimatorConditionMode.IfNot,0,"Crouching");
                var t=states[name].AddTransition(duck);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=0.1f;
                t.AddCondition(AnimatorConditionMode.If,0,"Crouching");t.AddCondition(AnimatorConditionMode.If,0,"Grounded");
            }
            var stand=duck.AddTransition(states["Idle"]);stand.hasExitTime=false;stand.hasFixedDuration=true;stand.duration=0.1f;stand.AddCondition(AnimatorConditionMode.IfNot,0,"Crouching");stand.AddCondition(AnimatorConditionMode.If,0,"Grounded");
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            var player=mario.GetComponent<MarioPlayerController>();if(player==null) player=Undo.AddComponent<MarioPlayerController>(mario);
            var settings=new SerializedObject(player);settings.FindProperty("visual").objectReferenceValue=mario.transform.Find("Visual");
            settings.FindProperty("spawnPoint").objectReferenceValue=roots.Single(r=>r.name=="Markers").transform.Find("Mario Spawn");settings.ApplyModifiedProperties();
            var follow=camera.GetComponent<MarioFollowCamera>();if(follow==null) follow=Undo.AddComponent<MarioFollowCamera>(camera.gameObject);
            var followSettings=new SerializedObject(follow);followSettings.FindProperty("target").objectReferenceValue=mario.transform;followSettings.ApplyModifiedProperties();
            camera.orthographicSize=4.5f;camera.transform.position=new Vector3(mario.transform.position.x+2,Mathf.Max(1.8f,mario.transform.position.y+1.8f),-25);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Could not save MarioTest");AssetDatabase.SaveAssets();
        }
        finally {if(opened) EditorSceneManager.CloseScene(scene,true);}
        Debug.Log("MarioTest configured with editable keyboard controls, crouch and camera follow.");
    }

    static AnimationClip CreateCrouch()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MarioAnimationTools.PrefabPath),scene);
            var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations+"/Idle.anim");
            var bindings=AnimationUtility.GetCurveBindings(idle);
            var bones=go.GetComponentsInChildren<Transform>(true).ToDictionary(t=>AnimationUtility.CalculateTransformPath(t,go.transform));
            var rest=go.GetComponentsInChildren<Transform>(true).ToDictionary(t=>t,t=>new MarioAnimationTools.Rest(t,go.transform));
            var keys=bindings.ToDictionary(b=>b,b=>new AnimationCurve());
            var clip=new AnimationClip {name="Crouch",frameRate=30};
            for(int frame=0;frame<=30;frame++)
            {
                foreach(var r in rest.Values) r.Reset();
                idle.SampleAnimation(go,0);
                Transform Bone(string n) => go.GetComponentsInChildren<Transform>(true).Single(t=>t.name==n);
                float breath=Mathf.Sin(frame/30f*Mathf.PI*2)*1.2f;
                var spine=Bone("spin");spine.rotation=Quaternion.AngleAxis(22+breath,Vector3.right)*rest[spine].worldRotation;
                var head=Bone("face_1");head.rotation=rest[head].worldRotation;
                foreach(var side in new[]{"l","r"})
                {
                    var thigh=Bone("leg_"+side+"1");thigh.rotation=Quaternion.AngleAxis(-46,Vector3.right)*rest[thigh].worldRotation;
                    var shin=Bone("leg_"+side+"2");shin.rotation=Quaternion.AngleAxis(92,Vector3.right)*shin.rotation;
                    var ankle=Bone("ankle_"+side+"1");ankle.rotation=rest[ankle].worldRotation;
                }
                foreach(var binding in bindings)
                {
                    var q=bones[binding.path].localRotation;int axis="xyzw".IndexOf(binding.propertyName.Last());
                    var curve=keys[binding];float value=q[axis];
                    // Pose remains within one quaternion hemisphere at these small breathing angles.
                    curve.AddKey(frame/30f,value);
                }
            }
            foreach(var pair in keys) AnimationUtility.SetEditorCurve(clip,pair.Key,pair.Value);
            clip.EnsureQuaternionContinuity();var options=AnimationUtility.GetAnimationClipSettings(clip);options.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,options);
            AssetDatabase.CreateAsset(clip,Animations+"/Crouch.anim");return clip;
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);}
    }

    [MenuItem("Tools/Mario Test/Inspect Crouch Pose")]
    public static void InspectCrouch()
    {
        var preview=new PreviewRenderUtility();
        try {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MarioAnimationTools.PrefabPath),preview.camera.scene);preview.AddSingleGO(go);
            go.GetComponent<Animator>().runtimeAnimatorController=null;
            foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.updateWhenOffscreen=true;
            AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations+"/Crouch.anim").SampleAnimation(go,0.3f);
            go.transform.Find("Visual").localPosition=Vector3.down*0.3f;
            MarioAnimationValidation.ConfigureCamera(preview,50);
            var image=MarioAnimationValidation.Render(preview,700);
            File.WriteAllBytes("Temp/MarioGameplayAudit/crouch-isolated.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            File.WriteAllText("Temp/MarioGameplayAudit/crouch-bones.json",Newtonsoft.Json.JsonConvert.SerializeObject(go.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("leg_")||t.name.StartsWith("ankle_")).Select(t=>new{t.name,pos=t.position.ToString(),rotation=t.localEulerAngles.ToString()}),Newtonsoft.Json.Formatting.Indented));
        } finally {preview.Cleanup();}
    }
}
