using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class DemoAssetAudit
{
 [MenuItem("Tools/Mario Demo/Install Prepared Scripts")]
 public static void Install(){var files=Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string,string>>(File.ReadAllText("Temp/MarioDemoScripts.json"));foreach(var pair in files){if(!pair.Key.StartsWith("Assets/_Project/")||!pair.Key.EndsWith(".cs"))throw new System.Exception("Invalid script target");Directory.CreateDirectory(Path.GetDirectoryName(pair.Key));File.WriteAllText(pair.Key,pair.Value,new System.Text.UTF8Encoding(false));}AssetDatabase.Refresh();}
 [MenuItem("Tools/Mario Demo/Audit Assets")]
 public static void Audit(){Directory.CreateDirectory("Temp/MarioDemoAudit");var rows=new System.Collections.Generic.List<object>();
 foreach(var path in new[]{"Assets/_Project/Prefabs/Enemies/HelloMario/Goomba/Goomba.prefab","Assets/_Project/Prefabs/Items/HelloMario/Coin/Coin.prefab","Assets/_Project/Prefabs/Items/HelloMario/Block/Brick.prefab"}){var p=AssetDatabase.LoadAssetAtPath<GameObject>(path);rows.Add(new{path,transforms=p.GetComponentsInChildren<Transform>(true).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t,p.transform),position=t.localPosition.ToString(),scale=t.localScale.ToString(),rotation=t.localEulerAngles.ToString()}).ToArray(),renderers=p.GetComponentsInChildren<Renderer>().Select(r=>new{r.name,bounds=r.bounds.ToString(),materials=r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m)).ToArray()}).ToArray()});}
 foreach(var name in new[]{"Walk","Run","PressDown"}){var p="Assets/_Project/Art/Characters/Enemies/HelloMario/Goomba/Animations/"+name+".fbx";var c=AssetDatabase.LoadAllAssetsAtPath(p).OfType<AnimationClip>().First(x=>!x.name.StartsWith("__preview"));rows.Add(new{name,c.length,paths=AnimationUtility.GetCurveBindings(c).Select(b=>b.path).Distinct().ToArray()});}
 File.WriteAllText("Temp/MarioDemoAudit/assets.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));}
}
