using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public sealed class MarioAnimationPreviewWindow : EditorWindow
{
    PreviewRenderUtility preview;
    GameObject instance;
    AnimationClip clip;
    Image view;
    Texture2D frame;
    Slider timeline, angle;
    Toggle playing;
    double lastTime;
    float time;
    IVisualElementScheduledItem ticker;
    public int renderedFrames {get;private set;}

    [MenuItem("Tools/Mario Animations/Preview Animations")]
    public static void Open() {var w=GetWindow<MarioAnimationPreviewWindow>();w.titleContent=new GUIContent("Mario Animations");w.minSize=new Vector2(420,520);w.Show();}

    public void CreateGUI()
    {
        Release();rootVisualElement.Clear();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MarioAnimationTools.PrefabPath);
        if(prefab==null) {rootVisualElement.Add(new Label("No se encuentra Mario.prefab."));return;}
        preview=new PreviewRenderUtility();
        instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,preview.camera.scene);preview.AddSingleGO(instance);
        instance.GetComponent<Animator>().runtimeAnimatorController=null;
        foreach(var r in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.updateWhenOffscreen=true;
        var choices=MarioAnimationTools.ClipNames.ToList();
        var picker=new PopupField<string>("Animacion",choices,0);
        rootVisualElement.Add(picker);
        playing=new Toggle("Reproducir") {value=true};rootVisualElement.Add(playing);
        timeline=new Slider("Tiempo",0,1);rootVisualElement.Add(timeline);
        angle=new Slider("Camara",-180,180) {value=25};rootVisualElement.Add(angle);
        var openClip=new Button(()=>{Selection.activeObject=clip;EditorGUIUtility.PingObject(clip);}) {text="Seleccionar clip en Project"};rootVisualElement.Add(openClip);
        var help=new Label("Vista aislada: no modifica el prefab ni la escena.\nJump se repite aqui para facilitar su revision; no tiene Loop Time.");
        help.style.whiteSpace=WhiteSpace.Normal;rootVisualElement.Add(help);
        view=new Image {scaleMode=ScaleMode.ScaleToFit};view.style.flexGrow=1;view.style.minHeight=320;rootVisualElement.Add(view);
        picker.RegisterValueChangedCallback(e=>SelectClip(e.newValue));
        timeline.RegisterValueChangedCallback(e=>{time=e.newValue;playing.value=false;DrawFrame();});
        angle.RegisterValueChangedCallback(e=>DrawFrame());
        SelectClip("Idle");
        lastTime=EditorApplication.timeSinceStartup;
        ticker=rootVisualElement.schedule.Execute(Tick).Every(50);
    }

    void SelectClip(string name)
    {
        clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(MarioAnimationTools.Folder+"/"+name+".anim");time=0;
        if(clip!=null) {timeline.highValue=clip.length;timeline.SetValueWithoutNotify(0);DrawFrame();}
    }

    void Tick()
    {
        if(preview==null || clip==null || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var now=EditorApplication.timeSinceStartup;float delta=(float)Math.Min(now-lastTime,0.1);lastTime=now;
        if(!playing.value) return;
        time=(time+delta)%clip.length;timeline.SetValueWithoutNotify(time);DrawFrame();
    }

    void DrawFrame()
    {
        if(preview==null || instance==null || clip==null) return;
        clip.SampleAnimation(instance,time);
        MarioAnimationValidation.ConfigureCamera(preview,angle.value);
        var next=MarioAnimationValidation.Render(preview,512);
        view.image=next;if(frame!=null) DestroyImmediate(frame);frame=next;renderedFrames++;
    }

    void OnDisable() {Release();}
    [MenuItem("Tools/Mario Animations/Verify Preview Playback")]
    public static void VerifyPlayback()
    {
        Open();var w=GetWindow<MarioAnimationPreviewWindow>();
        w.rootVisualElement.Q<PopupField<string>>().value="Run";w.playing.value=true;
        int initialFrames=w.renderedFrames;var initialImage=w.frame.EncodeToPNG();
        double started=EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction poll=null;
        poll=()=>
        {
            if(w==null || w.frame==null) {EditorApplication.update-=poll;return;}
            if(EditorApplication.timeSinceStartup-started<1.1) return;
            EditorApplication.update-=poll;
            bool advancing=w.renderedFrames-initialFrames>=3;
            bool changing=!initialImage.SequenceEqual(w.frame.EncodeToPNG());
            System.IO.File.WriteAllText(MarioAnimationValidation.Output+"/preview-validation.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(new {advancing,changing,renderedFrames=w.renderedFrames-initialFrames},Newtonsoft.Json.Formatting.Indented));
            if(!advancing || !changing) Debug.LogError("Mario preview did not advance visibly.");
            else Debug.Log("Mario preview playback verified: frames and rendered pose advance.");
        };
        EditorApplication.update+=poll;
    }
    void Release()
    {
        ticker?.Pause();ticker=null;
        if(view!=null) view.image=null;
        if(frame!=null) DestroyImmediate(frame);frame=null;
        preview?.Cleanup();preview=null;instance=null;
    }
}
