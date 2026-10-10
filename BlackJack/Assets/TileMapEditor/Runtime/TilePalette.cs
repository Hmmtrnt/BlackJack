using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BJ.TileMap
{
    /// <summary>
    /// エディタで配置に使うタイルの集合を定義する ScriptableObject。
    /// インデックス(0..n-1)で各エントリを参照する。
    /// JSON には「どのインデックスを置いたか」だけが保存され、
    /// 実際の見た目(Sprite/Tile)はこのパレットが解決する。
    ///
    /// 作成方法: Project ウィンドウで右クリック
    ///   → Create > BJ > TileMap > Tile Palette
    /// </summary>
    [CreateAssetMenu(fileName = "TilePalette", menuName = "BJ/TileMap/Tile Palette")]
    public class TilePalette : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("パレット上の表示名(任意)")]
            public string displayName;

            [Tooltip("配置に使うスプライト")]
            public Sprite sprite;
        }

        [SerializeField]
        private string paletteName = "DefaultPalette";

        [SerializeField]
        private List<Entry> entries = new List<Entry>();

        public string PaletteName => paletteName;
        public IReadOnlyList<Entry> Entries => entries;
        public int Count => entries.Count;

        public Sprite GetSprite(int index)
        {
            if (index < 0 || index >= entries.Count)
            {
                return null;
            }
            return entries[index].sprite;
        }

        /// <summary>
        /// Sprite から Tilemap 用の TileBase を生成する。
        /// 毎回生成すると無駄なので、呼び出し側でキャッシュして使う想定。
        /// </summary>
        public TileBase CreateTile(int index)
        {
            var sprite = GetSprite(index);
            if (sprite == null)
            {
                return null;
            }
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Sprite;
            return tile;
        }
    }
}
