using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Creator
{
    // Screens share the catalog in the Creator scene; saved files contain only piece IDs and cells.
    public sealed class CreatorGameFlow : MonoBehaviour
    {
        CreatorScreen creator;
        RectTransform canvas, page;
        GameObject world;
        Camera gameCamera;
        MarioPlayerController player;
        LevelData currentLevel;
        TextMeshProUGUI status;
        bool playing, finished;
        int coins, pageIndex;
        const int PageSize = 6;
        readonly Color ink = new Color(.10f,.16f,.23f);
        readonly Color accent = new Color(.16f,.40f,.68f);

        void Start()
        {
            creator = GetComponent<CreatorScreen>();
            var go = new GameObject("GameScreens", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas = (RectTransform)go.transform;
            canvas.SetParent(transform, false);
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 20;
            var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = .5f;
            ShowHome();
        }

        RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            r.SetParent(parent,false); r.anchorMin = new Vector2(x,y); r.anchorMax = new Vector2(x+w,y+h);
            r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        TextMeshProUGUI Text(Transform parent, string value, float x,float y,float w,float h,int size)
        {
            var t = Rect("Text",parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = creator.font; t.text = value; t.fontSize = size; t.color = ink;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.richText = false;
            t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = size; return t;
        }
        void Button(Transform parent,string label,float x,float y,float w,float h,Action click)
        {
            var r = Rect(label,parent,x,y,w,h);
            var image = r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = accent;
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = image;
            var nav = b.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.None; b.navigation = nav;
            b.onClick.AddListener(()=>click()); Text(r,label,.04f,.05f,.92f,.9f,26).color = Color.white;
        }
        void Page(bool background = true)
        {
            if(page != null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            page = Rect("Page",canvas,0,0,1,1);
            if(background) page.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.94f,.96f,.98f);
        }
        void ClearWorld()
        {
            playing = false; player = null;
            if(world != null) { world.SetActive(false); Destroy(world); }
            if(gameCamera != null) { gameCamera.gameObject.SetActive(false); Destroy(gameCamera.gameObject); }
        }
        public void ShowHome()
        {
            ClearWorld(); creator.CanvasObject.SetActive(false); Page();
            Text(page,"MARIO MAKER",.15f,.68f,.7f,.14f,64);
            Text(page,"Construye un nivel. Dale vida. Juega.",.15f,.58f,.7f,.07f,28);
            Button(page,"Crear",.25f,.36f,.23f,.13f,CreateNew);
            Button(page,"Jugar",.52f,.36f,.23f,.13f,()=>ShowLibrary(0));
        }
        public void CreateNew()
        {
            ClearWorld(); Page(false); creator.CanvasObject.SetActive(true); creator.NewLevel();
            var nameField = (RectTransform)creator.CanvasObject.transform.Find("Header/LevelName");
            nameField.anchorMax = new Vector2(.34f,nameField.anchorMax.y);
            Button(page,"Inicio",.35f,.949f,.10f,.046f,ShowHome);
        }
        public void ShowLibrary(int index = 0)
        {
            ClearWorld(); creator.CanvasObject.SetActive(false); Page();
            Text(page,"Tus niveles",.15f,.80f,.7f,.1f,48);
            Button(page,"Inicio",.04f,.88f,.14f,.07f,ShowHome);
            status = Text(page,"Elige un nivel para jugar",.15f,.73f,.7f,.06f,23);
            string[] paths;
            try { paths = Directory.Exists(Application.persistentDataPath) ? Directory.GetFiles(Application.persistentDataPath,"Creator-*.json").OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToArray() : Array.Empty<string>(); }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException)
            { status.text = "No se pudo leer la carpeta de niveles."; return; }
            int pages = Math.Max(1,(paths.Length+PageSize-1)/PageSize);
            pageIndex = Mathf.Clamp(index,0,pages-1);
            if(paths.Length==0) status.text = "Todavía no hay niveles. Crea uno y pulsa Guardar.";
            for(int i=pageIndex*PageSize;i<Math.Min(paths.Length,(pageIndex+1)*PageSize);i++)
            {
                string path = paths[i]; string title = Path.GetFileNameWithoutExtension(path).Substring(8);
                int row = i%PageSize;
                Button(page,title,.24f,.63f-row*.085f,.52f,.065f,()=>PlayFile(path));
            }
            if(pageIndex>0) Button(page,"Anterior",.24f,.07f,.17f,.06f,()=>ShowLibrary(pageIndex-1));
            if(pageIndex+1<pages) Button(page,"Siguiente",.59f,.07f,.17f,.06f,()=>ShowLibrary(pageIndex+1));
            Text(page,(pageIndex+1)+" / "+pages,.44f,.07f,.12f,.06f,20);
        }
        public void PlayFile(string path)
        {
            try
            {
                var data = JsonUtility.FromJson<LevelData>(File.ReadAllText(path));
                var board = new CreatorBoard(creator.pieces);
                if(!board.Load(data)) { status.text = "Este archivo no contiene un nivel compatible."; return; }
                if(board.Start==null) { status.text = "Este nivel necesita un personaje. Añádelo desde el creador."; return; }
                if(board.Placements.Any(p=>board.Definition(p).prefab==null)) { status.text = "Faltan piezas necesarias para jugar este nivel."; return; }
                currentLevel = board.Export(); currentLevel.name = string.IsNullOrWhiteSpace(data.name) ? Path.GetFileNameWithoutExtension(path).Substring(8) : data.name;
                StartLevel();
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { status.text = "No se pudo abrir el nivel. Comprueba el archivo guardado."; }
        }
        public void StartLevel()
        {
            ClearWorld(); Page(false); creator.CanvasObject.SetActive(false); finished = false; coins = 0;
            world = new GameObject("PlayableLevel"); world.SetActive(false);
            var board = new CreatorBoard(creator.pieces); board.Load(currentLevel);
            foreach(var p in board.Placements)
            {
                var def = board.Definition(p);
                var instance = Instantiate(def.prefab, new Vector3(p.x+.5f,p.y,0), Quaternion.identity,world.transform);
                instance.name = def.label+"_"+p.x+"_"+p.y;
                if(def.category==Category.Blocks || def.isGoal)
                {
                    // Imported props have different pivots/scales. Fit visuals to a cell, keep physics exact.
                    foreach(var collider in instance.GetComponentsInChildren<Collider>()) collider.enabled = false;
                    var renderers = instance.GetComponentsInChildren<Renderer>();
                    if(renderers.Length>0)
                    {
                        var bounds = renderers[0].bounds;
                        foreach(var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                        float size = Mathf.Max(bounds.size.x,bounds.size.y);
                        if(size>.001f) instance.transform.localScale *= 1f/size;
                        bounds = renderers[0].bounds; foreach(var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                        instance.transform.position += new Vector3(p.x+.5f,p.y+.5f,0)-bounds.center;
                    }
                    var physics = new GameObject(def.isGoal ? "GoalTrigger" : "SolidCell"); physics.transform.SetParent(world.transform);
                    physics.transform.position = new Vector3(p.x+.5f,p.y+.5f,0);
                    var box = physics.AddComponent<BoxCollider>(); box.size = new Vector3(1,1,2);
                    if(def.isGoal) { box.isTrigger = true; physics.AddComponent<CreatorGoal>().flow = this; }
                }
                if(def.category==Category.Characters) player = instance.GetComponent<MarioPlayerController>();
                var coin = instance.GetComponent<CollectibleCoin>();
                if(coin!=null) coin.CollectedBy += _=> { coins++; UpdateStatus(); };
            }
            if(player==null) { ShowLibrary(pageIndex); status.text = "El personaje no tiene un controlador jugable."; return; }
            foreach(var enemy in world.GetComponentsInChildren<GoombaEnemy>(true))
                enemy.Configure(player,enemy.transform.position.x-3,enemy.transform.position.x+3);
            player.Damaged += ()=>Finish(false);
            player.AllowManualRespawn = false;
            world.SetActive(true);
            var cameraObject = new GameObject("LevelCamera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData));
            gameCamera = cameraObject.GetComponent<Camera>(); gameCamera.depth = 10;
            gameCamera.clearFlags = CameraClearFlags.SolidColor; gameCamera.backgroundColor = new Color(.55f,.78f,.94f);
            gameCamera.orthographic = true; gameCamera.orthographicSize = 5;
            gameCamera.transform.position = new Vector3(player.transform.position.x+3,4,-25);
            Button(page,"Niveles",.02f,.9f,.12f,.07f,()=>ShowLibrary(pageIndex));
            status = Text(page,"",.16f,.9f,.67f,.07f,24); UpdateStatus();
            Text(page,"A / D: moverse    W / Espacio: saltar    S: agacharse    Esc: niveles",.06f,.015f,.88f,.05f,20);
            playing = true;
        }
        void UpdateStatus() { if(status!=null) status.text = currentLevel.name+"   ·   Monedas: "+coins; }
        public void Finish(bool won)
        {
            if(!playing || finished) return;
            finished = true; player.enabled = false;
            var body = player.GetComponent<Rigidbody>(); body.linearVelocity = Vector3.zero; body.isKinematic = true;
            player.GetComponent<Animator>().speed = 0;
            foreach(var enemy in world.GetComponentsInChildren<GoombaEnemy>()) enemy.SetStopped(true);
            Text(page,won ? "¡Nivel completado!" : "Has perdido",.2f,.49f,.6f,.15f,56);
            Button(page,"Volver a intentar",.35f,.34f,.3f,.1f,StartLevel);
        }
        void Update()
        {
            if(!playing) return;
            var keyboard = Keyboard.current;
            if(keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) { ShowLibrary(pageIndex); return; }
            if(!finished && player.GetComponent<Rigidbody>().position.y < -5) Finish(false);
        }
        void LateUpdate()
        {
            if(!playing || player==null || gameCamera==null) return;
            var pos = player.transform.position;
            gameCamera.transform.position = new Vector3(Mathf.Clamp(pos.x+3,7,23),Mathf.Max(4,pos.y+1),-25);
        }
    }
}
