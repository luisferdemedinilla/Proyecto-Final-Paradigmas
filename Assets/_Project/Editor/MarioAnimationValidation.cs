using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor.SceneManagement;

public static class MarioAnimationValidation
{
    public const string Output = "Temp/MarioAnimationAudit";
    static void Check(bool ok,string message) {if(!ok) throw new Exception(message);}

    public static void ConfigureCamera(PreviewRenderUtility p, float angle=25)
    {
        var target=new Vector3(0,0.88f,0);
        p.camera.transform.position=target+Quaternion.Euler(0,angle,0)*new Vector3(0,0.35f,4.2f);
        p.camera.transform.LookAt(target);
        p.camera.nearClipPlane=0.01f;p.camera.farClipPlane=50;p.camera.fieldOfView=30;
        p.camera.clearFlags=CameraClearFlags.SolidColor;p.camera.backgroundColor=new Color(0.19f,0.21f,0.25f);
        p.lights[0].intensity=1.4f;p.lights[0].transform.rotation=Quaternion.Euler(35,210,0);
        p.lights[1].intensity=0.9f;p.ambientColor=new Color(0.45f,0.45f,0.45f);
    }

    public static Texture2D Render(PreviewRenderUtility preview,int size)
    {
        preview.BeginStaticPreview(new Rect(0,0,size,size));preview.Render(true);return preview.EndStaticPreview();
    }

