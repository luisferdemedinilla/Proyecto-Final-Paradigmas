using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class QuestionBlockTools
{
 public const string Folder="Assets/_Project/Prefabs/Items/Gameplay/";
 [MenuItem("Tools/Blocks/Create Question And Empty")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
  foreach(var n in new[]{"BlockEmpty","BlockQuestion"})if(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+n+".prefab")!=null)throw new Exception(n+" already exists; inspect before replacing");
  var scene=EditorSceneManager.NewPreviewScene();
  try
  {
   GameObject Model(string name,Transform parent){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BrickTools.Originals+name+".prefab"),scene);go.transform.SetParent(parent,false);return go;}
   GameObject Root(string name){var go=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);return go;}
   BoxCollider Box(GameObject root,Bounds bounds){if(root.GetComponentsInChildren<Collider>(true).Length>0)throw new Exception("Unexpected source collider");var box=root.AddComponent<BoxCollider>();box.center=bounds.center-root.transform.position;box.size=bounds.size;return box;}
   var empty=Root("BlockEmpty");var emptyModel=Model("Block Empty",empty.transform);emptyModel.name="Visual";var emptyBounds=HelloMarioPlayableBuilder.BoundsOf(empty);Box(empty,emptyBounds);PrefabUtility.SaveAsPrefabAsset(empty,Folder+"BlockEmpty.prefab");UnityEngine.Object.DestroyImmediate(empty);
   var question=Root("BlockQuestion");var questionVisual=Model("Block Question",question.transform);questionVisual.name="QuestionVisual";var bounds=HelloMarioPlayableBuilder.BoundsOf(question);
   var emptyVisual=Model("Block Empty",question.transform);emptyVisual.name="EmptyVisual";var eb=HelloMarioPlayableBuilder.BoundsOf(emptyVisual);
   emptyVisual.transform.localScale=new Vector3(bounds.size.x/eb.size.x,bounds.size.y/eb.size.y,bounds.size.z/eb.size.z);
   eb=HelloMarioPlayableBuilder.BoundsOf(emptyVisual);emptyVisual.transform.position+=bounds.center-eb.center;emptyVisual.SetActive(false);Box(question,bounds);
   var component=question.AddComponent<QuestionBlock>();var so=new SerializedObject(component);so.FindProperty("questionVisual").objectReferenceValue=questionVisual;so.FindProperty("emptyVisual").objectReferenceValue=emptyVisual;so.FindProperty("contentPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"GoldCoin.prefab");so.ApplyModifiedPropertiesWithoutUndo();
   PrefabUtility.SaveAsPrefabAsset(question,Folder+"BlockQuestion.prefab");AssetDatabase.SaveAssets();
   Directory.CreateDirectory("Temp/QuestionBlockAudit");File.WriteAllText("Temp/QuestionBlockAudit/build.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{emptySize=emptyBounds.size.ToString(),questionSize=bounds.size.ToString(),emptyReplacementSize=HelloMarioPlayableBuilder.BoundsOf(emptyVisual).size.ToString(),content=Folder+"GoldCoin.prefab"}));
  }finally{EditorSceneManager.ClosePreviewScene(scene);}
 }
}
