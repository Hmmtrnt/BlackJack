# 引き継ぎ資料: TileMap Editor 開発

このファイルは、別の Kiro セッション（または別の担当者）が本作業の現状を把握し、
続きを進められるようにするための引き継ぎ用ドキュメントです。

- 作成日時: 2026-10-11
- 対象プロジェクト: `C:\Users\rento\Documents\UnityFile\BJ\BlackJack`
- Unity バージョン: 6000.3.19f1（2D / URP）
- 関連ドキュメント:
  - 操作マニュアル: `C:\Users\rento\Documents\UnityFile\BJ\kirodata\TileMapEditor_操作マニュアル.md`
  - Kiro spec: `C:\Users\rento\Documents\UnityFile\BJ\.kiro\specs\tilemap-editor\`（requirements / design / tasks）

---

## 1. このタスクのゴール

Unity で 2D ゲームを作るにあたり、「マップチップ（タイル）を正方形グリッドに並べて
外部ファイル（JSON）として保存し、それをゲーム内で Unity 標準 Tilemap として
読み込み描画する」ための **自作タイルマップエディタ** を Unity Editor 拡張として作る。

- ユーザーの当初案: C++ で exe を自作 → Unity Editor 拡張で実現できるならそれで、という方針になった
- MVP: 「正方形グリッドに並べる」ことに限定
- 将来: 当たり判定などのオプションを後付けする想定（データ構造に拡張余地を用意済み）

---

## 2. 現在の状態（サマリ）

- 実装: **完了**（Runtime 3 ファイル + Editor 1 ファイル + asmdef 2 つ）
- ドキュメント: 操作マニュアルと Kiro spec（requirements/design/tasks）作成済み
- 動作確認: **未実施**（ユーザーが後日 Unity で確認予定）
- 自動コンパイル検証: この環境ではバッチ起動が失敗したため未実施。
  ただし Unity 側で `.meta` が生成済み＝一度インポートはされている（下記 5 参照）

---

## 3. 実装したファイル一覧

すべて `C:\Users\rento\Documents\UnityFile\BJ\BlackJack\Assets\TileMapEditor\` 配下。
既存の BlackJack 本体（BJ.Domain / BJ.Data / BJ.Coordination / BJ.Presentation）とは
**独立**しており、既存コードには一切手を加えていない。

```
Assets/TileMapEditor/
├── Runtime/
│   ├── BJ.TileMap.Runtime.asmdef   … rootNamespace: BJ.TileMap。既存 BJ.* に非依存
│   ├── TileMapData.cs              … TileMapData / TileLayer（JSON データ構造）
│   ├── TilePalette.cs              … TilePalette（ScriptableObject。スプライト集合）
│   └── TileMapLoader.cs            … TileMapLoader（MonoBehaviour。JSON→Tilemap 描画）
└── Editor/
    ├── BJ.TileMap.Editor.asmdef    … Editor 限定。Runtime を参照
    └── TileMapEditorWindow.cs      … TileMapEditorWindow / NewMapPopup（編集 UI）
