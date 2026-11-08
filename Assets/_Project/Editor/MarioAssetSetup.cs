using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class MarioAssetSetup {
[MenuItem("Tools/Mario Assets/Import Sources")]
public static void ImportSources() {
var root = "Assets/_Project/Art/Characters/Mario";
var names = new Dictionary<string,string> {
 {"MB_model", "Mario_Super_Body"}, {"MH_model", "Mario_Super_Head"}, {"MC_model", "Mario_Super_Cap"},
 {"SMB_model", "Mario_Small_Body"}, {"SMH_model", "Mario_Small_Head"}, {"SMC_model", "Mario_Small_Cap"},
 {"MMB_model", "MMB_model"}, {"MMH_model", "MMH_model"},
 {"PMB_model", "Mario_Penguin_Body"}, {"PMH_model", "Mario_Penguin_Head"},
 {"PLMB_model", "Mario_Propeller_Body"}, {"PLMH_model", "Mario_Propeller_Head"}
};
var source = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Mario");
foreach (var folder in new [] {root+"/Models",root+"/Textures",root+"/Materials","Assets/_Project/Prefabs/Characters"}) {
 var parts=folder.Split('/'); var parent=parts[0];
 foreach(var part in parts.Skip(1)) {if(!AssetDatabase.IsValidFolder(parent+"/"+part)) AssetDatabase.CreateFolder(parent,part); parent+="/"+part;}
}
foreach(var file in Directory.GetFiles(source,"*.png")) {
 var dst=root+"/Textures/"+Path.GetFileName(file);
 if(File.Exists(dst)) throw new Exception("Refusing overwrite: "+dst);
 File.Copy(file,dst,false);
}
foreach(var entry in names) {
 var dst=root+"/Models/"+entry.Value+".dae";
 if(File.Exists(dst)) throw new Exception("Refusing overwrite: "+dst);
 var xml=File.ReadAllText(Path.Combine(source,entry.Key+".dae"));
 foreach(var image in Directory.GetFiles(source,"*.png")) {
  var name=Path.GetFileName(image);
  xml=xml.Replace("<init_from>"+name+"</init_from>","<init_from>../Textures/"+name+"</init_from>");
 }
 using(var stream=new FileStream(dst,FileMode.CreateNew)) using(var writer=new StreamWriter(stream)) writer.Write(xml);
}
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

Debug.Log("Mario sources imported.");
}

[MenuItem("Tools/Mario Assets/Inspect")]
public static void Inspect() {
 var root="Assets/_Project/Art/Characters/Mario";
 var data=new List<object>();
 foreach(var path in Directory.GetFiles(root+"/Models","*.dae")) {
  var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
  data.Add(new {path, loaded=go!=null, nodes=go==null?null:go.GetComponentsInChildren<Transform>(true).Select(t=>new {t.name,parent=t.parent==null?null:t.parent.name,position=t.localPosition.ToString("F4"),rotation=t.localEulerAngles.ToString("F2"),scale=t.localScale.ToString("F4")}).ToArray(),
   renderers=go==null?null:go.GetComponentsInChildren<Renderer>(true).Select(r=>new {r.name,type=r.GetType().Name,bounds=r.bounds.ToString("F3"),materials=r.sharedMaterials.Select(m=>m==null?null:m.name).ToArray()}).ToArray(),
   meshes=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Select(m=>new {m.name,m.vertexCount,triangles=m.triangles.Length/3,uvs=m.uv.Length,bones=m.bindposes.Length}).ToArray()});
 }
 Directory.CreateDirectory("Temp/MarioAudit");
 File.WriteAllText("Temp/MarioAudit/import.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
}

const string Art="Assets/_Project/Art/Characters/Mario";
[MenuItem("Tools/Mario Assets/Create Materials")]
public static void CreateMaterials() {
 var shader=Shader.Find("Universal Render Pipeline/Lit");
 if(shader==null) throw new Exception("URP Lit shader missing");
 var mats=new Dictionary<string,Material>();
 foreach(var file in Directory.GetFiles(Art+"/Textures","*.png")) {
  var key=Path.GetFileNameWithoutExtension(file);
  if(!(key.StartsWith("player_") && !key.EndsWith("_spc")) && key!="P") continue;
  var path=Art+"/Materials/Mario_"+key+".mat";
  if(File.Exists(path)) throw new Exception("Refusing overwrite: "+path);
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(file.Replace('\\','/'));
  if(tex==null) throw new Exception("Missing texture: "+file);
  var mat=new Material(shader) {name="Mario_"+key};
  mat.SetTexture("_BaseMap",tex); mat.SetColor("_BaseColor",Color.white);
  if(key.StartsWith("player_eye.")) mat.SetTextureScale("_BaseMap",new Vector2(2f,1f));
  mat.SetFloat("_Metallic",0); mat.SetFloat("_Smoothness",0.15f);
  if(key=="P") {mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",0.5f);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;}
  AssetDatabase.CreateAsset(mat,path); mats.Add(key,mat);
 }
 foreach(var file in Directory.GetFiles(Art+"/Models","*.dae")) {
  var path=file.Replace('\\','/');
  var doc=System.Xml.Linq.XDocument.Load(file);
  System.Xml.Linq.XNamespace ns="http://www.collada.org/2005/11/COLLADASchema";
  var importer=(ModelImporter)AssetImporter.GetAtPath(path);
  foreach(var element in doc.Descendants(ns+"material")) {
   var name=(string)element.Attribute("id");
   var effectId=((string)element.Element(ns+"instance_effect").Attribute("url")).TrimStart('#');
   var effect=doc.Descendants(ns+"effect").Single(e=>(string)e.Attribute("id")==effectId);
   var image=effect.Descendants(ns+"surface").First().Element(ns+"init_from").Value;
   importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),name),mats[image]);
  }
  importer.addCollider=false;
  importer.SaveAndReimport();
 }
 AssetDatabase.SaveAssets();
 Debug.Log("Mario materials created and explicitly remapped.");
}
[MenuItem("Tools/Mario Assets/Create Prefab")]
public static void CreatePrefab() {
 var path="Assets/_Project/Prefabs/Characters/Mario.prefab";
 if(File.Exists(path)) throw new Exception("Refusing overwrite: "+path);
 var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
 try {
  var root=new GameObject("Mario"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
  var visual=new GameObject("Visual"); visual.transform.SetParent(root.transform,false); visual.transform.localScale=Vector3.one*0.05f;
  var body=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/Mario_Super_Body.dae"),scene);
  body.transform.SetParent(visual.transform,false); body.name="Body";
  var neck=body.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="face_1");
  var head=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/Mario_Super_Head.dae"),scene);
  head.transform.SetParent(neck,false); head.name="Head";
  PrefabUtility.SaveAsPrefabAsset(root,path);
 } finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
 Debug.Log("Mario prefab created.");
}
[MenuItem("Tools/Mario Assets/Validate And Preview")]
public static void ValidateAndPreview() {
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/Mario.prefab");
 if(prefab==null) throw new Exception("Missing Mario prefab");
 var renderers=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
 if(renderers.Length!=5) throw new Exception("Expected body plus four head meshes");
 foreach(var r in renderers) {
  if(r.sharedMesh==null || r.bones.Any(b=>b==null)) throw new Exception("Missing mesh/bone: "+r.name);
  if(r.sharedMaterials.Any(m=>m==null || m.GetTexture("_BaseMap")==null || m.shader.name!="Universal Render Pipeline/Lit")) throw new Exception("Invalid material: "+r.name);
 }
 if(prefab.GetComponentsInChildren<Collider>(true).Length>0 || prefab.GetComponentsInChildren<Rigidbody>(true).Length>0 || prefab.GetComponentsInChildren<MonoBehaviour>(true).Length>0) throw new Exception("Unexpected gameplay components");
 var preview=new PreviewRenderUtility();
 try {
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview.camera.scene);
  preview.AddSingleGO(instance);
  var bounds=instance.GetComponentsInChildren<Renderer>().First().bounds;
  foreach(var r in instance.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
  preview.camera.transform.position=bounds.center+new Vector3(0f,0.3f,4.5f);
  preview.camera.transform.LookAt(bounds.center);
  preview.camera.nearClipPlane=0.01f;preview.camera.farClipPlane=100;preview.camera.fieldOfView=30;
  preview.camera.clearFlags=CameraClearFlags.SolidColor; preview.camera.backgroundColor=new Color(0.19f,0.21f,0.25f);
  preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=Quaternion.Euler(35,210,0);
  preview.lights[1].intensity=0.9f; preview.ambientColor=new Color(0.45f,0.45f,0.45f);
  preview.BeginStaticPreview(new Rect(0,0,800,800)); preview.Render(true);
  var image=preview.EndStaticPreview();
  Directory.CreateDirectory("Temp/MarioAudit");File.WriteAllBytes("Temp/MarioAudit/Mario-preview-static.png",image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
  File.WriteAllText("Temp/MarioAudit/validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {renderers=renderers.Length,triangles=renderers.Sum(r=>r.sharedMesh.triangles.Length/3),bounds=bounds.ToString("F3"),materials=renderers.SelectMany(r=>r.sharedMaterials).Select(m=>AssetDatabase.GetAssetPath(m)).Distinct().ToArray(),activeScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path},Newtonsoft.Json.Formatting.Indented));
 } finally {preview.Cleanup();}
 Debug.Log("Mario prefab validated and preview rendered.");
}

[MenuItem("Tools/Mario Assets/Finish Appearance")]
public static void FinishAppearance() {
 foreach(var file in Directory.GetFiles(Art+"/Textures","*.png")) {
  var key=Path.GetFileNameWithoutExtension(file);
  if(!key.StartsWith("player_") && key!="P") continue;
  var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
  importer.wrapModeU=key=="player_phead"?TextureWrapMode.Clamp:TextureWrapMode.Mirror;
  importer.wrapModeV=key=="player_phead" || key=="player_plhead"?TextureWrapMode.Clamp:key.StartsWith("player_mhead.")?TextureWrapMode.Repeat:TextureWrapMode.Mirror;
  importer.SaveAndReimport();
 }
 var path="Assets/_Project/Prefabs/Characters/Mario.prefab";
 var contents=PrefabUtility.LoadPrefabContents(path);
 try {
  var head=contents.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Head");
  head.Find("polygon0").gameObject.SetActive(false);
  PrefabUtility.SaveAsPrefabAsset(contents,path);
 } finally {PrefabUtility.UnloadPrefabContents(contents);}
}

// The eye PNG stores half of the face. The mesh UVs span both halves;
// tiling twice with Mirror wrapping reconstructs two symmetric eyes/brows.
[MenuItem("Tools/Mario Assets/Fix Eye Mapping")]
public static void FixEyeMapping() {
 for(int i=0;i<7;i++) {
  var path=Art+"/Materials/Mario_player_eye."+i+".mat";
  var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(mat==null) throw new Exception("Missing eye material: "+path);
  mat.SetTextureScale("_BaseMap",new Vector2(2f,1f));
  mat.SetTextureOffset("_BaseMap",Vector2.zero);
  if(mat.HasProperty("_MainTex")) {mat.SetTextureScale("_MainTex",new Vector2(2f,1f));mat.SetTextureOffset("_MainTex",Vector2.zero);}
  EditorUtility.SetDirty(mat);
  AssetDatabase.SaveAssetIfDirty(mat);
 }
 Debug.Log("Mario eye materials now sample the half-face atlas twice with Mirror wrapping.");
}
}
