using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BJ.TileMap
{
    /// <summary>
    /// 外部 JSON ファイルを読み込み、Unity 標準の Tilemap に描画するローダー。
    ///
    /// セットアップ:
    ///   1. シーンに GameObject を作り、子に Grid + Tilemap を用意する
    ///      (Hierarchy で右クリック > 2D Object > Tilemap > Rectangular でまとめて作成可)
    ///   2. この TileMapLoader を任意の GameObject に付け、
    ///      targetTilemap に Tilemap を、palette にパレットを割り当てる
    ///   3. jsonFile に TextAsset(保存した .json)を割り当てるか、
    ///      LoadFromPath で絶対パスから読み込む
    /// </summary>
    public class TileMapLoader : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField]
        private Tilemap targetTilemap;

        [SerializeField]
        private TilePalette palette;

        [Header("読み込み元 (どちらか)")]
        [Tooltip("プロジェクト内に取り込んだ .json を TextAsset として割り当てる")]
        [SerializeField]
        private TextAsset jsonFile;

        [Tooltip("実行時に絶対パスから読み込む場合のパス(jsonFile より優先度は低い)")]
        [SerializeField]
        private string externalJsonPath = "";

        [Header("オプション")]
        [SerializeField]
        private bool loadOnStart = true;

        // index -> TileBase のキャッシュ。同じインデックスのタイルを使い回す。
        private readonly Dictionary<int, TileBase> _tileCache = new Dictionary<int, TileBase>();

        private void Start()
        {
            if (loadOnStart)
            {
                Load();
            }
        }

        /// <summary>割り当て済みのソースから読み込んで描画する。</summary>
        public void Load()
        {
            string json = null;

            if (jsonFile != null)
            {
                json = jsonFile.text;
            }
            else if (!string.IsNullOrEmpty(externalJsonPath) && File.Exists(externalJsonPath))
            {
                json = File.ReadAllText(externalJsonPath);
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[TileMapLoader] 読み込むJSONが指定されていません。", this);
                return;
            }

            var data = JsonUtility.FromJson<TileMapData>(json);
            Render(data);
        }

        /// <summary>絶対パスを指定して読み込む。</summary>
        public void LoadFromPath(string path)
        {
            externalJsonPath = path;
            jsonFile = null;
            Load();
        }

        /// <summary>TileMapData を Tilemap に描画する。</summary>
        public void Render(TileMapData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[TileMapLoader] データが null です。", this);
                return;
            }
            if (targetTilemap == null)
            {
                Debug.LogError("[TileMapLoader] targetTilemap が未割り当てです。", this);
                return;
            }
            if (palette == null)
            {
                Debug.LogError("[TileMapLoader] palette が未割り当てです。", this);
                return;
            }

            targetTilemap.ClearAllTiles();
            _tileCache.Clear();

            foreach (var layer in data.layers)
            {
                if (layer == null || layer.tiles == null)
                {
                    continue;
                }

                for (int y = 0; y < data.height; y++)
                {
                    for (int x = 0; x < data.width; x++)
                    {
                        int tileIndex = layer.GetTile(x, y, data.width, data.height);
                        if (tileIndex == TileMapData.EmptyTile)
                        {
                            continue;
                        }

                        TileBase tile = GetOrCreateTile(tileIndex);
                        if (tile == null)
                        {
                            continue;
                        }

                        // JSON は左上原点(row-major)。Tilemap は下方向が +Y なので y を反転させる。
                        int cellY = data.height - 1 - y;
                        targetTilemap.SetTile(new Vector3Int(x, cellY, 0), tile);
                    }
                }
            }
        }

        private TileBase GetOrCreateTile(int index)
        {
            if (_tileCache.TryGetValue(index, out var cached))
            {
                return cached;
            }
            var tile = palette.CreateTile(index);
            _tileCache[index] = tile;
            return tile;
        }
    }
}
