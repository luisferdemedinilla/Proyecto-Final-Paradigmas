using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MarioAirSpinSetup
{
    const string Folder="Assets/_Project/Art/Characters/Mario/Animations/";
    [MenuItem("Tools/Mario Test/Validate Air Spin Transitions")]
    public static void FixTransitions()
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(MarioGameplaySetup.ControllerPath);
        foreach(var transition in controller.layers[0].stateMachine.anyStateTransitions)
            if(transition.destinationState.name!="AirSpin" && !transition.conditions.Any(c=>c.parameter=="AirSpinning"))
                transition.AddCondition(AnimatorConditionMode.IfNot,0,"AirSpinning");
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        Debug.Log("AirSpin cannot be interrupted by airborne AnyState transitions.");
    }
    [MenuItem("Tools/Mario Test/Add Air Spin")]
    public static void Create()
    {
        if(EditorApplication.isPlaying) throw new Exception("Exit Play Mode first");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(MarioGameplaySetup.ControllerPath);
        if(AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"AirSpin.anim")!=null || controller.parameters.Any(p=>p.name=="AirSpinning")) throw new Exception("AirSpin already exists; inspect before changing it");
        var scene=EditorSceneManager.NewPreviewScene();
        AnimationClip clip;
        try
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MarioAnimationTools.PrefabPath),scene);
            var jump=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"Jump.anim");
            jump.SampleAnimation(go,.15f);
            clip=new AnimationClip{name="AirSpin",frameRate=60};
            foreach(var binding in AnimationUtility.GetCurveBindings(jump))
            {
                float value=AnimationUtility.GetEditorCurve(jump,binding).Evaluate(.15f);
                AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,.45f,value));
            }
            var root=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="skl_root");
            var rest=root.localRotation;var axis=root.parent.InverseTransformDirection(Vector3.up).normalized;
            var curves=Enumerable.Range(0,4).Select(i=>new AnimationCurve()).ToArray();
            for(int i=0;i<=60;i++)
            {
                var q=Quaternion.AngleAxis(i*6f,axis)*rest;
                for(int c=0;c<4;c++) curves[c].AddKey(i/60f*.45f,q[c]);
            }
            string path=AnimationUtility.CalculateTransformPath(root,go.transform);
            for(int c=0;c<4;c++) AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[c]),curves[c]);
            clip.EnsureQuaternionContinuity();
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
            AssetDatabase.CreateAsset(clip,Folder+"AirSpin.anim");
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);}
        controller.AddParameter("AirSpinning",AnimatorControllerParameterType.Bool);
        controller.AddParameter("AirSpinSpeed",AnimatorControllerParameterType.Float);
        var machine=controller.layers[0].stateMachine;var states=machine.states.ToDictionary(s=>s.state.name,s=>s.state);
        var spin=machine.AddState("AirSpin",new Vector3(500,260));spin.motion=clip;spin.writeDefaultValues=false;spin.speedParameter="AirSpinSpeed";spin.speedParameterActive=true;
        foreach(var transition in machine.anyStateTransitions)
            transition.AddCondition(AnimatorConditionMode.IfNot,0,"AirSpinning");
        var entry=machine.AddAnyStateTransition(spin);entry.hasExitTime=false;entry.duration=0;entry.canTransitionToSelf=false;
        entry.AddCondition(AnimatorConditionMode.If,0,"AirSpinning");entry.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");
        foreach(var name in new[]{"Idle","Jump","Fall"})
        {
            var exit=spin.AddTransition(states[name]);exit.hasExitTime=false;exit.duration=0;
            exit.AddCondition(AnimatorConditionMode.IfNot,0,"AirSpinning");
            exit.AddCondition(name=="Idle"?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Grounded");
            if(name!="Idle") exit.AddCondition(name=="Jump"?AnimatorConditionMode.Greater:AnimatorConditionMode.Less, name=="Jump"?0:.001f,"VerticalSpeed");
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        var test=SceneManager.GetSceneByPath(MarioTestSceneSetup.ScenePath);
        if(test.IsValid() && test.isLoaded) {EditorSceneManager.MarkSceneDirty(test);EditorSceneManager.SaveScene(test);}
        Debug.Log("AirSpin animation and transitions created.");
    }
}
