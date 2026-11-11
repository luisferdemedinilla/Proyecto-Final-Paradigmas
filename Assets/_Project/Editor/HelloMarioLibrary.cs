using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class HelloMarioLibrary
{
    [Serializable] public class Entry {public string source,destination,guid;}
    static Entry[] entries;static int index;static bool running;
    public static void Folder(string path)
    {
        path=path.Replace('\\','/');if(AssetDatabase.IsValidFolder(path)) return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    [MenuItem("Tools/Hello Mario/Organize Library")]
    public static void Organize()
    {
        if(running||EditorApplication.isPlaying) throw new Exception("Exit play mode or wait for migration");
        entries=Newtonsoft.Json.JsonConvert.DeserializeObject<Entry[]>(File.ReadAllText("Documentation/HelloMario-migration.json"));
        foreach(var e in entries) {var path=AssetDatabase.GUIDToAssetPath(e.guid);if(path!=e.source&&path!=e.destination) throw new Exception("Unexpected source: "+e.source);if(File.Exists(e.destination)&&path!=e.destination) throw new Exception("Destination occupied: "+e.destination);}
        foreach(var folder in entries.Select(e=>Path.GetDirectoryName(e.destination).Replace('\\','/')).Distinct()) Folder(folder);
        index=0;running=true;AssetDatabase.StartAssetEditing();EditorApplication.update+=Step;
    }
    static void Step()
    {
        try
        {
            for(int n=0;n<100 && index<entries.Length;n++,index++)
            {
                var e=entries[index];if(AssetDatabase.GUIDToAssetPath(e.guid)==e.destination) continue;
                string error=AssetDatabase.MoveAsset(e.source,e.destination);if(!string.IsNullOrEmpty(error)) throw new Exception(error);
            }
            File.WriteAllText("Temp/HelloMarioAudit/migration-progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{index,total=entries.Length}));
            if(index==entries.Length) {Finish();Debug.Log("Hello Mario: all 5850 resources organized with their original GUIDs.");}
        }
        catch(Exception e) {Finish();Debug.LogException(e);File.WriteAllText("Temp/HelloMarioAudit/migration-error.txt",e.ToString());}
    }
    static void Finish(){EditorApplication.update-=Step;running=false;AssetDatabase.StopAssetEditing();AssetDatabase.SaveAssets();}
    static SerializedProperty Saved(SerializedObject obj,string array,string key)
    {
        var list=obj.FindProperty("m_SavedProperties."+array);
        for(int i=0;i<list.arraySize;i++){var item=list.GetArrayElementAtIndex(i);if(item.FindPropertyRelative("first").stringValue==key) return item.FindPropertyRelative("second");}return null;
    }
    [MenuItem("Tools/Hello Mario/Adapt Materials To URP")]
    public static void Materials()
    {
        var list=Newtonsoft.Json.JsonConvert.DeserializeObject<Entry[]>(File.ReadAllText("Documentation/HelloMario-migration.json"));
        var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null) throw new Exception("URP Lit unavailable");
        int count=0;var report=new List<object>();
        foreach(var e in list.Where(e=>e.destination.EndsWith(".mat",StringComparison.OrdinalIgnoreCase)))
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(e.destination);var so=new SerializedObject(mat);
            var main=Saved(so,"m_TexEnvs","_MainTex");var tex=main?.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
            var tiling=main==null?Vector2.one:main.FindPropertyRelative("m_Scale").vector2Value;var offset=main==null?Vector2.zero:main.FindPropertyRelative("m_Offset").vector2Value;
            var normal=Saved(so,"m_TexEnvs","_BumpMap")?.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
            var emission=Saved(so,"m_TexEnvs","_EmissionMap")?.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
            var color=Saved(so,"m_Colors","_Color")?.colorValue??Color.white;
            mat.shader=shader;mat.shaderKeywords=Array.Empty<string>();
            mat.SetTexture("_BaseMap",tex);mat.SetTextureScale("_BaseMap",tiling);mat.SetTextureOffset("_BaseMap",offset);mat.SetColor("_BaseColor",color);
            mat.SetFloat("_Smoothness",.15f);mat.SetFloat("_Metallic",0);mat.SetFloat("_Cull",0);
            mat.SetTexture("_BumpMap",normal);if(normal!=null)mat.EnableKeyword("_NORMALMAP");
            if(emission!=null){mat.SetTexture("_EmissionMap",emission);mat.SetColor("_EmissionColor",Color.white);mat.EnableKeyword("_EMISSION");}
            mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.3f);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;mat.SetOverrideTag("RenderType","TransparentCutout");
            EditorUtility.SetDirty(mat);report.Add(new{e.destination,baseTexture=AssetDatabase.GetAssetPath(tex),normal=AssetDatabase.GetAssetPath(normal),shader=shader.name});count++;
        }
        AssetDatabase.SaveAssets();File.WriteAllText("Temp/HelloMarioAudit/materials.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));Debug.Log("Hello Mario: adapted "+count+" materials to URP; original serialized materials backed up in Documentation.");
    }
}
