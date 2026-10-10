using System.IO;
using UnityEditor;
using UnityEngine;

namespace BJ.TileMap.Editor
{
    /// <summary>
    /// 正方形グリッドにタイルを並べて JSON として保存するエディタウィンドウ。
    /// メニュー: Window > BJ > TileMap Editor
    ///
    /// 使い方:
    ///   1. Palette に TilePalette アセットを割り当てる
    ///   2. 「新規マップ」でサイズを決めて作成、または「読み込み」で既存JSONを開く
    ///   3. 右のパレットからタイルを選び、左のグリッドを左ドラッグで配置
    ///      (右ドラッグ / 消しゴム選択で削除)
    ///   4. 「保存」で .json を書き出す
    /// </summary>
    public class TileMapEditorWindow : EditorWindow
    {
        // --- 編集対象 ---
        private TileMapData _map;
        private TilePalette _palette;
        private string _currentPath;
        private bool _dirty;

        // --- 新規作成パラメータ ---
        private int _newWidth = 10;
        private int _newHeight = 10;
        private int _newTileSize = 16;

        // --- 編集状態 ---
        private const int EraserIndex = -2; // 「消しゴム」を表す特別な選択値
        private int _selectedTile = 0;
        private int _activeLayer = 0;

        // --- 表示 ---
        private float _cellPixels = 28f;
        private Vector2 _gridScroll;
        private Vector2 _paletteScroll;

