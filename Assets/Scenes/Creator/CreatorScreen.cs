using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Creator
{
    [DisallowMultipleComponent]
    public sealed class CreatorScreen : MonoBehaviour
    {
        public List<Piece> pieces = new List<Piece>();
        public Grid worldGrid;
        public TMP_FontAsset font;
        public string initialDesign;
        [SerializeField] RectTransform canvasRoot;
        [SerializeField] List<CreatorCell> cells = new List<CreatorCell>();
        [SerializeField] List<UnityEngine.UI.Button> cards = new List<UnityEngine.UI.Button>();
        [SerializeField] UnityEngine.UI.Button eraseButton, saveButton;
        [SerializeField] TMP_InputField nameInput;
        CreatorBoard board;
        Piece selected;
        bool erasing;
        float savedUntil;
        TextMeshProUGUI storageStatus;
        public CreatorBoard Board => board;
        public GameObject CanvasObject => canvasRoot.gameObject;
        public void NewLevel()
        {
            board = new CreatorBoard(pieces);
            nameInput.text = "juego1";
            for (int i = 1; File.Exists(SavePath); i++) nameInput.text = "juego" + (i + 1);
            erasing = false;
            Refresh();
        }
        public string LevelName => string.IsNullOrWhiteSpace(nameInput.text) ? "juego1" : nameInput.text.Trim();
        public string SavePath => Path.Combine(Application.persistentDataPath, "Creator-" + SafeName(LevelName) + ".json");
        static string SafeName(string value) => System.Text.RegularExpressions.Regex.Replace(value, @"[^\p{L}\p{N} _-]", "_");

        void Awake()
        {
            InitializeModel();
            if (canvasRoot == null) BuildUI();
            EnsureGridLines();
            EnsureStorageUI();
            eraseButton.onClick.AddListener(() => { erasing = true; Refresh(); });
            saveButton.onClick.AddListener(Save);
            for (int i = 0; i < cards.Count; i++)
            {
                int index = i;
                cards[i].onClick.AddListener(() => { selected = pieces[index]; erasing = false; Refresh(); });
            }
            Refresh();
            if (GetComponent<CreatorGameFlow>() == null) gameObject.AddComponent<CreatorGameFlow>();
        }

        void InitializeModel()
        {
            board = new CreatorBoard(pieces); selected = pieces.FirstOrDefault();
            if (!string.IsNullOrEmpty(initialDesign)) board.Load(JsonUtility.FromJson<LevelData>(initialDesign));
        }

        void Update()
        {
            if (!canvasRoot.gameObject.activeInHierarchy) return;
            if (savedUntil > 0 && Time.unscaledTime >= savedUntil)
            { saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Guardar"; savedUntil = 0; }
            var keyboard = Keyboard.current;
            if (keyboard == null || nameInput.isFocused) return;
            if (keyboard.ctrlKey.isPressed && keyboard.zKey.wasPressedThisFrame) { board.Undo(); Refresh(); }
            if (keyboard.escapeKey.wasPressedThisFrame) { erasing = false; Refresh(); }
        }

        public void Paint(int x, int y, bool rightClick)
        {
            if (rightClick || erasing) board.Erase(x, y);
            else if (selected != null) board.Place(selected.id, x, y);
            Refresh();
        }

        public bool TryGetSpawn(out GameObject prefab, out Vector3 position)
        {
            prefab = null; position = Vector3.zero;
            var start = board?.Start;
            if (start == null) return false;
            prefab = board.Definition(start).prefab;
            position = worldGrid != null ? worldGrid.CellToWorld(new Vector3Int(start.x, start.y, 0)) + worldGrid.transform.TransformVector(new Vector3(worldGrid.cellSize.x * .5f, 0, 0)) : new Vector3(start.x + .5f, start.y, 0);
            return true;
        }

        public void Save()
        {
            try
            {
                nameInput.text = LevelName;
                var data = board.Export(); data.name = LevelName;
                Directory.CreateDirectory(Application.persistentDataPath);
                // Replace only after the complete JSON has been written successfully.
                string temporaryPath = SavePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
                if (File.Exists(SavePath)) File.Replace(temporaryPath, SavePath, SavePath + ".bak");
                else File.Move(temporaryPath, SavePath);
                saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Guardado";
                storageStatus.text = "Guardado: " + LevelName;
                Debug.Log("Nivel guardado en: " + SavePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Error al guardar"; storageStatus.text = "No se pudo guardar el nivel."; Debug.LogWarning(ex.Message); }
            savedUntil = Time.unscaledTime + 2;
        }

        public void LoadSavedLevel()
        {
            try
            {
                if (!File.Exists(SavePath))
                { storageStatus.text = "No hay un nivel guardado con ese nombre."; return; }
                var data = JsonUtility.FromJson<LevelData>(File.ReadAllText(SavePath));
                if (!board.Load(data))
                { storageStatus.text = "El archivo no contiene un nivel compatible."; return; }
                // Keep the requested name so subsequent saves use the same file.
                nameInput.text = LevelName;
                erasing = false;
                Refresh();
                storageStatus.text = "Cargado: " + LevelName + " (Ctrl+Z para deshacer)";
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { storageStatus.text = "No se pudo cargar el nivel."; Debug.LogWarning(ex.Message); }
        }

        void OpenSaveFolder()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                Application.OpenURL(new Uri(Application.persistentDataPath + Path.DirectorySeparatorChar).AbsoluteUri);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { storageStatus.text = "No se pudo abrir la carpeta."; Debug.LogWarning(ex.Message); }
        }

        void EnsureStorageUI()
        {
            var header = canvasRoot.Find("Header");
            Rect("LevelName", header, .02f, .36f, .45f, .94f);
            Rect("Save", header, .85f, .36f, .98f, .94f);
            var load = Button("Load", header, .47f, .36f, .62f, .94f, "Cargar");
            var folder = Button("OpenFolder", header, .64f, .36f, .83f, .94f, "Abrir carpeta");
            storageStatus = Label("StorageStatus", header, .02f, .01f, .98f, .32f,
                "Escribe el nombre del nivel para guardarlo o cargarlo.", 16);
            load.onClick.RemoveListener(LoadSavedLevel);
            load.onClick.AddListener(LoadSavedLevel);
            folder.onClick.RemoveListener(OpenSaveFolder);
            folder.onClick.AddListener(OpenSaveFolder);
        }

        public void Refresh()
        {
            if (board == null) InitializeModel();
            foreach (var cell in cells)
            {
                var placement = board.At(cell.x, cell.y);
                var piece = placement == null || placement.y != cell.y ? null : board.Definition(placement);
                cell.icon.texture = piece?.thumbnail; cell.icon.enabled = piece != null;
                bool character = piece != null && piece.category == Category.Characters;
                var frame = (RectTransform)cell.icon.transform.parent;
                frame.anchorMin = character ? new Vector2(-.5f,0) : Vector2.zero;
                frame.anchorMax = character ? new Vector2(1.5f,2) : Vector2.one;
                frame.offsetMin = frame.offsetMax = Vector2.zero;
                if(character)cell.transform.SetAsLastSibling();
                // Terrain fills its cell; characters and tall objects retain their proportions.
                bool block = piece != null && piece.category == Category.Blocks;
                var aspect = cell.icon.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                aspect.enabled = !block;
                cell.icon.uvRect = block ? new UnityEngine.Rect(.01f,.01f,.98f,.98f) : new UnityEngine.Rect(0,0,1,1);
                if (block)
                {
                    var iconRect = cell.icon.rectTransform;
                    iconRect.anchorMin = Vector2.zero; iconRect.anchorMax = Vector2.one;
                    iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
                }
                cell.background.color = new Color(.89f, .94f, .97f);
            }
            canvasRoot.Find("BoardFrame/Cells/GridLines").SetAsLastSibling();
            for (int i = 0; i < cards.Count; i++)
                cards[i].GetComponent<UnityEngine.UI.Image>().color = selected == pieces[i] && !erasing ? new Color(.76f,.79f,.81f) : new Color(.94f,.94f,.94f);
            eraseButton.GetComponent<UnityEngine.UI.Image>().color = erasing ? new Color(.76f,.79f,.81f) : new Color(.94f,.94f,.94f);
        }

        // Reuses the existing canvas, grid and retained cards when adjusting the Creator scene.
        public void BuildUI()
        {
            InitializeModel(); cells.Clear(); cards.Clear();
            if (canvasRoot == null)
            {
                var go = new GameObject("CreatorCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                go.transform.SetParent(transform, false); canvasRoot = (RectTransform)go.transform;
                go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = .5f;
            }
            Panel("Background", canvasRoot, 0,0,1,1, new Color(.96f,.96f,.96f));
            var header = Panel("Header", canvasRoot, 0,.92f,1,1, new Color(.96f,.96f,.96f));
            var field = Panel("LevelName",header,.02f,.18f,.45f,.82f,Color.white);
            field.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            nameInput = field.GetComponent<TMP_InputField>() ?? field.gameObject.AddComponent<TMP_InputField>();
            var textArea = Rect("Text Area",field,.035f,.08f,.965f,.92f);
            var label = Label("Text",textArea,0,0,1,1,"juego1",26);
            nameInput.textViewport = textArea; nameInput.textComponent = label;
            nameInput.fontAsset = font; nameInput.characterLimit = 50; nameInput.lineType = TMP_InputField.LineType.SingleLine;
            nameInput.text = "juego1";
            saveButton = Button("Save",header,.85f,.18f,.98f,.82f,"Guardar");
            var gridFrame = Panel("BoardFrame",canvasRoot,0,.23f,1,.92f,new Color(.70f,.77f,.81f));
            var grid = Rect("Cells",gridFrame,0,0,1,1);
            var oldAspect = grid.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            if (oldAspect != null) DestroyImmediate(oldAspect);
            grid.anchorMin=Vector2.zero;grid.anchorMax=Vector2.one;grid.offsetMin=grid.offsetMax=Vector2.zero;
            for (int y=0;y<CreatorBoard.Rows;y++)
            for (int x=0;x<CreatorBoard.Columns;x++)
            {
                var rect = Panel("Cell_"+x+"_"+y,grid,x/(float)CreatorBoard.Columns,y/(float)CreatorBoard.Rows,(x+1)/(float)CreatorBoard.Columns,(y+1)/(float)CreatorBoard.Rows,Color.white);
                rect.offsetMin=rect.offsetMax=Vector2.zero;
                var cell=rect.GetComponent<CreatorCell>() ?? rect.gameObject.AddComponent<CreatorCell>();
                cell.screen=this;cell.x=x;cell.y=y;cell.background=rect.GetComponent<UnityEngine.UI.Image>();cell.background.raycastTarget=true;
                cell.icon=Icon(rect,0,0,1,1);cells.Add(cell);
            }
            EnsureGridLines();
            string[] titles={"PERSONAJES","BLOQUES","ENEMIGOS","EXTRAS"};
            for(int c=0;c<4;c++)
            {
                var panel=Panel(titles[c],canvasRoot,c*.25f+.008f,.015f,(c+1)*.25f-.008f,.215f,Color.white);
                Label("Heading",panel,.05f,.73f,.95f,.95f,titles[c],20);
                var group=pieces.Where(p=>(int)p.category==c).ToList();
                int count=group.Count+(c==1?1:0);
                for(int i=0;i<group.Count;i++)
                {
                    float width=.90f/count, left=.05f+i*width;
                    var card=Button("Piece_"+i,panel,left,.08f,left+width-.015f,.66f,null);
                    var image=Icon((RectTransform)card.transform,.04f,.04f,.96f,.96f);
                    image.texture=group[i].thumbnail;
                    cards.Add(card);
                }
                if(c==1)
                {
                    eraseButton=Button("Erase",panel,.05f+3*.90f/count,.08f,.935f,.66f,null);
                    // Empty tile: return a cell to nothing, with no extra text.
                    var empty=Panel("Empty",eraseButton.transform,.23f,.23f,.77f,.77f,new Color(.70f,.77f,.81f));
                    Panel("Inside",empty,.04f,.04f,.96f,.96f,new Color(.89f,.94f,.97f));
                }
            }
            Refresh();
        }

        public void EnsureGridLines()
        {
            var grid = canvasRoot.Find("BoardFrame/Cells");
            var boardFrame = grid.parent;
            var layout = boardFrame.GetComponent<CreatorBoardLayout>() ?? boardFrame.gameObject.AddComponent<CreatorBoardLayout>();
            layout.Apply();
            foreach(var existing in grid.GetComponentsInChildren<CreatorCell>(true))
                if(existing.x >= CreatorBoard.Columns || existing.y >= CreatorBoard.Rows)
                {
                    existing.gameObject.SetActive(false);
                    if(Application.isPlaying) Destroy(existing.gameObject); else DestroyImmediate(existing.gameObject);
                }
            cells.Clear();
            for (int y = 0; y < CreatorBoard.Rows; y++)
            for (int x = 0; x < CreatorBoard.Columns; x++)
            {
                var rect = Panel("Cell_"+x+"_"+y,grid,x/(float)CreatorBoard.Columns,y/(float)CreatorBoard.Rows,(x+1)/(float)CreatorBoard.Columns,(y+1)/(float)CreatorBoard.Rows,new Color(.89f,.94f,.97f));
                var cell = rect.GetComponent<CreatorCell>() ?? rect.gameObject.AddComponent<CreatorCell>();
                cell.screen = this; cell.x = x; cell.y = y;
                cell.background = rect.GetComponent<UnityEngine.UI.Image>();cell.background.raycastTarget = true;
                cell.icon = Icon(rect,0,0,1,1);cells.Add(cell);
            }
            foreach (var cell in cells)
            {
                var rect = (RectTransform)cell.transform;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            // Fit the whole board without stretching its cells: width / height = columns / rows.
            var gridAspect = grid.GetComponent<UnityEngine.UI.AspectRatioFitter>() ?? grid.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            gridAspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            gridAspect.aspectRatio = CreatorBoard.Columns / (float)CreatorBoard.Rows;
            var overlay = Rect("GridLines", grid, 0, 0, 1, 1);
            var lines = overlay.GetComponent<CreatorGridLines>() ?? overlay.gameObject.AddComponent<CreatorGridLines>();
            lines.color = new Color(.59f,.69f,.75f,1);
            lines.raycastTarget = false;
            overlay.SetAsLastSibling();
        }

        static RectTransform Rect(string name,Transform parent,float x0,float y0,float x1,float y1)
        {
            var found=parent.Find(name);
            var rect=found!=null ? (RectTransform)found : (RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            if(found==null)rect.SetParent(parent,false);
            rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;
            return rect;
        }
        static RectTransform Panel(string name,Transform parent,float x0,float y0,float x1,float y1,Color color)
        {
            var rect=Rect(name,parent,x0,y0,x1,y1);
            var image=rect.GetComponent<UnityEngine.UI.Image>() ?? rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color=color;image.raycastTarget=false;return rect;
        }
        TextMeshProUGUI Label(string name,Transform parent,float x0,float y0,float x1,float y1,string text,float size)
        {
            var rect=Rect(name,parent,x0,y0,x1,y1);
            var label=rect.GetComponent<TextMeshProUGUI>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=font;label.text=text;label.fontSize=size;label.color=new Color(.14f,.14f,.14f);
            label.raycastTarget=false;label.alignment=TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode=TextWrappingModes.NoWrap;return label;
        }
        UnityEngine.UI.Button Button(string name,Transform parent,float x0,float y0,float x1,float y1,string text)
        {
            var rect=Panel(name,parent,x0,y0,x1,y1,new Color(.88f,.88f,.88f));
            var image=rect.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=true;
            var button=rect.GetComponent<UnityEngine.UI.Button>() ?? rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var navigation=button.navigation;navigation.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=navigation;
            if(text!=null)Label("Label",rect,0,0,1,1,text,22).alignment=TextAlignmentOptions.Center;
            return button;
        }
        static UnityEngine.UI.RawImage Icon(RectTransform parent,float x0,float y0,float x1,float y1)
        {
            var wrapper=Rect("IconFrame",parent,x0,y0,x1,y1);
            var rect=Rect("Icon",wrapper,0,0,1,1);
            var icon=rect.GetComponent<UnityEngine.UI.RawImage>() ?? rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();icon.raycastTarget=false;
            var aspect=rect.GetComponent<UnityEngine.UI.AspectRatioFitter>() ?? rect.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            return icon;
        }
    }
}
