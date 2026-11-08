using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor.SceneManagement;

public static class MarioAnimationTools
{
    public const string PrefabPath = "Assets/_Project/Prefabs/Characters/Mario.prefab";
    public const string Folder = "Assets/_Project/Art/Characters/Mario/Animations";
    public static readonly string[] ClipNames = { "Idle", "Walk", "Run", "Jump", "Fall" };
    const string AuditFolder = "Temp/MarioAnimationAudit";

    public sealed class Rest
    {
        public Transform bone;
        public string path;
        public Vector3 position, scale;
        public Quaternion rotation, worldRotation;
        public Rest(Transform t, Transform root)
        {
            bone=t; path=AnimationUtility.CalculateTransformPath(t,root);
            position=t.localPosition; scale=t.localScale; rotation=t.localRotation; worldRotation=t.rotation;
        }
        public void Reset() { bone.localPosition=position; bone.localRotation=rotation; bone.localScale=scale; }
    }

    static void Require(bool condition, string message) { if(!condition) throw new InvalidOperationException(message); }
    static void Write(string name, object data)
    {
        Directory.CreateDirectory(AuditFolder);
        File.WriteAllText(AuditFolder+"/"+name+".json", Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
    }

    [MenuItem("Tools/Mario Animations/Inspect Rig")]
    public static void InspectRig()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Require(prefab!=null,"Mario prefab missing");
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            var meshes=new List<object>();
            foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Require(r.sharedMesh!=null && r.bones.All(b=>b!=null),"Missing mesh or bone on "+r.name);
                Require(r.sharedMesh.bindposes.Length==r.bones.Length,"Bindpose count mismatch: "+r.name);
                var weights=r.sharedMesh.boneWeights;
                Require(weights.Length==r.sharedMesh.vertexCount,"Missing skin weights");
                float maxError=0;
                foreach(var w in weights)
                {
                    maxError=Mathf.Max(maxError,Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1));
                    Require(w.boneIndex0<r.bones.Length && w.boneIndex1<r.bones.Length && w.boneIndex2<r.bones.Length && w.boneIndex3<r.bones.Length,"Invalid skin index");
                }
                Require(maxError<0.001f,"Unnormalized weights");
                // In the rest pose every skin matrix should be identity in renderer space.
                float bindError=0;
                for(int i=0;i<r.bones.Length;i++)
                {
                    var m=r.transform.worldToLocalMatrix*r.bones[i].localToWorldMatrix*r.sharedMesh.bindposes[i];
                    for(int j=0;j<16;j++) bindError=Mathf.Max(bindError,Mathf.Abs(m[j]-Matrix4x4.identity[j]));
                }
                Require(bindError<0.005f,"Rest skin matrix mismatch: "+r.name+" "+bindError);
                meshes.Add(new {path=AnimationUtility.CalculateTransformPath(r.transform,go.transform),vertices=r.sharedMesh.vertexCount,bones=r.bones.Length,maxWeightError=maxError,maxBindError=bindError});
            }
            var clips=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).Distinct().SelectMany(p=>AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().Select(c=>new{path=p,c.name,c.length,c.empty})).ToArray();
            Write("rig-before",new {prefabGuid=AssetDatabase.AssetPathToGUID(PrefabPath),clips,meshes,animators=go.GetComponentsInChildren<Animator>(true).Select(a=>new{path=AnimationUtility.CalculateTransformPath(a.transform,go.transform),avatar=a.avatar==null?null:a.avatar.name,controller=a.runtimeAnimatorController==null?null:a.runtimeAnimatorController.name}).ToArray(),bones=go.GetComponentsInChildren<Transform>(true).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t,go.transform),localPosition=t.localPosition.ToString("F6"),localRotation=t.localRotation.ToString("F6"),worldPosition=t.position.ToString("F6"),right=t.right.ToString("F3"),up=t.up.ToString("F3"),forward=t.forward.ToString("F3")}).ToArray(),importType=((ModelImporter)AssetImporter.GetAtPath("Assets/_Project/Art/Characters/Mario/Models/Mario_Super_Body.dae")).animationType.ToString()});
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        Debug.Log("Mario rig audit passed.");
    }
    static readonly string[] AnimatedBones = {"spin","face_1","arm_l1","arm_l2","wrist_l1","arm_r1","arm_r2","wrist_r1","leg_l1","leg_l2","ankle_l1","leg_r1","leg_r2","ankle_r1"};
    static Dictionary<string,Rest> Rig(GameObject go) => AnimatedBones.ToDictionary(n=>n,n=>new Rest(go.GetComponentsInChildren<Transform>(true).Single(t=>t.name==n),go.transform));

    static void Pose(Dictionary<string,Rest> rig, string clip, float phase)
    {
        foreach(var r in rig.Values) r.Reset();
        float wave=Mathf.Sin(phase*2*Mathf.PI), sway=Mathf.Sin(phase*4*Mathf.PI);
        float stride=0, knee=0, lower=72, arm=0, elbow=12, lean=0, breath=0;
        switch(clip)
        {
            case "Idle": breath=1.1f*wave; break;
            case "Walk": stride=23; knee=28; arm=20; elbow=20; lean=2; breath=1.2f*sway; break;
            case "Run": stride=38; knee=62; arm=42; elbow=64; lean=9; breath=2*sway; lower=65; break;
            case "Jump": lower=62; lean=-3; elbow=50; break;
            case "Fall": lower=40; elbow=25; lean=5; breath=1.5f*wave; break;
        }
        rig["spin"].bone.rotation=Quaternion.AngleAxis(lean+breath,Vector3.right)*rig["spin"].worldRotation;
        // Counter-rotate the neck slightly so the gaze stays level as the torso leans.
        rig["face_1"].bone.rotation=Quaternion.AngleAxis((lean+breath)*0.3f,Vector3.right)*rig["face_1"].worldRotation;
        foreach(var side in new[]{"l","r"})
        {
            float sidePhase=side=="l"?0:Mathf.PI;
            float s=Mathf.Sin(phase*2*Mathf.PI+sidePhase);
            float legSwing=stride*s;
            float kneeBend=knee*Mathf.Max(0,Mathf.Cos(phase*2*Mathf.PI+sidePhase));
            float armSwing=-arm*s;
            float elbowBend=elbow;
            if(clip=="Jump")
            {
                float ease=Mathf.SmoothStep(0,1,Mathf.Clamp01(phase*2));
                legSwing=side=="l"?Mathf.Lerp(8,24,ease):Mathf.Lerp(-8,-18,ease);
                kneeBend=side=="l"?Mathf.Lerp(14,42,ease):Mathf.Lerp(12,26,ease);
                armSwing=side=="r"?Mathf.Lerp(50,145,ease):Mathf.Lerp(20,45,ease);
                elbowBend=side=="r"?28:55;
            }
            if(clip=="Fall") {legSwing=side=="l"?10:-8;kneeBend=side=="l"?20:12;armSwing=15+3*wave;}
            var thigh=rig["leg_"+side+"1"];
            thigh.bone.rotation=Quaternion.AngleAxis(-legSwing,Vector3.right)*thigh.worldRotation;
            var shin=rig["leg_"+side+"2"];
            shin.bone.rotation=Quaternion.AngleAxis(kneeBend,Vector3.right)*shin.bone.rotation;
            var ankle=rig["ankle_"+side+"1"];
            float footPitch=clip=="Idle"?0:clip=="Jump"?15:clip=="Fall"?12:Mathf.Max(0,-s)*(clip=="Run"?18:10);
            ankle.bone.rotation=Quaternion.AngleAxis(footPitch,Vector3.right)*ankle.worldRotation;
            var upper=rig["arm_"+side+"1"];
            float sign=upper.bone.position.x>=0?-1:1;
            upper.bone.rotation=Quaternion.AngleAxis(-armSwing,Vector3.right)*Quaternion.AngleAxis(sign*lower,Vector3.forward)*upper.worldRotation;
            var fore=rig["arm_"+side+"2"];
            fore.bone.rotation=Quaternion.AngleAxis(-elbowBend,Vector3.right)*fore.bone.rotation;
        }
    }

    static AnimationClip BuildClip(GameObject go,string name)
    {
        float duration=name=="Idle"?2:name=="Walk"?1:name=="Run"?0.64f:name=="Jump"?0.55f:1.2f;
        int frames=Mathf.CeilToInt(duration*30);
        var clip=new AnimationClip {name=name,frameRate=30,legacy=false};
        var rig=Rig(go);
        var curves=rig.Keys.ToDictionary(k=>k,k=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()});
        var last=new Dictionary<string,Quaternion>();
        for(int frame=0;frame<=frames;frame++)
        {
            float phase=(float)frame/frames;
            Pose(rig,name,phase);
            foreach(var pair in rig)
            {
                var q=pair.Value.bone.localRotation;
                if(last.TryGetValue(pair.Key,out var prev) && Quaternion.Dot(q,prev)<0) q=new Quaternion(-q.x,-q.y,-q.z,-q.w);
                last[pair.Key]=q;
                for(int axis=0;axis<4;axis++) curves[pair.Key][axis].AddKey(phase*duration,q[axis]);
            }
        }
        foreach(var pair in rig)
        {
            for(int axis=0;axis<4;axis++)
            {
                var c=curves[pair.Key][axis];
                for(int key=0;key<c.length;key++) {AnimationUtility.SetKeyLeftTangentMode(c,key,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(c,key,AnimationUtility.TangentMode.Linear);}
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(pair.Value.path,typeof(Transform),"m_LocalRotation."+"xyzw"[axis]),c);
            }
            pair.Value.Reset();
        }
        clip.EnsureQuaternionContinuity();
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=name!="Jump";
        settings.loopBlend=false;
        settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;settings.keepOriginalOrientation=true;
        // No root translation or rotation curves are authored.
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        AssetDatabase.CreateAsset(clip,Folder+"/"+name+".anim");
        return clip;
    }

    static AnimatorStateTransition Transition(AnimatorState from,AnimatorState to,float duration=0.12f)
    {
        var t=from.AddTransition(to); t.hasExitTime=false;t.hasFixedDuration=true;t.duration=duration;t.canTransitionToSelf=false;
        return t;
    }
    static void Condition(AnimatorStateTransition t,string parameter,AnimatorConditionMode mode,float threshold=0) => t.AddCondition(mode,threshold,parameter);

    [MenuItem("Tools/Mario Animations/Create Initial Animations")]
    public static void CreateAnimations()
    {
        foreach(var n in ClipNames) Require(!File.Exists(Folder+"/"+n+".anim"),"Will not overwrite existing clip: "+n);
        Require(!File.Exists(Folder+"/Mario.controller"),"Mario.controller already exists");
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder).Replace('\\','/'),"Animations");
        var prefabGuid=AssetDatabase.AssetPathToGUID(PrefabPath);
        var contents=PrefabUtility.LoadPrefabContents(PrefabPath);
        var rest=contents.GetComponentsInChildren<Transform>(true).Select(t=>new Rest(t,contents.transform)).ToArray();
        try
        {
            Require(contents.GetComponentsInChildren<Animator>(true).Length==0,"Existing Animator requires manual review");
            var clips=ClipNames.ToDictionary(n=>n,n=>BuildClip(contents,n));
            foreach(var r in rest) r.Reset();
            var controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/Mario.controller");
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter {name="Grounded",type=AnimatorControllerParameterType.Bool,defaultBool=true});
            controller.AddParameter("VerticalSpeed",AnimatorControllerParameterType.Float);
            var sm=controller.layers[0].stateMachine;
            var states=new Dictionary<string,AnimatorState>();
            int idx=0;
            foreach(var n in ClipNames) {var s=sm.AddState(n,new Vector3(260+(idx%3)*240,80+(idx/3)*160));s.motion=clips[n];s.writeDefaultValues=false;states[n]=s;idx++;}
            sm.defaultState=states["Idle"];
            foreach(var a in new[]{"Idle","Walk","Run"}) foreach(var b in new[]{"Idle","Walk","Run"})
            {
                if(a==b) continue;
                var t=Transition(states[a],states[b]);Condition(t,"Grounded",AnimatorConditionMode.If);
                if(b=="Idle") Condition(t,"Speed",AnimatorConditionMode.Less,0.1f);
                if(b=="Walk") {Condition(t,"Speed",AnimatorConditionMode.Greater,0.1f);Condition(t,"Speed",AnimatorConditionMode.Less,0.65f);}
                if(b=="Run") Condition(t,"Speed",AnimatorConditionMode.Greater,0.65f);
            }
            foreach(var n in new[]{"Jump","Fall"})
            {
                var t=sm.AddAnyStateTransition(states[n]);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=0.09f;t.canTransitionToSelf=false;
                Condition(t,"Grounded",AnimatorConditionMode.IfNot);
                Condition(t,"VerticalSpeed",n=="Jump"?AnimatorConditionMode.Greater:AnimatorConditionMode.Less,0.05f);
                var land=Transition(states[n],states["Idle"],0.1f);Condition(land,"Grounded",AnimatorConditionMode.If);
            }
            var animator=contents.AddComponent<Animator>();
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var r in rest) Require(Vector3.Distance(r.position,r.bone.localPosition)<1e-6f && Quaternion.Angle(r.rotation,r.bone.localRotation)<0.01f && r.scale==r.bone.localScale,"Rest transform changed: "+r.path);
            PrefabUtility.SaveAsPrefabAsset(contents,PrefabPath);
            AssetDatabase.SaveAssets();
            Require(prefabGuid==AssetDatabase.AssetPathToGUID(PrefabPath),"Prefab GUID changed");
        }
        finally {PrefabUtility.UnloadPrefabContents(contents);}
        Debug.Log("Created five in-place Generic animations and assigned Mario.controller to the existing prefab.");
    }
}
