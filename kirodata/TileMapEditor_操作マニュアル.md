# タイルマップエディタ 操作マニュアル

Unity 6000.3.19f1 用。`Assets/TileMapEditor/` に追加したタイルマップエディタの
セットアップから動作確認までの手順をまとめたものです。

対象プロジェクト: `C:\Users\rento\Documents\UnityFile\BJ\BlackJack`

---

## 1. 全体像

このツールは「タイルを並べて外部ファイル(JSON)として保存するエディタ」と、
「保存したJSONをゲーム内でUnity標準の Tilemap に描画するローダー」の2つで構成されています。

```
Assets/TileMapEditor/
├── Runtime/
│   ├── TileMapData.cs    … JSONに対応するデータ構造
│   ├── TilePalette.cs    … 配置に使うスプライトの集合 (ScriptableObject)
│   └── TileMapLoader.cs  … JSONを読み込み Tilemap に描画 (MonoBehaviour)
└── Editor/
    └── TileMapEditorWindow.cs … エディタ本体 (Window > BJ > TileMap Editor)
```

処理の流れ:

1. エディタで JSON を作る（タイルのインデックスだけを保存）
2. ゲーム実行時に TileMapLoader が JSON を読む
3. パレット(TilePalette)がインデックスを実際のスプライトに変換して Tilemap に描画

---

## 2. 事前準備

### 2-1. コンパイル確認

1. Unity Hub から `BlackJack` プロジェクトを開く
2. 画面下部のコンソール (Window > General > Console) にエラーが出ていないことを確認する
   - エラーが出た場合はメッセージを控えておく

### 2-2. タイル用スプライトを用意する

動作確認だけなら、どんな画像でも構いません。手軽な方法:

- 手元の PNG を 2〜3枚、`Assets` 内の任意のフォルダ(例: `Assets/TileMapEditor/SampleTiles/`)にドラッグ＆ドロップする
- 画像を選択し、インスペクタで以下を設定して「Apply」
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Single`
  - Pixels Per Unit: タイルのピクセルサイズに合わせる（例: 16pxのタイルなら 16）

> 手持ちの画像が無い場合は、Unity の「2D Object > Sprites > Square」等で作った単色スプライトでも代用できます。

---

## 3. パレットの作成

1. Project ウィンドウで、タイルを置きたいフォルダを右クリック
2. **Create > BJ > TileMap > Tile Palette** を選択
3. 作成された `TilePalette` アセットを選択し、インスペクタで設定
   - **Palette Name**: 任意の名前（例: `DefaultPalette`）
   - **Entries**: 「+」で枠を追加し、各 `Sprite` に 2-2 で用意したスプライトを割り当てる
     - 登録した順に 0, 1, 2 … というインデックスになる
     - `Display Name` は任意（空でも可）

---

## 4. エディタでマップを作る

### 4-1. ウィンドウを開く

メニューバー **Window > BJ > TileMap Editor** を選択。

### 4-2. 操作手順

1. **Palette**（右パネル上部）に、3 で作った TilePalette を割り当てる
2. ツールバーの **新規マップ** をクリック
   - 幅・高さ（タイル数）とタイルサイズ(px)を入力して「作成」
3. 右パネルの **タイル** 一覧から置きたいタイルをクリックして選択
4. 左のグリッドを操作して配置

### 4-3. 操作一覧

| 操作           | 動作           |
|----------------|----------------|
| 左ドラッグ      | 選択中のタイルを配置 |
| 右ドラッグ      | タイルを削除       |
| 「消しゴム」選択中に左ドラッグ | タイルを削除 |
| 上部スライダー   | グリッドの表示倍率を変更 |

### 4-4. 保存

- ツールバーの **保存** / **名前を付けて保存** で `.json` を書き出す
- 保存先はどこでも良いが、ゲームから TextAsset として使う場合は
  `Assets` 内（例: `Assets/TileMapEditor/Maps/`）に保存すると取り込みが楽

---

## 5. ゲーム内で表示して確認する

### 5-1. シーンに Tilemap を用意

1. Hierarchy で右クリック → **2D Object > Tilemap > Rectangular**
   - `Grid` とその子 `Tilemap` が作成される
2. （任意）`Grid` の Cell Size を調整。初期値 1×1 で問題ない

### 5-2. ローダーを設置

1. Hierarchy で空の GameObject を作成（右クリック → Create Empty、名前は `MapLoader` など）
2. その GameObject に **TileMapLoader** コンポーネントを追加
   （Add Component → 検索欄に `TileMapLoader`）
3. インスペクタで割り当て
   - **Target Tilemap**: 5-1 で作った `Tilemap`
   - **Palette**: 3 で作った TilePalette
   - **Json File**: 4-4 で `Assets` 内に保存した `.json`（TextAsset としてドラッグ）
     - `Assets` 外に保存した場合は代わりに **External Json Path** に絶対パスを入力
   - **Load On Start**: チェックを入れる

### 5-3. 再生して確認

1. 画面上部の再生ボタン（▶）を押す
2. Game ビュー / Scene ビューにマップが描画されれば成功
   - 見えない場合は 6. のトラブルシュートを確認

---

## 6. トラブルシュート

| 症状 | 原因と対処 |
|------|-----------|
| エディタの一覧にタイルが出ない | Palette が未割り当て、または Entries が空。3 を確認 |
| グリッドに色のタイルしか出ない | Palette は割り当てたがスプライトが未設定。Entries の Sprite を確認 |
| 再生してもマップが見えない | Target Tilemap / Palette / Json File のいずれかが未割り当て。コンソールの警告を確認 |
| マップが上下逆に見える | JSONは左上原点、Tilemapは下方向が+Y。ローダーで反転済みのため通常は正しく出る。意図と逆なら相談を |
| タイルが大きすぎ/小さすぎ | スプライトの Pixels Per Unit と Grid の Cell Size の関係を調整 |
| 画面に映らない | Scene ビューでマップ位置へカメラを合わせる。Main Camera の位置/サイズ(Orthographic Size)も確認 |

---

## 7. 保存される JSON の形式（参考）

```json
{
  "width": 10,
  "height": 8,
  "tileSize": 16,
  "paletteName": "DefaultPalette",
  "layers": [
    {
      "name": "Ground",
      "collidable": false,
      "tiles": [ -1, 0, 0, 1, -1, ... ]
    }
  ]
}
```

- `tiles` は `height * width` 個の1次元配列（row-major、左上から右へ→下へ）
- 値はパレットのインデックス。`-1` は空セル
- `collidable` は将来の当たり判定用（現状は未使用）

---

## 8. 現時点でできること / 今後の予定

できること（今回）:
- 正方形グリッドへのタイル配置・削除
- JSON の保存 / 読み込み
- ゲーム内での Tilemap 描画

今後の拡張候補:
- 当たり判定（TilemapCollider2D 連携、レイヤーごとの collidable 反映）
- 複数レイヤーの編集 UI
- 矩形塗りつぶし・範囲選択・バケツ塗り
- アンドゥ / リドゥ

動作確認で気づいた点や不具合があれば、症状を教えてください。