```

### 各ファイルの役割

- **TileMapData.cs**
  - `TileMapData`: width / height / tileSize / paletteName / `List<TileLayer> layers`。
    定数 `EmptyTile = -1`、`CreateEmpty(w,h,ts)`。
  - `TileLayer`: name / collidable（将来の当たり判定用・現状未使用）/ `int[] tiles`（row-major, index=y*width+x）。
    `GetTile` / `SetTile`（範囲外は無視）。
- **TilePalette.cs**
  - `[CreateAssetMenu]`「BJ/TileMap/Tile Palette」。
  - `Entry { displayName, sprite }` のリスト。登録順が 0 始まりのタイルインデックス。
  - `GetSprite(index)`（範囲外 null）、`CreateTile(index)`（Sprite→`Tile`/TileBase 生成）。
- **TileMapLoader.cs**
  - 入力: `TextAsset jsonFile`（優先）/ `string externalJsonPath`。`loadOnStart`。
  - `Load()` / `LoadFromPath()` / `Render(TileMapData)`。
  - TileBase を `Dictionary<int,TileBase>` でキャッシュ。
  - 座標変換: JSON は左上原点、Tilemap は左下原点 +Y のため `cellY = height-1-y` で Y 反転。
  - targetTilemap / palette 未割り当て時は Debug.LogError で中断。
- **TileMapEditorWindow.cs**
  - メニュー `Window/BJ/TileMap Editor`。
  - ツールバー: 新規マップ / 読み込み / 保存 / 名前を付けて保存、dirty（*）表示。
  - グリッド: セル描画・グリッド線・ズームスライダー。左ドラッグ配置 / 右ドラッグ削除 / 消しゴム。
  - サイドパネル: TilePalette 割り当て、タイル選択、消しゴムトグル。
  - `NewMapPopup`: 幅/高さ/タイルサイズ入力（1 未満は 1 に補正）。
  - 保存は `JsonUtility.ToJson`、Assets 配下なら `AssetDatabase.Refresh()`。

---

## 4. データ形式（JSON）

```json
{
  "width": 10,
  "height": 8,
  "tileSize": 16,
  "paletteName": "DefaultPalette",
  "layers": [
    { "name": "Ground", "collidable": false, "tiles": [ -1, 0, 0, 1, -1 ] }
  ]
}
```

- `tiles` は長さ `width*height` の 1 次元配列（左上原点・行優先）。値はパレットのインデックス。`-1` は空。
- `JsonUtility` でシリアライズ可能な構造（public フィールド + `[Serializable]`）。

---

## 5. 検証状況と既知の注意点

- **自動コンパイル検証は未実施。** この作業環境から Unity をバッチモード
  （`-batchmode -quit -nographics`）で起動しようとしたが、ログも生成されず失敗した
  （ライセンス/Hub 依存の可能性）。そのため最終的なコンパイル確認は Unity エディタ上で行う必要がある。
- `Assets/TileMapEditor/` 配下に `.meta` が生成済み。これは過去に一度 Unity が
  プロジェクトを開いてインポートした痕跡。コンパイルエラーの有無までは未確認。
- API はすべて Unity 6000.3 で安定しているもの（EditorWindow / IMGUI / Tilemap /
  Tile / JsonUtility / AssetDatabase）を使用。ロジックはレビュー済みだが実機未検証。

---

## 6. 次にやること（優先順）

1. **Unity でコンパイル確認**（最優先）
   - BlackJack プロジェクトを開き、Console にエラーが無いことを確認。
   - エラーがあればメッセージ全文を控えて修正する。
2. **手動動作確認**
   - 操作マニュアルの手順、または spec の tasks.md「タスク 7」に沿って、
     パレット作成 → マップ編集 → 保存 → 読込 → ゲーム内描画まで一通り試す。
   - 確認チェックリストはこのファイルの「8. 動作確認チェックリスト」を参照。
3. **タイルセット画像のスライス対応の案内 / 機能追加**
   - ユーザーのマップチップは「1 枚の画像に複数チップが並ぶタイルセット形式」。
   - 現状の TilePalette は「スライス済みの個別スプライトを 1 つずつ登録」する設計。
   - 当面はタイルセットを Sprite Mode: Multiple でスライスし、各スプライトを登録すれば動く。
   - チップ数が多い場合に備え、「タイルセット画像を指定すると
     スライス済みスプライトを自動一括登録する」機能を TilePalette / Editor に
     追加するのが望ましい（`AssetDatabase.LoadAllAssetsAtPath` で子スプライトを取得可能）。
     ※ ユーザーに提案済み・未着手。

### スライスに必要な未確認情報（ユーザーに要確認）

- 1 チップのピクセルサイズ（例: 16×16 / 32×32）
- チップ間の隙間（余白・区切り線）の有無
- タイルセット内のチップ種類数（登録 UI 設計の判断材料）

---

## 7. 将来拡張（本スコープ外・spec の tasks.md 9〜12）

- 当たり判定（`TileLayer.collidable` を Tilemap の `TilemapCollider2D` に反映）
- 複数レイヤー編集 UI（`layers` は既に配列化済み）
- 矩形塗りつぶし・範囲選択・バケツ塗り
- アンドゥ / リドゥ

---

## 8. 動作確認チェックリスト

Unity で確認する際の項目。カッコ内は requirements.md の要件番号。

コンパイル・基盤
- [ ] Console にコンパイルエラー・例外が出ない（要件 8）
- [ ] `Assets/TileMapEditor/` 配下だけで完結し既存コードに影響なし（8.1, 8.2）

パレット（要件 1）
- [ ] Create > BJ > TileMap > Tile Palette でアセット作成（1.1）
- [ ] Entries にスプライト登録・編集（1.2）
- [ ] 登録順に 0,1,2… のインデックス（1.3）

新規作成（要件 2）
- [ ] 幅・高さ・タイルサイズ指定で空マップ作成（2.1）
- [ ] 0 や負数を入れても 1 以上に補正（2.2）
- [ ] 既定でレイヤー "Ground" が 1 枚（2.3）

配置・削除（要件 3）
- [ ] タイル選択 + 左ドラッグで配置（3.1）
- [ ] 右ドラッグで削除（3.2）
- [ ] 消しゴム + 左ドラッグで削除（3.3）
- [ ] 範囲外ドラッグで無反応（3.4）
- [ ] 編集するとタイトルに未保存マーク `*`（3.5）

保存（要件 4）
- [ ] 「保存」で JSON 書き出し（4.1）
- [ ] 保存先未定なら保存ダイアログ（4.2）
- [ ] Assets 配下保存で TextAsset 化（4.3）
- [ ] 保存後に `*` が消える（4.4）

読み込み（要件 5）
- [ ] 保存 JSON の読込で内容復元（5.1）
- [ ] 壊れた JSON でエラーダイアログ・編集非破壊（5.2）
- [ ] 未保存状態での新規/読込で破棄確認（5.3）

ゲーム内描画（要件 6）
- [ ] 再生時に JSON から自動描画（6.1）
- [ ] タイルがパレットのスプライトで表示（6.2）
- [ ] 同一タイル大量配置でも正常（キャッシュ）（6.3）
- [ ] エディタとゲーム画面の上下が一致（Y 反転）（6.4）
- [ ] 必須参照未割り当てでエラーログ・非描画（6.5）

データ形式（要件 7）
- [ ] JSON に width/height/tileSize/paletteName/layers がある（7.1）
- [ ] tiles が長さ width×height の 1 次元配列、空セルは -1（7.2, 7.4）

---

## 9. 不具合報告時に添えてほしい情報（ユーザー向け）

- どの手順・どのチェック項目で起きたか
- Console のエラーメッセージ全文（赤いログ）
- 可能ならスクリーンショット（エディタ or ゲーム画面）

---

## 10. 会話の経緯（要約）

1. ユーザー: Unity で 2D ゲームを作りたい。チップを並べた外部ファイルを読み込んで作りたい。
   それ用のファイルを作れるソフトを作れるか？
2. 方針確定: 自作エディタを Unity Editor 拡張で作る。まずは正方形グリッドのみ。
   当たり判定などは後付け。Unity 6000.3.19f1。描画は Unity 標準 Tilemap を採用
   （将来の当たり判定が楽なため）。
3. 実装: `Assets/TileMapEditor/` に Runtime + Editor を作成（既存コードに非干渉）。
4. ドキュメント: `kirodata/TileMapEditor_操作マニュアル.md` と
   `.kiro/specs/tilemap-editor/`（requirements/design/tasks/.config.kiro）を作成。
5. 確認手順・チェックリストを提示。
6. マップチップは「1 枚にまとまったタイルセット画像」を想定していると判明
   → スライス（Sprite Mode: Multiple）が必要。自動一括登録機能を提案（未着手）。
7. 本日は確認が難しいため、この引き継ぎファイルを作成（現在地点）。
