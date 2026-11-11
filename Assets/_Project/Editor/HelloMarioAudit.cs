using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class HelloMarioAudit
{
    [MenuItem("Tools/Hello Mario/Audit Source")]
    public static void Audit()
    {
        Directory.CreateDirectory("Temp/HelloMarioAudit");
        var rows=new List<object>();
        foreach(var name in new[]{"Mario","Luigi","Toad"})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/HelloMario/"+name+"/"+name+".prefab");
            rows.Add(new{name,transforms=prefab.GetComponentsInChildren<Transform>(true).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t,prefab.transform),pos=t.localPosition.ToString("F4"),scale=t.localScale.ToString("F4"),rot=t.localEulerAngles.ToString("F2"),active=t.gameObject.activeSelf}).ToArray(),renderers=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r=>new{r.name,active=r.gameObject.activeSelf,vertices=r.sharedMesh.vertexCount,bones=r.bones.Length,bounds=r.bounds.ToString(),root=r.rootBone==null?null:r.rootBone.name}).ToArray(),components=prefab.GetComponents<Component>().Select(c=>c.GetType().Name).ToArray()});
        }
        var clips=new List<object>();
        foreach(var name in new[]{"Wait","Walk","Run","Dash","Jump","LuigiJump","Fall","FallLoop","SquatWait","SpinJumpL","SpinJumpR","TailAttackAir","PropellerJump"})
        {
            var path="Assets/_Project/Art/Characters/Shared/HelloMario/Animations/Source/"+name+".fbx";
            foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")))
            {var bindings=AnimationUtility.GetCurveBindings(clip);clips.Add(new{name,clip.length,clip.legacy,bindings=bindings.Select(b=>new{b.path,b.propertyName}).ToArray()});}
        }
        File.WriteAllText("Temp/HelloMarioAudit/source.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{players=rows,clips},Newtonsoft.Json.Formatting.Indented));
        Debug.Log("Hello Mario source audit saved.");
    }
}
