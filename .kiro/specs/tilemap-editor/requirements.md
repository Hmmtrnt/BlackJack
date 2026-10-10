# Requirements Document

## Introduction

本ドキュメントは、Unity（2D）向けの **タイルマップエディタ拡張** の要件を定義する。目的は、タイル（スプライト）を正方形グリッド上に並べて外部ファイル（JSON）として保存し、そのファイルをゲーム実行時に Unity 標準の Tilemap として読み込み・描画できるようにすることである。

本機能は BlackJack プロジェクトのゲームロジック（Domain/Data/Coordination/Presentation の各レイヤ）とは独立した開発支援ツールであり、既存のアセンブリ構成に影響を与えない独立フォルダ `Assets/TileMapEditor/` に実装する。

MVP（今回のスコープ）は「正方形グリッドにタイルを並べて保存・読込・描画する」ことに限定する。当たり判定や複数レイヤー編集 UI などは将来拡張として明記する。

> **スコープ外（将来拡張）:**
> - 当たり判定（TilemapCollider2D 連携、レイヤーごとの collidable 反映）
> - 複数レイヤーの編集 UI
> - 矩形塗りつぶし・範囲選択・バケツ塗り
> - アンドゥ / リドゥ

対象 Unity バージョン: 6000.3.19f1

## Glossary

- **TileMapEditor**: 本機能全体。タイルを並べて JSON を作る Editor 拡張と、JSON を描画する Runtime で構成される。
- **TileMapEditorWindow**: タイルを並べて編集・保存・読込する Unity の EditorWindow。
- **TilePalette**: 配置に使うスプライトの集合を定義する ScriptableObject。各エントリは 0 始まりのインデックスで参照される。
- **TileIndex**: TilePalette 内のエントリを指す整数。`-1` は空セル（EmptyTile）を表す。
- **TileMapData**: マップ全体を表すデータ構造。width / height / tileSize / layers を持ち、JSON とシリアライズ可能。
- **TileLayer**: 1 枚のタイルレイヤー。row-major の 1 次元 TileIndex 配列（`tiles`）を持つ。
- **TileMapLoader**: JSON を読み込み、TilePalette を介して Unity 標準 Tilemap に描画する MonoBehaviour。
- **Tilemap**: Unity 標準のタイルマップコンポーネント（`UnityEngine.Tilemaps.Tilemap`）。描画先。
- **JSON ファイル**: TileMapData をシリアライズした外部ファイル（`.json`）。エディタの出力かつローダーの入力。

## Requirements

### Requirement 1: パレット定義

**User Story:** ゲーム開発者として、配置に使うタイル（スプライト）の集合を定義したい。そうすれば、エディタ上でタイルを選んで並べられる。

#### Acceptance Criteria

1. WHEN 開発者が Project メニューから「BJ > TileMap > Tile Palette」を選択する THEN TileMapEditor SHALL 新しい TilePalette アセットを作成する
2. WHERE TilePalette のインスペクタ THE TileMapEditor SHALL スプライトのエントリ一覧を編集可能にする
3. WHEN TilePalette に N 個のエントリが登録されている THEN TileMapEditor SHALL 各エントリに 0 から N-1 の TileIndex を割り当てる
4. IF 指定された TileIndex が範囲外である THEN TilePalette SHALL スプライトとして null を返す

### Requirement 2: マップの新規作成

**User Story:** ゲーム開発者として、幅・高さを指定して空のマップを作りたい。そうすれば、任意サイズのレベルを編集できる。

#### Acceptance Criteria

1. WHEN 開発者が「新規マップ」で幅・高さ・タイルサイズを入力して作成する THEN TileMapEditorWindow SHALL 指定サイズの空マップ（全セル EmptyTile）を生成する
2. IF 入力された幅または高さが 1 未満である THEN TileMapEditorWindow SHALL 値を 1 以上に補正する
3. WHEN 新規マップが作成される THEN TileMapEditorWindow SHALL 既定で 1 枚のレイヤー（名称 "Ground"）を含める

### Requirement 3: タイルの配置と削除

**User Story:** ゲーム開発者として、グリッド上をマウス操作してタイルを配置・削除したい。そうすれば、直感的にレベルを描ける。

#### Acceptance Criteria