    [MenuItem("Tools/Mario Animations/Validate And Render Clips")]
    public static void ValidateClips()
    {
        Directory.CreateDirectory(Output);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MarioAnimationTools.PrefabPath);
        var preview=new PreviewRenderUtility();
        var reports=new List<object>();
        try
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview.camera.scene);preview.AddSingleGO(go);
            var animator=go.GetComponent<Animator>();Check(animator!=null && !animator.applyRootMotion,"Root motion must be disabled");animator.runtimeAnimatorController=null;
            foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen=true;
            var rest=go.GetComponentsInChildren<Transform>(true).Select(t=>new MarioAnimationTools.Rest(t,go.transform)).ToArray();
            var renderers=go.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();
            ConfigureCamera(preview);
            foreach(var name in MarioAnimationTools.ClipNames)
            {
                foreach(var r in rest) r.Reset();
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioAnimationTools.Folder+"/"+name+".anim");
                var bindings=AnimationUtility.GetCurveBindings(clip);
                Check(bindings.Length==56,"Expected 14 quaternion bone curves: "+name);
                foreach(var binding in bindings) Check(binding.type==typeof(Transform) && binding.propertyName.StartsWith("m_LocalRotation.") && !string.IsNullOrEmpty(binding.path) && go.transform.Find(binding.path)!=null,"Invalid binding: "+binding.path);
                float maxPositionError=0,maxScaleError=0,maxHeight=0,minY=100,maxMotion=0,maxVertexMotion=0;
                var firstVertices=new Dictionary<SkinnedMeshRenderer,Vector3[]>();
                var first=new Dictionary<Transform,Quaternion>();
                for(int frame=0;frame<=60;frame++)
                {
                    float time=clip.length*frame/60;
                    clip.SampleAnimation(go,time);
                    foreach(var r in rest)
                    {
                        maxPositionError=Mathf.Max(maxPositionError,Vector3.Distance(r.bone.localPosition,r.position));
                        maxScaleError=Mathf.Max(maxScaleError,Vector3.Distance(r.bone.localScale,r.scale));
                        if(frame==0) first[r.bone]=r.bone.localRotation;
                        maxMotion=Mathf.Max(maxMotion,Quaternion.Angle(first[r.bone],r.bone.localRotation));
                    }
                    var bounds=new Bounds();bool set=false;
                    foreach(var r in renderers)
                    {
                        var baked=new Mesh();r.BakeMesh(baked,false);
                        var vertices=baked.vertices;
                        if(frame==0) firstVertices[r]=vertices;
                        for(int vi=0;vi<vertices.Length;vi++)
                        {
                            var v=vertices[vi];
                            maxVertexMotion=Mathf.Max(maxVertexMotion,Vector3.Distance(r.transform.TransformPoint(v),r.transform.TransformPoint(firstVertices[r][vi])));
                            Check(!float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.sqrMagnitude),"Non-finite skinned vertex");
                            var world=r.transform.TransformPoint(v);
                            if(!set) {bounds=new Bounds(world,Vector3.zero);set=true;} else bounds.Encapsulate(world);
                        }
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                    maxHeight=Mathf.Max(maxHeight,bounds.size.y);minY=Mathf.Min(minY,bounds.min.y);
                    Check(bounds.size.x<3 && bounds.size.y<3 && bounds.size.z<3,"Exploded skin bounds: "+name);
                }
                Check(maxPositionError<0.00001f && maxScaleError<0.00001f,"Animation changes bone translation/scale: "+name);
                Check(maxMotion>1,"Clip is static: "+name);
                Check(maxVertexMotion>0.0001f,"Skinned mesh is not changing: "+name);
                float seam=0;
                foreach(var r in rest) seam=Mathf.Max(seam,Quaternion.Angle(first[r.bone],r.bone.localRotation));
                if(name!="Jump") Check(seam<0.1f,"Loop seam: "+name);
                reports.Add(new{name,clip.length,curveCount=bindings.Length,samples=61,maxPositionError,maxScaleError,maxAngularMotion=maxMotion,maxVertexMotion,loopSeamDegrees=seam,maxHeight,minY});
                for(int f=0;f<4;f++)
                {
                    clip.SampleAnimation(go,clip.length*f/4);
                    // Use a fresh instance for each still. A disabled Animator can leave
                    // cached skin matrices when many poses are rendered in one editor tick.
                    var freshPreview=new PreviewRenderUtility();
                    var fresh=(GameObject)PrefabUtility.InstantiatePrefab(prefab,freshPreview.camera.scene);
                    freshPreview.AddSingleGO(fresh);
                    fresh.GetComponent<Animator>().runtimeAnimatorController=null;
                    foreach(var skin in fresh.GetComponentsInChildren<SkinnedMeshRenderer>(true)) skin.updateWhenOffscreen=true;
                    clip.SampleAnimation(fresh,clip.length*f/4);
                    ConfigureCamera(freshPreview);
                    var image=Render(freshPreview,384);
                    File.WriteAllBytes(Output+"/"+name+"-"+f+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                    freshPreview.Cleanup();
                }
            }
        }
        finally {preview.Cleanup();}
        File.WriteAllText(Output+"/clips-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
        Debug.Log("Mario: five clips sampled, skin baked, bindings and loop seams validated.");
    }

    [MenuItem("Tools/Mario Animations/Validate Controller")]
    public static void ValidateController()
    {
        Directory.CreateDirectory(Output);
        var scene=EditorSceneManager.NewPreviewScene();
        var graph=PlayableGraph.Create("Mario controller validation");
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MarioAnimationTools.PrefabPath),scene);
            var animator=go.GetComponent<Animator>();
            var controller=animator.runtimeAnimatorController;
            animator.runtimeAnimatorController=null;animator.applyRootMotion=false;
            var playable=AnimatorControllerPlayable.Create(graph,controller);
            var output=AnimationPlayableOutput.Create(graph,"Mario",animator);output.SetSourcePlayable(playable);graph.Play();
            var results=new List<object>();
            var bone=go.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="leg_l1");
            Action<string,float,bool,float> test=(expected,speed,grounded,vertical)=>
            {
                playable.SetFloat("Speed",speed);playable.SetBool("Grounded",grounded);playable.SetFloat("VerticalSpeed",vertical);
                float motion=0;var previous=bone.localRotation;
                for(int i=0;i<45;i++){graph.Evaluate(1f/60);motion=Mathf.Max(motion,Quaternion.Angle(previous,bone.localRotation));}
                var info=playable.GetCurrentAnimatorStateInfo(0);
                Check(info.IsName(expected),"Expected "+expected+", got hash "+info.shortNameHash);
                Check(go.transform.localPosition==Vector3.zero && Quaternion.Angle(go.transform.localRotation,Quaternion.identity)<0.01f,"Animator displaced the root");
                if(expected=="Walk" || expected=="Run") Check(motion>5,"Animator did not drive leg bones");
                results.Add(new{expected,speed,grounded,vertical,normalizedTime=info.normalizedTime,legMotionDegrees=motion,rootMotion=animator.applyRootMotion});
            };
            test("Idle",0,true,0);test("Walk",0.35f,true,0);test("Run",1,true,0);
            test("Walk",0.3f,true,0);test("Idle",0,true,0);
            test("Jump",0,false,3);test("Fall",0,false,-2);test("Idle",0,true,0);
            test("Fall",0.4f,false,0);test("Walk",0.4f,true,0);
            test("Jump",1,false,2);test("Fall",1,false,-1);test("Run",1,true,0);
            File.WriteAllText(Output+"/controller-validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
        }
        finally {graph.Destroy();EditorSceneManager.ClosePreviewScene(scene);}
        Debug.Log("Mario Animator passed 13 parameter/transition checks with manual PlayableGraph evaluation in the Editor.");
    }
}