        [MenuItem("Window/BJ/TileMap Editor")]
        public static void Open()
        {
            var win = GetWindow<TileMapEditorWindow>("TileMap Editor");
            win.minSize = new Vector2(640, 420);
            win.Show();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_map == null)
            {
                EditorGUILayout.HelpBox(
                    "マップがありません。ツールバーの「新規マップ」で作成するか「読み込み」で開いてください。",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawGridArea();
            DrawSidePanel();
            EditorGUILayout.EndHorizontal();
        }

        // ---------------------------------------------------------------
        // ツールバー
        // ---------------------------------------------------------------
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("新規マップ", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                CreateNewMapDialog();
            }
            if (GUILayout.Button("読み込み", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                LoadDialog();
            }
            using (new EditorGUI.DisabledScope(_map == null))
            {
                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Save(false);
                }
                if (GUILayout.Button("名前を付けて保存", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    Save(true);
                }
            }

            GUILayout.FlexibleSpace();

            if (_map != null)
            {
                string title = string.IsNullOrEmpty(_currentPath)
                    ? "(未保存)"
                    : Path.GetFileName(_currentPath);
                GUILayout.Label((_dirty ? "* " : "") + title, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ---------------------------------------------------------------
        // グリッド描画 + 編集
        // ---------------------------------------------------------------
        private void DrawGridArea()
        {
            EditorGUILayout.BeginVertical();

            // ズーム
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"サイズ: {_map.width} x {_map.height}", GUILayout.Width(120));
            GUILayout.Label("セル表示", GUILayout.Width(55));
            _cellPixels = GUILayout.HorizontalSlider(_cellPixels, 12f, 48f, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            _gridScroll = EditorGUILayout.BeginScrollView(_gridScroll);

            float totalW = _map.width * _cellPixels;
            float totalH = _map.height * _cellPixels;
            Rect gridRect = GUILayoutUtility.GetRect(totalW, totalH);

            // 背景
            EditorGUI.DrawRect(gridRect, new Color(0.15f, 0.15f, 0.15f));

            var layer = _map.layers[_activeLayer];

            for (int y = 0; y < _map.height; y++)
            {
                for (int x = 0; x < _map.width; x++)
                {
                    var cell = new Rect(
                        gridRect.x + x * _cellPixels,
                        gridRect.y + y * _cellPixels,
                        _cellPixels,
                        _cellPixels);

                    int tileIndex = layer.GetTile(x, y, _map.width, _map.height);
                    DrawCell(cell, tileIndex);
                }
            }

            DrawGridLines(gridRect);
            HandleGridInput(gridRect, layer);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCell(Rect cell, int tileIndex)
        {
            if (tileIndex == TileMapData.EmptyTile)
            {
                return;
            }

            Sprite sprite = _palette != null ? _palette.GetSprite(tileIndex) : null;
            if (sprite != null)
            {
                DrawSprite(cell, sprite);
            }
            else
            {
                // パレット未割り当て等。インデックスを色で表現しておく。
                EditorGUI.DrawRect(cell, IndexColor(tileIndex));
            }
        }

        private void DrawGridLines(Rect gridRect)
        {
            Handles.BeginGUI();
            Color prev = Handles.color;
            Handles.color = new Color(1f, 1f, 1f, 0.12f);
            for (int x = 0; x <= _map.width; x++)
            {
                float px = gridRect.x + x * _cellPixels;
                Handles.DrawLine(new Vector3(px, gridRect.y), new Vector3(px, gridRect.y + _map.height * _cellPixels));
            }
            for (int y = 0; y <= _map.height; y++)
            {
                float py = gridRect.y + y * _cellPixels;
                Handles.DrawLine(new Vector3(gridRect.x, py), new Vector3(gridRect.x + _map.width * _cellPixels, py));
            }
            Handles.color = prev;
            Handles.EndGUI();
        }

        private void HandleGridInput(Rect gridRect, TileLayer layer)
        {
            Event e = Event.current;
            if (!gridRect.Contains(e.mousePosition))
            {
                return;
            }

            bool isPaint = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag);
            if (!isPaint)
            {
                return;
            }

            int localX = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / _cellPixels);
            int localY = Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / _cellPixels);
            if (localX < 0 || localX >= _map.width || localY < 0 || localY >= _map.height)
            {
                return;
            }

            // 左ボタン=配置(消しゴム選択中は削除) / 右ボタン=削除
            bool erase = e.button == 1 || _selectedTile == EraserIndex;
            int value = erase ? TileMapData.EmptyTile : _selectedTile;

            if (layer.GetTile(localX, localY, _map.width, _map.height) != value)
            {
                layer.SetTile(localX, localY, _map.width, _map.height, value);
                _dirty = true;
                Repaint();
            }
            e.Use();
        }

        // ---------------------------------------------------------------
        // サイドパネル(パレット選択)
        // ---------------------------------------------------------------
        private void DrawSidePanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(220));

            EditorGUILayout.LabelField("パレット", EditorStyles.boldLabel);
            var newPalette = (TilePalette)EditorGUILayout.ObjectField(
                _palette, typeof(TilePalette), false);
            if (newPalette != _palette)
            {
                _palette = newPalette;
                if (_palette != null && _map != null)
                {
                    _map.paletteName = _palette.PaletteName;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ツール", EditorStyles.boldLabel);

            // 消しゴム
            bool eraserSelected = _selectedTile == EraserIndex;
            bool newEraser = GUILayout.Toggle(eraserSelected, "消しゴム", "Button");
            if (newEraser && !eraserSelected)
            {
                _selectedTile = EraserIndex;
            }
            else if (!newEraser && eraserSelected)
            {
                _selectedTile = 0;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("タイル", EditorStyles.boldLabel);

            if (_palette == null || _palette.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "タイルがありません。TilePalette を割り当てるか、スプライトを登録してください。",
                    MessageType.Info);
            }
            else
            {
                _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll);
                const int columns = 3;
                float size = 56f;
                int count = _palette.Count;
                int rows = Mathf.CeilToInt(count / (float)columns);
                for (int row = 0; row < rows; row++)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int col = 0; col < columns; col++)
                    {
                        int index = row * columns + col;
                        if (index >= count)
                        {
                            break;
                        }
                        DrawPaletteButton(index, size);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(
                "左ドラッグ: 配置 / 右ドラッグ: 削除",
                MessageType.None);

            EditorGUILayout.EndVertical();
        }

        private void DrawPaletteButton(int index, float size)
        {
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            bool selected = _selectedTile == index;

            EditorGUI.DrawRect(rect, selected
                ? new Color(0.25f, 0.5f, 0.9f)
                : new Color(0.2f, 0.2f, 0.2f));

            var inner = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
            Sprite sprite = _palette.GetSprite(index);
            if (sprite != null)
            {
                DrawSprite(inner, sprite);
            }
            else
            {
                EditorGUI.DrawRect(inner, IndexColor(index));
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                _selectedTile = index;
            }
        }

        // ---------------------------------------------------------------
        // 新規 / 保存 / 読込
        // ---------------------------------------------------------------
        private void CreateNewMapDialog()
        {
            if (_dirty && !ConfirmDiscard())
            {
                return;
            }

            // 簡易入力: 専用の小ダイアログを出す
            NewMapPopup.Show(this, _newWidth, _newHeight, _newTileSize, (w, h, ts) =>
            {
                _newWidth = w;
                _newHeight = h;
                _newTileSize = ts;
                _map = TileMapData.CreateEmpty(w, h, ts);
                _activeLayer = 0;
                _currentPath = null;
                _dirty = false;
                if (_palette != null)
                {
                    _map.paletteName = _palette.PaletteName;
                }
                Repaint();
            });
        }

        private void LoadDialog()
        {
            if (_dirty && !ConfirmDiscard())
            {
                return;
            }

            string path = EditorUtility.OpenFilePanel("タイルマップ(JSON)を読み込み", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<TileMapData>(json);
                if (data == null || data.layers == null || data.layers.Count == 0)
                {
                    EditorUtility.DisplayDialog("読み込み失敗", "有効なタイルマップJSONではありません。", "OK");
                    return;
                }
                _map = data;
                _activeLayer = 0;
                _currentPath = path;
                _dirty = false;
                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("読み込み失敗", ex.Message, "OK");
            }
        }

        private void Save(bool saveAs)
        {
            string path = _currentPath;
            if (saveAs || string.IsNullOrEmpty(path))
            {
                path = EditorUtility.SaveFilePanel(
                    "タイルマップを保存", Application.dataPath, "NewTileMap", "json");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
            }

            try
            {
                string json = JsonUtility.ToJson(_map, true);
                File.WriteAllText(path, json);
                _currentPath = path;
                _dirty = false;

                // プロジェクト内に保存した場合はインポートして TextAsset 化する
                if (path.Replace('\\', '/').StartsWith(Application.dataPath.Replace('\\', '/')))
                {
                    AssetDatabase.Refresh();
                }
                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("保存失敗", ex.Message, "OK");
            }
        }

        private bool ConfirmDiscard()
        {
            return EditorUtility.DisplayDialog(
                "未保存の変更",
                "保存していない変更があります。破棄して続行しますか？",
                "破棄する", "キャンセル");
        }

        // ---------------------------------------------------------------
        // 補助
        // ---------------------------------------------------------------
        /// <summary>Sprite を矩形内にアスペクト比保持で描画する。</summary>
        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            Texture2D tex = sprite.texture;
            if (tex == null)
            {
                return;
            }
            Rect tr = sprite.textureRect;
            var uv = new Rect(
                tr.x / tex.width,
                tr.y / tex.height,
                tr.width / tex.width,
                tr.height / tex.height);
            GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
        }

        /// <summary>パレット未割り当て時に、インデックスごとに安定した色を返す。</summary>
        private static Color IndexColor(int index)
        {
            float h = (index * 0.137f) % 1f;
            return Color.HSVToRGB(h, 0.6f, 0.85f);
        }
    }

    /// <summary>新規マップ作成の入力ポップアップ。</summary>
    public class NewMapPopup : EditorWindow
    {
        private int _w;
        private int _h;
        private int _tileSize;
        private System.Action<int, int, int> _onCreate;

        public static void Show(EditorWindow owner, int w, int h, int tileSize,
            System.Action<int, int, int> onCreate)
        {
            var popup = CreateInstance<NewMapPopup>();
            popup.titleContent = new GUIContent("新規マップ");
            popup._w = w;
            popup._h = h;
            popup._tileSize = tileSize;
            popup._onCreate = onCreate;
            popup.minSize = popup.maxSize = new Vector2(260, 140);
            popup.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("新しいタイルマップ", EditorStyles.boldLabel);
            _w = Mathf.Max(1, EditorGUILayout.IntField("幅 (タイル数)", _w));
            _h = Mathf.Max(1, EditorGUILayout.IntField("高さ (タイル数)", _h));
            _tileSize = Mathf.Max(1, EditorGUILayout.IntField("タイルサイズ(px)", _tileSize));

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("作成"))
            {
                _onCreate?.Invoke(_w, _h, _tileSize);
                Close();
            }
            if (GUILayout.Button("キャンセル"))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