1. WHEN 開発者がパレットからタイルを選択し、グリッドのセルを左ドラッグする THEN TileMapEditorWindow SHALL そのセルの TileIndex を選択中のタイルに設定する
2. WHEN 開発者がグリッドのセルを右ドラッグする THEN TileMapEditorWindow SHALL そのセルを EmptyTile に設定する
3. WHILE 消しゴムツールが選択されている WHEN 開発者がセルを左ドラッグする THEN TileMapEditorWindow SHALL そのセルを EmptyTile に設定する
4. IF ドラッグ位置がグリッド範囲外である THEN TileMapEditorWindow SHALL そのセルを変更しない
5. WHEN セルの内容が変化した THEN TileMapEditorWindow SHALL 未保存状態（dirty）であることを表示する

### Requirement 4: 保存

**User Story:** ゲーム開発者として、編集したマップを外部ファイルとして保存したい。そうすれば、後で読み込んで再編集・利用できる。

#### Acceptance Criteria

1. WHEN 開発者が「保存」を実行する THEN TileMapEditorWindow SHALL TileMapData を JSON 形式でファイルに書き出す
2. IF 保存先がまだ決まっていない THEN TileMapEditorWindow SHALL 保存先選択ダイアログを表示する
3. WHEN 保存先が Assets フォルダ配下である THEN TileMapEditorWindow SHALL AssetDatabase を更新してプロジェクトに取り込む
4. WHEN 保存が完了する THEN TileMapEditorWindow SHALL 未保存状態（dirty）を解除する

### Requirement 5: 読み込み

**User Story:** ゲーム開発者として、保存済みの JSON を読み込んで続きを編集したい。

#### Acceptance Criteria

1. WHEN 開発者が「読み込み」でファイルを選択する THEN TileMapEditorWindow SHALL JSON を TileMapData にデシリアライズして編集状態へ復元する
2. IF 選択されたファイルが有効な TileMapData でない THEN TileMapEditorWindow SHALL エラーダイアログを表示し、現在の編集状態を破壊しない
3. IF 現在の編集に未保存の変更がある状態で新規作成または読み込みが要求される THEN TileMapEditorWindow SHALL 破棄確認ダイアログを表示する

### Requirement 6: ゲーム内での描画

**User Story:** ゲーム開発者として、保存した JSON を実行時に Unity の Tilemap として表示したい。そうすれば、作ったマップをゲームで利用できる。

#### Acceptance Criteria

1. WHEN TileMapLoader が有効化され loadOnStart が真である THEN TileMapLoader SHALL 割り当てられた入力ソースから JSON を読み込み、描画を実行する
2. WHEN TileMapLoader が TileMapData を描画する THEN TileMapLoader SHALL 各非 EmptyTile セルについて TilePalette からスプライトを解決し、対応する Tilemap セルにタイルを設定する
3. WHEN 同一の TileIndex を複数セルで使用する THEN TileMapLoader SHALL 生成した TileBase をキャッシュして再利用する
4. WHEN JSON の座標系（左上原点）を Tilemap（下方向 +Y）へ変換する THEN TileMapLoader SHALL Y 軸を反転させて配置する
5. IF targetTilemap または palette が未割り当てである THEN TileMapLoader SHALL エラーをログ出力し、描画を行わない

### Requirement 7: データ形式と拡張性

**User Story:** ゲーム開発者として、保存形式が人間にも読め、将来の機能追加に耐える構造であってほしい。

#### Acceptance Criteria

1. THE TileMapData SHALL width / height / tileSize / paletteName / layers を保持する
2. THE TileLayer SHALL row-major（index = y * width + x）の 1 次元 TileIndex 配列として tiles を保持する
3. THE TileMapData SHALL layers を配列（リスト）として保持し、将来の複数レイヤー追加を可能にする
4. THE TileLayer SHALL 将来の当たり判定のための collidable フラグを保持する（MVP では未使用）
5. THE JSON ファイル SHALL JsonUtility で読み書き可能な構造である

### Requirement 8: 既存プロジェクトからの独立

**User Story:** ゲーム開発者として、このツールが既存の BlackJack 本体コードに影響しないでほしい。

#### Acceptance Criteria

1. THE TileMapEditor SHALL 独立フォルダ `Assets/TileMapEditor/` に配置される
2. THE TileMapEditor SHALL 独自のアセンブリ定義（Runtime / Editor）を持ち、既存の BJ.* アセンブリへ依存しない
3. THE Editor 用アセンブリ SHALL Editor プラットフォーム限定とし、ビルド成果物に含まれない
