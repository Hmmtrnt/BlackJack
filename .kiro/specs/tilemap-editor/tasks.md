# Implementation Plan: TileMap Editor

## Overview

本実装計画は、確定済みの `requirements.md` と `design.md` に基づく、Unity 向けタイルマップエディタ拡張の実装タスクを定義する。実装言語は C#（Unity 6000.3.19f1）。

実装順序の原則:

1. **アセンブリ基盤**: Runtime / Editor の asmdef を独立フォルダに作成（要件 8）
2. **Runtime データ**: TileMapData / TileLayer / TilePalette（要件 1, 2, 7）
3. **Runtime 描画**: TileMapLoader（要件 6）
4. **Editor UI**: TileMapEditorWindow / NewMapPopup（要件 2, 3, 4, 5）
5. **動作確認とドキュメント**

`[x]` は実装済み、`[ ]` は未着手を表す。`*` を付したタスクは任意（MVP では省略可能）。

## Tasks

- [x] 1. アセンブリ定義と独立フォルダを整備する
  - `Assets/TileMapEditor/Runtime/BJ.TileMap.Runtime.asmdef`（rootNamespace: BJ.TileMap）を作成する
  - `Assets/TileMapEditor/Editor/BJ.TileMap.Editor.asmdef`（Editor 限定、Runtime 参照）を作成する
  - 既存 BJ.* アセンブリへ依存しないことを確認する
  - _Requirements: 8.1, 8.2, 8.3_

- [x] 2. Runtime データ構造を実装する
  - `TileMapData`（width/height/tileSize/paletteName/layers、EmptyTile 定数、CreateEmpty）を実装する
  - `TileLayer`（name/collidable/tiles、GetTile/SetTile、CreateEmpty）を実装する
  - row-major（index = y*width+x）と範囲外の扱いを実装する
  - _Requirements: 2.1, 2.2, 2.3, 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 3. TilePalette（ScriptableObject）を実装する
  - CreateAssetMenu「BJ/TileMap/Tile Palette」を実装する
  - Entry（displayName/sprite）リストと登録順インデックスを実装する
  - GetSprite（範囲外 null）、CreateTile（スプライト→TileBase）を実装する
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 4. TileMapLoader（MonoBehaviour）を実装する
  - TextAsset / 絶対パスの入力ソースと Load/LoadFromPath を実装する
  - Render で非 EmptyTile セルをパレット解決して Tilemap に設定する
  - TileBase のインデックスキャッシュを実装する
  - Y 軸反転（cellY = height-1-y）を実装する
  - targetTilemap/palette 未割り当て時のエラーログと中断を実装する
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 5. TileMapEditorWindow（EditorWindow）を実装する
  - [x] 5.1 ウィンドウ起動とツールバーを実装する
    - メニュー「Window/BJ/TileMap Editor」で起動する
    - 新規マップ / 読み込み / 保存 / 名前を付けて保存 と dirty 表示を実装する
    - _Requirements: 3.5, 4.1, 4.2, 5.1_

  - [x] 5.2 グリッド描画と編集入力を実装する
    - セル描画・グリッド線・ズームを実装する
    - 左ドラッグ配置 / 右ドラッグ削除 / 消しゴム / 範囲外無視を実装する
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

  - [x] 5.3 サイドパネル（パレット選択）を実装する
    - TilePalette 割り当て、消しゴムトグル、タイル選択グリッドを実装する
    - _Requirements: 1.2, 3.3_

  - [x] 5.4 新規作成ポップアップ（NewMapPopup）を実装する
    - 幅/高さ/タイルサイズ入力と 1 以上補正を実装する
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 5.5 保存・読み込みと破棄確認を実装する
    - JSON 保存、Assets 配下は AssetDatabase.Refresh、dirty 解除を実装する
    - 読み込み・無効ファイルのエラーダイアログ・破棄確認を実装する
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3_

- [x] 6. 動作確認ドキュメントを作成する
  - `kirodata/TileMapEditor_操作マニュアル.md` にセットアップ〜確認手順を記載する
  - _Requirements: 全要件の確認手段_

- [ ] 7. Unity エディタ上でコンパイルと手動動作確認を行う
  - Unity でプロジェクトを開き、コンソールにコンパイルエラーが無いことを確認する
  - マニュアル手順でパレット作成 → マップ編集 → 保存 → ローダー描画まで一通り確認する
  - 不具合があれば記録し修正する
  - _Requirements: 1〜8（統合確認）_

- [ ]* 8. Runtime ロジックの自動テストを追加する
  - [ ]* 8.1 TileLayer.GetTile/SetTile の境界ユニットテスト
    - _Requirements: 3.4, 1.4, 7.2_
  - [ ]* 8.2 TileMapData.CreateEmpty のサイズ補正ユニットテスト
    - _Requirements: 2.2_
  - [ ]* 8.3 JSON ラウンドトリップ（ToJson→FromJson 等価）テスト
    - _Requirements: 7.5_

## 将来拡張（本スコープ外）

- [ ] 9. 当たり判定サポート（TilemapCollider2D + collidable 反映）
- [ ] 10. 複数レイヤー編集 UI
- [ ] 11. 矩形塗りつぶし・範囲選択・バケツ塗り
- [ ] 12. アンドゥ / リドゥ
