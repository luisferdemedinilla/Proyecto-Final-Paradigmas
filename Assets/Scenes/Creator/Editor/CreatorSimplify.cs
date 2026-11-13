using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Creator.Editor
{
    public static class CreatorSimplify
    {
        public static string Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path!="Assets/Scenes/creator.unity") throw new InvalidOperationException("Open Creator outside Play mode.");
            var screen=UnityEngine.Object.FindAnyObjectByType<CreatorScreen>();
            screen.pieces=screen.pieces.Where(p=>p.category==Category.Characters ||
                (p.category==Category.Blocks && new[]{"Brick","Brick Hard","Block Question"}.Contains(p.prefab.name)) ||
                (p.category==Category.Enemies && p.prefab.name=="Goomba") ||
                (p.category==Category.Extras && (p.isGoal || p.label=="Moneda"))).ToList();
            if(screen.pieces.Count!=11)throw new Exception("Expected exactly 11 pieces.");
            var canvas=screen.transform.Find("CreatorCanvas");
            void Remove(Transform parent,string name) {var t=parent.Find(name);if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}
            foreach(var name in new[]{"Selection","StartStatus","GoalStatus","Status","Undo","Erase","Instructions"})Remove(canvas,name);
            foreach(var name in new[]{"Step","Title","Subtitle","Load"})Remove(canvas.Find("Header"),name);
            string[] groups={"PERSONAJES","BLOQUES","ENEMIGOS","EXTRAS"};
            for(int i=0;i<4;i++)
            {
                var panel=canvas.Find(groups[i]);
                foreach(var name in new[]{"Accent","Hint","Previous","Next","Page"})Remove(panel,name);
                int count=screen.pieces.Count(p=>(int)p.category==i);
                for(int s=0;s<6;s++)
                {
                    if(s>=count)Remove(panel,"Piece_"+s);
                    else { var card=panel.Find("Piece_"+s);card.gameObject.SetActive(true);Remove(card,"Label"); }
                }
            }
            foreach(var cell in screen.GetComponentsInChildren<CreatorCell>(true)) {Remove(cell.transform,"Role");cell.badge=null;}
            var backup="Temp/Creator/before-simplify.json";
            if(File.Exists(backup))
            {
                var data=JsonUtility.FromJson<LevelData>(File.ReadAllText(backup));
                data.placements.RemoveAll(p=>!screen.pieces.Any(d=>d.id==p.pieceId));
                screen.initialDesign=JsonUtility.ToJson(data);
            }
            foreach(var piece in screen.pieces)Render(piece);
            AssetDatabase.Refresh();
            foreach(var piece in screen.pieces)
            {
                string path="Assets/Scenes/Creator/Thumbnails/"+piece.id+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.sRGBTexture=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                piece.thumbnail=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            screen.BuildUI();
            EditorUtility.SetDirty(screen);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Creator simplified: 3 characters, 3 blocks + empty, 1 Goomba, 4 extras.";
        }

        static void Render(Piece piece)
        {
            var preview=new PreviewRenderUtility();
            var materials=new System.Collections.Generic.List<Material>();
            try
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(piece.prefab,preview.camera.scene);preview.AddSingleGO(go);
                foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{if(m==null)return null;var copy=new Material(m);materials.Add(copy);return copy;}).ToArray();
                go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;
                foreach(var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
                foreach(var animator in go.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                if(piece.category==Category.Characters)
                {
                    string name=piece.label;
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Characters/"+name+"/HelloMario/Animations/Gameplay/Idle.anim");
                    if(clip!=null)clip.SampleAnimation(go,0);
                    var visual=go.transform.Find("Visual");if(visual!=null)visual.localRotation=Quaternion.Euler(0,90,0);
                }
                if(piece.category==Category.Enemies)
                {
                    var visual=go.transform.Find("Visual");if(visual!=null)visual.localRotation=Quaternion.Euler(0,90,0);
                    foreach(var animator in go.GetComponentsInChildren<Animator>(true))
                    {
                        if(animator.runtimeAnimatorController!=null)
                        {
                            var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.Contains("Walk"));
                            if(clip!=null)clip.SampleAnimation(go,0);
                        }
                    }
                }
                bool first=true;var bounds=new Bounds();
                foreach(var renderer in go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled))
                {
                    if(renderer is SkinnedMeshRenderer skin)
                    {
                        var mesh=new Mesh();skin.BakeMesh(mesh);
                        foreach(var v in mesh.vertices) {var point=skin.transform.TransformPoint(v);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                    else { if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds); }
                }
                if(piece.category==Category.Enemies)
                    foreach(var renderer in go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled)) bounds.Encapsulate(renderer.bounds);
                var cam=preview.camera;cam.orthographic=true;cam.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.y)*.51f;
                cam.transform.position=bounds.center+Vector3.back*(bounds.size.magnitude+10);cam.transform.rotation=Quaternion.identity;
                cam.nearClipPlane=.01f;cam.farClipPlane=bounds.size.magnitude*3+30;
                cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;
                preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(45,-35,0);
                preview.lights[1].intensity=.5f;preview.lights[1].transform.rotation=Quaternion.Euler(0,180,0);
                preview.ambientColor=new Color(.68f,.72f,.8f);
                preview.BeginStaticPreview(new Rect(0,0,256,256));preview.Render(true);
                var image=preview.EndStaticPreview();
                var pixels=image.GetPixels32();
                var seen=new bool[pixels.Length];var queue=new System.Collections.Generic.Queue<int>();
                void Add(int i){if(seen[i])return;seen[i]=true;var p=pixels[i];if(p.r<8&&p.g<8&&p.b<8)queue.Enqueue(i);}
                for(int i=0;i<256;i++){Add(i);Add(255*256+i);Add(i*256);Add(i*256+255);}
                while(queue.Count>0){int i=queue.Dequeue();pixels[i].a=0;if(i%256>0)Add(i-1);if(i%256<255)Add(i+1);if(i>=256)Add(i-256);if(i<255*256)Add(i+256);}
                var transparent=new Texture2D(256,256,TextureFormat.RGBA32,false);
                transparent.SetPixels32(pixels);transparent.Apply();
                File.WriteAllBytes("Assets/Scenes/Creator/Thumbnails/"+piece.id+".png",transparent.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(transparent);
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally {preview.Cleanup();foreach(var material in materials)UnityEngine.Object.DestroyImmediate(material);}
        }
    }
}
