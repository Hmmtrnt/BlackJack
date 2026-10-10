using System;
using System.Collections.Generic;
using UnityEngine;

namespace BJ.TileMap
{
    /// <summary>
    /// タイルマップ全体を表すデータ。
    /// 外部ファイル(JSON)との読み書き対象。
    /// Unity の JsonUtility でシリアライズできるよう、
    /// すべて public フィールド + [Serializable] で構成する。
    /// </summary>
    [Serializable]
    public class TileMapData
    {
        /// <summary>マップの横のタイル数。</summary>
        public int width = 10;

        /// <summary>マップの縦のタイル数。</summary>
        public int height = 10;

        /// <summary>1タイルのピクセルサイズ(情報として保持。描画は Grid の cellSize に従う)。</summary>
        public int tileSize = 16;

        /// <summary>このマップが参照するパレットの識別名(任意)。</summary>
        public string paletteName = "";

        /// <summary>レイヤー一覧。今回は 1 レイヤー想定だが、将来の拡張のため配列で持つ。</summary>
        public List<TileLayer> layers = new List<TileLayer>();

        /// <summary>空セルを表すタイルインデックス。</summary>
        public const int EmptyTile = -1;

        /// <summary>指定サイズ・1レイヤーの空マップを生成する。</summary>
        public static TileMapData CreateEmpty(int width, int height, int tileSize)
        {
            var data = new TileMapData
            {
                width = Mathf.Max(1, width),
                height = Mathf.Max(1, height),
                tileSize = Mathf.Max(1, tileSize)
            };
            data.layers.Add(TileLayer.CreateEmpty("Ground", data.width, data.height));
            return data;
        }
    }

    /// <summary>
    /// 1 枚のタイルレイヤー。
    /// tiles は width*height 個の 1 次元配列(row-major: index = y * width + x)。
    /// 値はパレット内のタイルインデックス。EmptyTile(-1) は空セル。
    /// </summary>
    [Serializable]
    public class TileLayer
    {
        public string name = "Layer";

        /// <summary>当たり判定を持つレイヤーか(今回は未使用。将来の拡張用)。</summary>
        public bool collidable = false;

        /// <summary>row-major の 1 次元タイル配列。</summary>
        public int[] tiles = Array.Empty<int>();

        public static TileLayer CreateEmpty(string name, int width, int height)
        {
            var layer = new TileLayer { name = name };
            layer.tiles = new int[width * height];
            for (int i = 0; i < layer.tiles.Length; i++)
            {
                layer.tiles[i] = TileMapData.EmptyTile;
            }
            return layer;
        }

        /// <summary>(x, y) のタイルインデックスを取得。範囲外は EmptyTile を返す。</summary>
        public int GetTile(int x, int y, int width, int height)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return TileMapData.EmptyTile;
            }
            return tiles[y * width + x];
        }

        /// <summary>(x, y) にタイルインデックスを設定。範囲外は無視。</summary>
        public void SetTile(int x, int y, int width, int height, int value)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return;
            }
            tiles[y * width + x] = value;
        }
    }
}
