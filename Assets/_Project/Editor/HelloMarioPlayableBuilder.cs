using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class HelloMarioPlayableBuilder
{
    const string Source="Assets/_Project/Art/Characters/Shared/HelloMario/Animations/Source/";
    public const string Playables="Assets/_Project/Prefabs/Characters/Playable/";
    public static string AnimFolder(string name)=>"Assets/_Project/Art/Characters/"+name+"/HelloMario/Animations/Gameplay/";
    public static Bounds BoundsOf(GameObject go)
    {
        bool first=true;var bounds=new Bounds();
        foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled))
        {
            var mesh=r.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
            var matrices=bind.Select((m,i)=>r.bones[i].localToWorldMatrix*m).ToArray();
            for(int i=0;i<vertices.Length;i++)
            {
                var v=vertices[i];var w=weights[i];Vector3 p=Vector3.zero;
                if(w.weight0>0)p+=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0;
                if(w.weight1>0)p+=matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1;
                if(w.weight2>0)p+=matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2;
                if(w.weight3>0)p+=matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
            }
        }
        return bounds;
    }
    [MenuItem("Tools/Hello Mario/Build Playable Characters")]
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new Exception("Exit play mode first");
        HelloMarioLibrary.Folder(Playables.TrimEnd('/'));Directory.CreateDirectory("Temp/HelloMarioAudit");
        var scene=EditorSceneManager.NewPreviewScene();var reports=new List<object>();
        try
        {
            foreach(var name in new[]{"Mario","Luigi","Toad"})
            {
                string path=Playables+name+"Player.prefab";
                var root=new GameObject(name);SceneManager.MoveGameObjectToScene(root,scene);
                var visual=new GameObject("Visual").transform;visual.SetParent(root.transform,false);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/HelloMario/"+name+"/"+name+".prefab"),scene);
                model.name="Model";model.transform.SetParent(visual,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
                // Keep the source proportions and disabled facial/power-up variants.
                visual.localScale=Vector3.one*1.15f;
                foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen=true;
                var baseBounds=BoundsOf(root);visual.localPosition=Vector3.up*(-baseBounds.min.y);
                var animator=root.AddComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var folder=AnimFolder(name);HelloMarioLibrary.Folder(folder.TrimEnd('/'));
                var mapping=new Dictionary<string,string>{{"Idle","Wait"},{"Walk","Walk"},{"Run","Run"},{"Jump",name=="Luigi"?"LuigiJump":"Jump"},{"Fall","FallLoop"},{"Crouch","SquatWait"},{"AirSpin","SpinJumpL"}};
                var clips=new Dictionary<string,AnimationClip>();
                foreach(var pair in mapping) clips[pair.Key]=ConvertClip(root,model,pair.Key,pair.Value,folder+pair.Key+".anim",reports);
                string controllerPath=folder+name+"Gameplay.controller";
                if(!File.Exists(controllerPath) && !AssetDatabase.CopyAsset(MarioGameplaySetup.ControllerPath,controllerPath)) throw new Exception("Controller copy failed");
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                foreach(var s in controller.layers[0].stateMachine.states) {if(clips.TryGetValue(s.state.name,out var motion))s.state.motion=motion;s.state.writeDefaultValues=false;}
                animator.runtimeAnimatorController=controller;
                clips["Idle"].SampleAnimation(root,0);
                var bounds=BoundsOf(root);float height=bounds.max.y;float radius=Mathf.Min(.32f,height*.22f);
                var capsule=root.AddComponent<CapsuleCollider>();capsule.height=height;capsule.radius=radius;capsule.center=Vector3.up*height/2;capsule.sharedMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Art/Environment/MarioTest/MarioTest.physicMaterial");
                var rb=root.AddComponent<Rigidbody>();rb.constraints=RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                var player=root.AddComponent<MarioPlayerController>();var data=new SerializedObject(player);
                data.FindProperty("visual").objectReferenceValue=visual;data.FindProperty("authoredCrouchHeight").boolValue=true;data.FindProperty("airSpinClipLength").floatValue=clips["AirSpin"].length;
                clips["Crouch"].SampleAnimation(root,0);float crouch=BoundsOf(root).max.y;
                data.FindProperty("crouchHeight").floatValue=Mathf.Clamp(crouch,2*radius,height);data.ApplyModifiedPropertiesWithoutUndo();
                clips["Idle"].SampleAnimation(root,0);
                PrefabUtility.SaveAsPrefabAsset(root,path);reports.Add(new{name,path,height,radius,crouch});
                Render(root,name+"-idle",clips["Idle"],0);
                clips["Crouch"].SampleAnimation(root,.2f);Render(root,name+"-crouch",clips["Crouch"],.2f);
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();File.WriteAllText("Temp/HelloMarioAudit/playables.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
            Debug.Log("Hello Mario: Mario, Luigi and Toad playable prefabs generated.");
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);}
    }
    public static AnimationClip ConvertClip(GameObject root,GameObject model,string state,string sourceName,string destination,List<object> reports)
    {
        var path=Source+sourceName+".fbx";var original=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
        var sourceModel=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var bindings=AnimationUtility.GetCurveBindings(original);var bonePaths=bindings.Select(b=>b.path).Distinct().ToArray();
        var targets=bonePaths.ToDictionary(p=>p,p=>model.transform.Find(p));
        if(targets.Any(p=>p.Value==null)) throw new Exception("Unbound animation: "+state);
        var restPos=targets.ToDictionary(p=>p.Key,p=>p.Value.localPosition);var restRot=targets.ToDictionary(p=>p.Key,p=>p.Value.localRotation);var restScale=targets.ToDictionary(p=>p.Key,p=>p.Value.localScale);
        var sourceRest=bonePaths.ToDictionary(p=>p,p=>sourceModel.transform.Find(p));
        var curves=bindings.ToDictionary(b=>b,b=>AnimationUtility.GetEditorCurve(original,b));
        var output=bindings.Where(b=>b.propertyName.StartsWith("m_LocalRotation")||b.propertyName.StartsWith("m_LocalPosition")).ToDictionary(b=>b,b=>new AnimationCurve());
        float Read(string p,string property,float time,float fallback) {var b=EditorCurveBinding.FloatCurve(p,typeof(Transform),property);return curves.TryGetValue(b,out var c)?c.Evaluate(time):fallback;}
        var restBounds=BoundsOf(root);
        int frames=Mathf.CeilToInt(original.length*60);float minY=999,maxY=-999;
        for(int f=0;f<=frames;f++)
        {
            float time=original.length*f/frames;
            foreach(var p in bonePaths)
            {
                var t=targets[p];var src=sourceRest[p];if(src==null)throw new Exception("Missing source bind bone "+p);
                var q=new Quaternion(Read(p,"m_LocalRotation.x",time,src.localRotation.x),Read(p,"m_LocalRotation.y",time,src.localRotation.y),Read(p,"m_LocalRotation.z",time,src.localRotation.z),Read(p,"m_LocalRotation.w",time,src.localRotation.w));
                t.localRotation=q;
                t.localPosition=restPos[p];t.localScale=restScale[p];
                // Only pelvis animation translates the visual pose. Limb lengths stay character-specific.
                if(p=="AllRoot/JointRoot")
                {
                    float scale=restPos[p].y/Mathf.Max(.01f,src.localPosition.y);
                    t.localPosition=restPos[p]+Vector3.up*((Read(p,"m_LocalPosition.y",time,src.localPosition.y)-src.localPosition.y)*scale);
                }
            }
            // Grounded clips keep their lowest foot at ground level; root motion never drives physics.
            if(state=="Idle"||state=="Walk"||state=="Run"||state=="Crouch"||state.StartsWith("GroundPound"))
            {
                var poseBounds=BoundsOf(root);var pelvis=targets["AllRoot/JointRoot"];
                var correction=Vector3.up*poseBounds.min.y;
                // The somersault rotates around AllRoot at the feet. Keep its
                // visual centre above the collider instead of orbiting in Z.
                if(state.StartsWith("GroundPound")){correction.x=poseBounds.center.x-restBounds.center.x;correction.z=poseBounds.center.z-restBounds.center.z;}
                pelvis.position-=correction;
            }
            if(f%Mathf.Max(1,frames/8)==0){var b=BoundsOf(root);minY=Mathf.Min(minY,b.min.y);maxY=Mathf.Max(maxY,b.max.y);}
            foreach(var b in output.Keys)
            {
                int axis="xyzw".IndexOf(b.propertyName.Last());var t=targets[b.path];float value=b.propertyName.StartsWith("m_LocalRotation")?t.localRotation[axis]:t.localPosition[axis];output[b].AddKey(time,value);
            }
        }
        var clip=new AnimationClip{name=state,frameRate=60};
        foreach(var pair in output){var b=pair.Key;b.path="Visual/Model/"+b.path;AnimationUtility.SetEditorCurve(clip,b,pair.Value);}
        clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=state=="Idle"||state=="Walk"||state=="Run"||state=="Fall"||state=="Crouch";AnimationUtility.SetAnimationClipSettings(clip,settings);
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(destination);
        if(existing==null) AssetDatabase.CreateAsset(clip,destination);
        else {EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(clip);}
        foreach(var p in bonePaths){targets[p].localPosition=restPos[p];targets[p].localRotation=restRot[p];targets[p].localScale=restScale[p];}
        reports.Add(new{character=root.name,state,source=path,clip.length,bindings=output.Count,minY,maxY});return clip;
    }
    static void Render(GameObject root,string name,AnimationClip clip,float time)
    {
        var preview=new PreviewRenderUtility();
        try {var clone=UnityEngine.Object.Instantiate(root);preview.AddSingleGO(clone);UnityEngine.Object.DestroyImmediate(clone.GetComponent<MarioPlayerController>());UnityEngine.Object.DestroyImmediate(clone.GetComponent<Rigidbody>());clone.GetComponent<Animator>().runtimeAnimatorController=null;clip.SampleAnimation(clone,time);MarioAnimationValidation.ConfigureCamera(preview,25);var image=MarioAnimationValidation.Render(preview,600);File.WriteAllBytes("Temp/HelloMarioAudit/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
        finally {preview.Cleanup();}
    }
    [MenuItem("Tools/Hello Mario/Use Mario In Test Scene")]
    public static void UseMario()=>UseCharacter("Mario");
    [MenuItem("Tools/Hello Mario/Use Luigi In Test Scene")]
    public static void UseLuigi()=>UseCharacter("Luigi");
    [MenuItem("Tools/Hello Mario/Use Toad In Test Scene")]
    public static void UseToad()=>UseCharacter("Toad");
    static void UseCharacter(string character)
    {
        var scene=SceneManager.GetSceneByPath(MarioTestSceneSetup.ScenePath);if(!scene.IsValid()||!scene.isLoaded||EditorApplication.isPlaying)throw new Exception("Open MarioTest in Edit Mode");
        var actors=scene.GetRootGameObjects().Single(g=>g.name=="Actors").transform;
        bool upgraded=actors.Find("MarioLegacy")!=null;
        var old=actors.Find("Mario");var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Playables+character+"Player.prefab"),scene);
        go.name="Mario";go.transform.SetParent(actors,false);go.transform.SetPositionAndRotation(old.position,old.rotation);
        // Copy the user's keyboard and movement tuning, then configure only the new visual rig.
        var player=go.GetComponent<MarioPlayerController>();EditorUtility.CopySerialized(old.GetComponent<MarioPlayerController>(),player);
        var settings=new SerializedObject(player);settings.FindProperty("visual").objectReferenceValue=go.transform.Find("Visual");settings.FindProperty("authoredCrouchHeight").boolValue=true;settings.FindProperty("airSpinClipLength").floatValue=AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimFolder(character)+"AirSpin.anim").length;
        var prefabSettings=new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(Playables+character+"Player.prefab").GetComponent<MarioPlayerController>());settings.FindProperty("crouchHeight").floatValue=prefabSettings.FindProperty("crouchHeight").floatValue;settings.ApplyModifiedPropertiesWithoutUndo();
        go.GetComponent<CapsuleCollider>().sharedMaterial=old.GetComponent<CapsuleCollider>().sharedMaterial;
        if(upgraded) UnityEngine.Object.DestroyImmediate(old.gameObject);else {old.name="MarioLegacy";old.gameObject.SetActive(false);}
        foreach(var follow in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MarioFollowCamera>(true))) {var so=new SerializedObject(follow);so.FindProperty("target").objectReferenceValue=go.transform;so.ApplyModifiedPropertiesWithoutUndo();}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("MarioTest now uses Hello Mario "+character+"; original Mario preserved inactive as MarioLegacy.");
    }
}
