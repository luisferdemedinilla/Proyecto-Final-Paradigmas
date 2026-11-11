using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class HelloMarioValidation
{
    [MenuItem("Tools/Hello Mario/Validate Library And Animations")]
    public static void Validate()
    {
        Directory.CreateDirectory("Temp/HelloMarioAudit");
        var entries=Newtonsoft.Json.JsonConvert.DeserializeObject<HelloMarioLibrary.Entry[]>(File.ReadAllText("Documentation/HelloMario-migration.json"));
        var issues=new List<string>();int prefabs=0,renderers=0;
        foreach(var e in entries)
        {
            if(AssetDatabase.GUIDToAssetPath(e.guid)!=e.destination) issues.Add("GUID mismatch: "+e.destination);
            if(!e.destination.EndsWith(".prefab"))continue;
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(e.destination);if(go==null){issues.Add("Cannot load prefab: "+e.destination);continue;}prefabs++;
            foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            {
                renderers++;if(r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name=="Hidden/InternalErrorShader")) issues.Add("Missing material/shader: "+e.destination+"/"+r.name);
                if(r is SkinnedMeshRenderer skin && (skin.sharedMesh==null||skin.bones.Any(b=>b==null)))issues.Add("Broken skin: "+e.destination+"/"+r.name);
            }
            foreach(var t in go.GetComponentsInChildren<Transform>(true)) if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)issues.Add("Missing script: "+e.destination);
        }
        var scene=EditorSceneManager.NewPreviewScene();var clipsReport=new List<object>();
        try
        {
            foreach(var name in new[]{"Mario","Luigi","Toad"})
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HelloMarioPlayableBuilder.Playables+name+"Player.prefab"),scene);
                go.GetComponent<Animator>().runtimeAnimatorController=null;
                var transforms=go.GetComponentsInChildren<Transform>(true).ToDictionary(t=>AnimationUtility.CalculateTransformPath(t,go.transform));
                var limbs=transforms.Where(p=>p.Key.Contains("/Leg")||p.Key.Contains("/Arm")).ToDictionary(p=>p.Key,p=>p.Value.localPosition.magnitude);
                foreach(var state in new[]{"Idle","Walk","Run","Jump","Fall","Crouch","AirSpin"})
                {
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(HelloMarioPlayableBuilder.AnimFolder(name)+state+".anim");
                    foreach(var b in AnimationUtility.GetCurveBindings(clip))if(!transforms.ContainsKey(b.path))issues.Add("Missing binding: "+name+" "+state+" "+b.path);
                    float minimum=999,maximum=-999,motion=0,spinTravel=0;Quaternion last=Quaternion.identity;
                    var leg=transforms.Values.First(t=>t.name=="LegL1");var first=leg.localRotation;
                    for(int i=0;i<=24;i++)
                    {
                        clip.SampleAnimation(go,clip.length*i/24f);
                        var bounds=HelloMarioPlayableBuilder.BoundsOf(go);minimum=Mathf.Min(minimum,bounds.min.y);maximum=Mathf.Max(maximum,bounds.max.y);motion=Mathf.Max(motion,Quaternion.Angle(first,leg.localRotation));
                        var rotation=transforms.Values.Single(t=>t.name=="AllRoot").localRotation;if(i>0)spinTravel+=Quaternion.Angle(last,rotation);last=rotation;
                        foreach(var l in limbs)if(Mathf.Abs(transforms[l.Key].localPosition.magnitude-l.Value)>.001f)issues.Add("Changed limb length: "+name+" "+state+" "+l.Key);
                        if(go.transform.position.sqrMagnitude>.00001f)issues.Add("Root motion: "+name+" "+state);
                    }
                    if((state=="Idle"||state=="Walk"||state=="Run"||state=="Crouch")&&minimum<-.02f)issues.Add("Ground penetration: "+name+" "+state+" "+minimum);
                    if(state=="AirSpin" && spinTravel<330)issues.Add("Incomplete spin: "+name+" "+spinTravel);
                    if(maximum>3||minimum<-.5f)issues.Add("Unexpected skin bounds: "+name+" "+state);
                    clipsReport.Add(new{name,state,minimum,maximum,motion,spinTravel});
                }
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);}
        var output=new{passed=issues.Count==0,resources=entries.Length,prefabs,renderers,issues,clips=clipsReport};File.WriteAllText("Temp/HelloMarioAudit/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(output,Newtonsoft.Json.Formatting.Indented));
        if(issues.Count>0)Debug.LogError("Hello Mario validation: "+issues.Count+" issues; see report.");else Debug.Log("Hello Mario: library references and 21 animation/skin checks passed.");
    }
}
