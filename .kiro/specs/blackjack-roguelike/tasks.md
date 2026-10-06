# Implementation Plan: Blackjack Roguelike

## Overview

本実装計画は、確定済みの `requirements.md` と `design.md` に基づき、Unity（C#）製の横スクロール 2D アクションローグライクを段階的に実装するためのコーディングタスクを定義する。

実装言語は **C#**（Unity）で確定している（設計の方針どおり、Domain ロジックは `UnityEngine` 非依存のプレーン C# として実装する）。

実装順序の原則:

1. **Domain 層優先**: Hand_Total 算出・Ace 可変・Bust 判定・Strength_Bonus 配分・Suit_Synergy・RunState・MetaProgression を、Unity 非依存のプレーン C# として先に実装し、EditMode のプロパティベーステスト（PBT）で検証可能にする。PBT ライブラリは **FsCheck または CsCheck** を採用し、各プロパティは**最低 100 反復**で実行する。
2. **データ層整備**: 52 種の `CardDefinition`、4 種の `WeaponDefinition`、各種 Config（`BalanceConfig` / `SynergyConfig` / `RunConfig`）の ScriptableObject を整備する。
3. **Presentation / Coordination 層**: `InputReader`・`PlayerCharacterController`・`WeaponController`・`EnemyController`・`StageManager` / `RunManager`・`GameHUD`・`SaveRepository` を MonoBehaviour / PlayMode テストとして実装し、Domain と配線する。

各タスクは要件番号（Requirements X.Y）と、該当する設計プロパティ（Property N）への参照を含む。`*` を付したサブタスク（ユニット／PBT／統合／PlayMode テスト）は任意であり、MVP では省略可能。

## Tasks

- [x] 1. アセンブリ定義とプロジェクト基盤を整備する
  - `Assets/Scripts/Domain/` に `UnityEngine` 非依存のアセンブリ定義（`BJ.Domain.asmdef`）を作成する
  - `Assets/Scripts/Presentation/`、`Assets/Scripts/Coordination/`、`Assets/Scripts/Data/` の各 asmdef を作成し、依存方向を Presentation → Coordination → Domain → Data の一方向のみに制限する
  - EditMode テスト用 asmdef（`BJ.Domain.Tests.asmdef`、Domain と PBT ライブラリを参照）と PlayMode テスト用 asmdef を作成する
  - PBT ライブラリ（FsCheck または CsCheck）をパッケージとして導入し、EditMode テスト asmdef から参照できるようにする
  - _Requirements: 全要件の基盤_

- [ ] 2. Domain 層の列挙型・値型・カード基盤を実装する
  - [ ] 2.1 列挙型と Card_Value テーブルを実装する
    - `Suit`（Spade/Diamond/Club/Heart）、`Rank`（Ace〜King）を定義する
    - `CardValueTable.Of(Rank)` を実装する（A=11 基準、2-10=額面、J/Q/K=10）。Card_Value の唯一の実装点とする
    - `CardInstanceId`、`CardInstance`（定義参照 + 一意 ID）を実装する
    - _Requirements: 1.1, 5.1_

  - [ ]* 2.2 Card_Value テーブルのユニットテストを書く
    - 各 Rank の Card_Value を検証（A=11, 10/J/Q/K=10, 数字は額面）
    - _Requirements: 1.1_

- [ ] 3. HandSystem の Hand_Total 算出と Ace 最適化を実装する
  - [ ] 3.1 ComputeHandTotal と Ace 最適化アルゴリズムを実装する
    - 設計の疑似コードに従い、全 Ace を 1 で算入後、Target を超えない範囲で +10 昇格して合計を最大化する
    - `HandSnapshot`、`AceAssignment`（各 Ace が 1 か 11 か）を生成する
    - 全 Ace を 1 にしても超過する場合は正当な Bust として扱う
    - _Requirements: 1.1, 5.1, 5.2, 5.3_

  - [ ] 3.2 HandSystem の手札管理（追加・除去・再計算）を実装する
    - `AddCard`（自動加入、上限なし）、`RemoveCard(id)`（指定 ID のみ除去、不在時は no-op で false）を実装する
    - 変更時に Hand_Total を再計算し、`HandChanged` / `BustStateChanged` イベントを発火する
    - 武器装備 Suit の保持（`SetEquippedWeaponSuit`）を実装するが Hand_Total には影響させない
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 2.2, 6.3_

  - [ ]* 3.3 Hand_Total 合計一致のプロパティテストを書く
    - **Property 1: Hand_Total はカード Value の合計に一致する**
    - **Validates: Requirements 1.1**

  - [ ]* 3.4 カード追加のプロパティテストを書く
    - **Property 2: カード追加は手札を 1 枚増やし合計を再計算する**
    - **Validates: Requirements 1.2, 1.5**

  - [ ]* 3.5 追加・除去 round-trip のプロパティテストを書く
    - **Property 3: 追加・除去の round-trip は状態を復元する**
    - **Validates: Requirements 1.3**

  - [ ]* 3.6 武器非干渉のプロパティテストを書く
    - **Property 4: 武器は Hand_Total に影響しない**
    - **Validates: Requirements 1.4, 6.3**

  - [ ]* 3.7 指定カード除去のプロパティテストを書く
    - **Property 5: 指定カードの除去は対象のみを取り除く**
    - **Validates: Requirements 2.2**

  - [ ]* 3.8 Ace 割り当て値域のプロパティテストを書く
    - **Property 15: Ace の割り当て値は常に 1 または 11 である**
    - **Validates: Requirements 5.1**

  - [ ]* 3.9 Ace 最大化のプロパティテストを書く
    - **Property 16: Ace は Target を超えない範囲で合計を最大化する**
    - **Validates: Requirements 5.2, 5.3**

  - [ ]* 3.10 Ace 代表例のユニットテストを書く
    - A+K=21、A+A=12、A+9+K=20 などのエッジケース
    - _Requirements: 5.2, 5.3_

- [ ] 4. Bust 判定と Bust_State ロジックを実装する
  - [ ] 4.1 Bust 判定を実装する
    - `IsBust == (HandTotal > TargetValue)` を HandSystem に実装する
    - Bust 突入・解除時に `BustStateChanged` を発火する
    - _Requirements: 4.1, 4.2_

  - [ ]* 4.2 Bust 判定のプロパティテストを書く
    - **Property 12: Bust 判定は合計が Target を超えるとき、かつそのときのみ真である**
    - **Validates: Requirements 4.1**

  - [ ]* 4.3 Bust 非即死のプロパティテストを書く
    - **Property 13: Bust_State への遷移は即死を引き起こさない**
    - **Validates: Requirements 4.2**

- [ ] 5. StrengthCalculator（強化値算出と配分）を実装する
  - [ ] 5.1 Strength_Bonus 算出と配分を実装する
    - `ComputeStrengthBonus`（0〜Target で比例・単調増加、一致時に Max、Bust 中は 0）を実装する
    - `Distribute`（60/20/20 比率、`StatBreakdown` へ）を実装する
    - _Requirements: 3.2, 3.3, 3.4_

  - [ ] 5.2 Blackjack ブーストと Bust モディファイアを実装する
    - `ApplyBlackjackBoost`（一致時に攻撃力を `round(base × (1+rate))`）を実装する
    - `ApplyBustModifier`（攻撃力強化 ≥1 倍かつ被ダメージ倍率 >1 を同時付与）を実装する
    - _Requirements: 3.6, 4.3_

  - [ ]* 5.3 Strength_Bonus 単調性のプロパティテストを書く
    - **Property 8: Strength_Bonus は Target 以下の範囲で単調非減少である**
    - **Validates: Requirements 3.2**

  - [ ]* 5.4 Strength_Bonus 最大化のプロパティテストを書く
    - **Property 9: Hand_Total が Target と一致すると Strength_Bonus は最大になる**
    - **Validates: Requirements 3.3**

  - [ ]* 5.5 配分比例・総量保存のプロパティテストを書く
    - **Property 10: Strength_Bonus の配分は比率に比例し総量を保存する**
    - **Validates: Requirements 3.4**

  - [ ]* 5.6 Blackjack ブーストのプロパティテストを書く
    - **Property 11: Blackjack 状態は一致時に有効化され攻撃を倍率ブーストする**
    - **Validates: Requirements 3.6**

  - [ ]* 5.7 Bust モディファイア同時付与のプロパティテストを書く
    - **Property 14: Bust_State は強化とデメリットを同時に付与する**
    - **Validates: Requirements 4.3**

- [ ] 6. Bust 解除と強化再開ロジックを HandSystem に統合する
  - [ ] 6.1 Card_Removal による Bust 解除と Strength_Bonus 再開を実装する
    - 除去により Hand_Total が Target 以下へ戻ったら Bust_State を解除し、比例算出を再開する
    - _Requirements: 2.5, 4.5_

  - [ ]* 6.2 Bust 解除のプロパティテストを書く
    - **Property 7: 合計を Target 以下へ戻すと Bust_State は解除される**
    - **Validates: Requirements 2.5, 4.5**

- [ ] 7. SynergyCalculator（武器×カード シナジー）を実装する
  - [ ] 7.1 Suit_Synergy 算出を実装する
    - 一致 Suit 枚数から `active`（≥1）と `multiplier`（枚数比例）を算出する
    - 武器 Suit 変更・手札変化で再算出する `EvaluateSynergy` を HandSystem に配線する
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

  - [ ]* 7.2 Synergy 有効条件のプロパティテストを書く
    - **Property 17: Suit_Synergy は一致カードが 1 枚以上あるとき、かつそのときのみ有効である**
    - **Validates: Requirements 7.1**

  - [ ]* 7.3 Synergy 単調性のプロパティテストを書く
    - **Property 18: Suit_Synergy の強度は一致枚数について単調非減少である**
    - **Validates: Requirements 7.2, 7.3, 7.4**

- [ ] 8. チェックポイント - Domain の Hand/Strength/Bust/Synergy をまとめて検証する
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. ダメージ計算と操作可否ロジックを実装する
  - [ ] 9.1 HP・ダメージ・操作受付ロジックを Domain に実装する
    - `ApplyDamage`（`max(0, hp − round(damage × multiplier))`）、敵への `max(0, enemyHp − attack)` を実装する
    - `CanAcceptInput == (HP > 0)` を実装する
    - _Requirements: 8.5, 8.6, 8.8_

  - [ ]* 9.2 ダメージ下限のプロパティテストを書く
    - **Property 19: ダメージ適用は HP を 0 未満にしない**
    - **Validates: Requirements 8.5, 8.6**

  - [ ]* 9.3 操作受付可否のプロパティテストを書く
    - **Property 20: 操作受付可否は HP が正であることと一致する**
    - **Validates: Requirements 8.8**

- [ ] 10. RunState と Run 進行ロジックを実装する
  - [ ] 10.1 RunState と Chip・除去コスト処理を実装する
    - `RunState`（HP、Hand 参照、Weapon、Chips、StageIndex、Result、一時リソース集合）を実装する
    - Card_Removal コスト消費（残高十分時のみ成立、不足時は拒否して状態不変）を実装する
    - _Requirements: 2.3, 2.4_

  - [ ] 10.2 Run 開始・死亡・クリア・リソース破棄を実装する
    - `StartRun`（HP=StartingHp、Hand 空、Weapon=StartingWeapon、Chips=0、アンロック反映）
    - HP≤0 で Dead、最終 Stage クリアで Cleared、死亡時に一時リソース破棄
    - _Requirements: 10.1, 10.2, 10.3_

  - [ ] 10.3 Stage クリア・Run クリア判定を実装する
    - `IsStageCleared == (全敵 HP ≤ 0)`、最終 Stage クリア時のみ Cleared、それ以前は InProgress
    - _Requirements: 9.3, 9.5_

  - [ ] 10.4 Meta_Currency 算出を実装する
    - Run 成果（到達度）に応じた非負・単調非減少の Meta_Currency 算出を実装する
    - _Requirements: 10.4_

  - [ ]* 10.5 除去コストのプロパティテストを書く
    - **Property 6: 除去コストは残高が十分なときのみ消費される**
    - **Validates: Requirements 2.3, 2.4**

  - [ ]* 10.6 Stage クリア判定のプロパティテストを書く
    - **Property 21: Stage クリア判定は全敵撃破と一致する**
    - **Validates: Requirements 9.3**

  - [ ]* 10.7 Run クリア判定のプロパティテストを書く
    - **Property 22: Run クリアは最終 Stage 到達時のみ成立する**
    - **Validates: Requirements 9.5**

  - [ ]* 10.8 Run 開始リセットのプロパティテストを書く
    - **Property 23: Run 開始は状態を config の初期値へリセットする**
    - **Validates: Requirements 10.1**

  - [ ]* 10.9 死亡終了のプロパティテストを書く
    - **Property 24: HP が 0 以下になると Run は死亡終了する**
    - **Validates: Requirements 10.2**

  - [ ]* 10.10 リソース破棄のプロパティテストを書く
    - **Property 25: 死亡終了は Run 中の一時リソースを破棄する**
    - **Validates: Requirements 10.3**

  - [ ]* 10.11 Meta_Currency 算出のプロパティテストを書く
    - **Property 26: 付与される Meta_Currency は成果について単調非減少かつ非負である**
    - **Validates: Requirements 10.4**

- [ ] 11. MetaProgressionState とアンロックロジックを実装する
  - [ ] 11.1 MetaProgressionState とアンロック購入を実装する
    - `MetaCurrency`、`UnlockedIds`、`SchemaVersion` を実装する
    - アンロック購入（残高十分時のみ成立・減算・集合追加、不足時は拒否して不変）を実装する
    - アンロック効果の Run 開始時反映ロジックを実装する
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [ ] 11.2 SaveData シリアライズ構造と Checksum を実装する
    - `SaveData`（SchemaVersion、Meta、Checksum）と round-trip 用シリアライズ／デシリアライズを実装する
    - _Requirements: 11.1_

  - [ ]* 11.3 Meta round-trip のプロパティテストを書く
    - **Property 27: Meta_Progression の保存・読込は round-trip で等価である**
    - **Validates: Requirements 11.1**

  - [ ]* 11.4 アンロック購入のプロパティテストを書く
    - **Property 28: アンロック購入は残高が十分なときのみ成立する**
    - **Validates: Requirements 11.2, 11.4**

  - [ ]* 11.5 アンロック反映のプロパティテストを書く
    - **Property 29: アンロック済み効果は Run 開始時に反映される**
    - **Validates: Requirements 11.3**

- [ ] 12. 表示支援ロジック（バスト警告・プレビュー）を Domain に実装する
  - [ ] 12.1 バスト警告とカード入手プレビューを実装する
    - `ShouldWarn == (0 ≤ TargetValue − HandTotal ≤ margin)` を実装する
    - 候補カード加入後の予測 Hand_Total を実際の `ComputeHandTotal` と一致するよう算出する
    - _Requirements: 12.2, 12.3_

  - [ ]* 12.2 バスト警告のプロパティテストを書く
    - **Property 30: バスト警告は Target までの残りが閾値以内のとき発火する**
    - **Validates: Requirements 12.2**

  - [ ]* 12.3 プレビュー一致のプロパティテストを書く
    - **Property 31: カード入手候補のプレビュー合計は実際の加入結果と一致する**
    - **Validates: Requirements 12.3**

- [ ] 13. チェックポイント - Domain 層全体の PBT を検証する
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 14. 調整パラメータ ScriptableObject を整備する
  - [ ] 14.1 BalanceConfig / SynergyConfig / RunConfig を実装する
    - `BalanceConfig`（TargetValue=21、MaxStrengthBonus、StatRatios 60/20/20、BlackjackBoostRate、BlackjackDurationMs、BustMod、BustWarningMargin=3）
    - `SynergyConfig`（PerMatchingCardMultiplier）、`RunConfig`（StagesPerRun=8、CardRemovalChipCost、StartingHp、StartingWeapon）
    - `OnValidate` で不変条件（Target∈[1,99]、StatDistribution 合計=1.0、Boost∈[0,5]、Duration∈[1,600000]）を検証・クランプする
    - _Requirements: 3.1, 3.4, 3.6, 3.7, 7.2, 9.1, 10.1, 12.2_

  - [ ]* 14.2 Config 既定値・クランプのスモーク／ユニットテストを書く
    - `TargetValue==21`、`StagesPerRun==8`、Target=0→1 補正、配分合計≠1.0 の補正
    - _Requirements: 3.1, 9.1_

- [ ] 15. カード・武器データ ScriptableObject を整備する
  - [ ] 15.1 CardDefinition と CardDatabase（52 種）を実装する
    - `CardDefinition`（Suit, Rank, Effects, BaseCardValue, IsAce）と `CardDatabase` を実装する
    - 4 スート × 13 ランク = 52 定義アセットを生成するエディタユーティリティを実装する
    - _Requirements: 1.1, 5.1_

  - [ ] 15.2 WeaponDefinition と WeaponDatabase（4 種）を実装する
    - `WeaponDefinition`（WeaponSuit, ActionPattern、Card_Value なし）、`EquippedWeapon`、`WeaponDatabase` を実装する
    - Spade/Diamond/Club/Heart の 4 定義アセットを生成し、各 ActionPattern を相互に異なる値で設定する
    - _Requirements: 6.1, 6.2, 6.3_

  - [ ]* 15.3 データベースのスモークテストを書く
    - `CardDatabase` が 52 定義、`WeaponDatabase` が 4 スート分かつ ActionPattern が相互に異なることを検証する
    - _Requirements: 6.1, 6.2_

- [ ] 16. SaveRepository による永続化を実装する
  - [ ] 16.1 persistentDataPath へのアトミック書き込みと破損復元を実装する
    - 一時ファイル → リネームのアトミック書き込み、`save.bak` バックアップ保持
    - ロード時に Checksum 照合、破損・欠損時はバックアップ復元、両方不可なら初期状態で継続
    - SchemaVersion マイグレーション／未知バージョンの安全フォールバックを実装する
    - _Requirements: 11.1, 11.5_

  - [ ]* 16.2 破損復元のユニットテストを書く
    - Checksum 不一致時のバックアップ復元、両方破損時の初期化フォールバック
    - _Requirements: 11.1, 11.5_

- [ ] 17. チェックポイント - データ層と永続化を検証する
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 18. 入力とプレイヤーキャラクターを実装する
  - [ ] 18.1 InputReader で Input System をラップする
    - 既存 `InputSystem_Actions.inputactions` をラップし、Move / Jump / Attack を C# イベントとして公開する
    - Update ループで即時反映し入力遅延を抑える
    - _Requirements: 8.1, 8.2, 8.4, 8.7_

  - [ ] 18.2 PlayerCharacterController（横移動・ジャンプ・重力・接地・被弾）を実装する
    - `Rigidbody2D` + `Collider2D` + `GroundCheck` による横移動・ジャンプ・重力・接地停止を実装する
    - `ApplyStats(StatBreakdown)` で Domain のステータスを反映、`TakeDamage` で Domain のダメージ結果を適用する
    - HP>0 のときのみ操作を受け付ける（`CanAcceptInput` を購読）
    - _Requirements: 8.1, 8.2, 8.3, 8.6, 8.8, 3.4_

  - [ ]* 18.3 プレイヤー基本操作の PlayMode テストを書く
    - 移動・ジャンプ・接地停止の基本挙動、入力から反映まで 100ms 以内
    - _Requirements: 8.1, 8.2, 8.3, 8.7_

- [ ] 19. 武器と敵のコントローラを実装する
  - [ ] 19.1 WeaponController（4 スート別アクション）を実装する
    - `WeaponDefinition.ActionPattern` に基づく攻撃挙動を実行し、命中時に現在攻撃力でダメージを与える
    - Suit_Synergy の multiplier を攻撃パラメータへ反映する
    - _Requirements: 6.4, 7.1, 8.4, 8.5_

  - [ ] 19.2 EnemyController（AI・攻撃・被ダメージ・撃破）を実装する
    - 敵の移動・攻撃・被ダメージ・HP≤0 での撃破を実装し、プレイヤーへの命中でダメージを与える
    - _Requirements: 8.5, 8.6, 9.2_

  - [ ]* 19.3 攻撃・被弾の PlayMode テストを書く
    - 攻撃がスート別アクションで実行され、命中で敵 HP が減り、撃破されることを検証する
    - _Requirements: 6.4, 8.4, 8.5, 8.6_

- [ ] 20. ステージ・ラン管理と Coordination を実装する
  - [ ] 20.1 StageManager（敵・地形配置・クリア判定）を実装する
    - Stage 生成、1 体以上の敵と段差／足場の配置、全敵撃破で `StageCleared` 発火
    - _Requirements: 9.2, 9.3_

  - [ ] 20.2 RunManager（Stage 遷移・報酬・死亡／クリア仲介）を実装する
    - Stage クリア後の次 Stage 選択肢、Card／Weapon／Chip 報酬の提示、最終 Stage で Run クリア
    - 死亡時の Run 終了、Run 終了時に新 Run 開始の選択肢と Meta_Currency 付与を仲介する
    - _Requirements: 9.4, 9.5, 9.6, 10.4, 10.5_

  - [ ] 20.3 GameManager（起動・復元・注入・イベント中継）を実装する
    - 起動時に SaveRepository から Meta_Progression を復元し、GameConfig を各 Domain オブジェクトへ注入する
    - Domain イベント（HandChanged / StrengthBonusChanged / BustStateChanged / BlackjackStateChanged / SynergyChanged）を UI へ中継する
    - _Requirements: 11.3, 11.5_

  - [ ]* 20.4 ステージ／ラン進行とフローの PlayMode テストを書く
    - 敵・地形配置、クリア後の遷移・報酬・新 Run 提示、保存→再ロードでの Meta 復元
    - _Requirements: 9.2, 9.4, 9.6, 10.5, 11.5_

- [ ] 21. GameHUD / UI を実装し Domain と配線する
  - [ ] 21.1 GameHUD（合計・強化・HP・Bust・Blackjack・Ace・Suit・Synergy 表示）を実装する
    - Domain イベントを購読し、Hand_Total／Target_Value／Strength_Bonus／HP／Bust 警告・ラベル／Blackjack 状態／各 Ace の算入値／Weapon_Suit／Suit_Synergy を表示する
    - バスト警告（残り margin 以内）とカード入手プレビュー（予測 Hand_Total）を表示する
    - イベント発火から 1000ms 以内に UI 反映されるよう次フレーム更新で配線する
    - _Requirements: 1.6, 3.5, 4.4, 4.6, 5.4, 6.5, 12.1, 12.2, 12.3, 12.4, 12.5_

  - [ ] 21.2 Blackjack 持続タイマーを配線する
    - Blackjack 有効化から `BlackjackDurationMs` 経過後に無効化・ブースト解除し、`BlackjackStateChanged` を発火する
    - _Requirements: 3.6, 3.7_

  - [ ]* 21.3 UI タイミングと Blackjack 持続の PlayMode テストを書く
    - Domain イベント後 1000ms 以内の HUD 反映、Blackjack 有効化から Duration 経過での解除
    - _Requirements: 3.5, 3.7, 4.4, 4.6, 12.1, 12.4, 12.5_

- [ ] 22. 最終チェックポイント - 全体統合を検証する
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- `*` を付したサブタスク（ユニット／PBT／統合／PlayMode テスト）は任意であり、より速い MVP のために省略できる。トップレベルタスクとコア実装サブタスクは省略不可。
- 各タスクは要件番号への参照を持ち、トレーサビリティを確保する。
- PBT（Property 1〜31）は Domain 層の純粋ロジックを対象とし、FsCheck または CsCheck で**最低 100 反復**実行する。各プロパティテストは `Feature: blackjack-roguelike, Property {番号}: {本文}` タグを付す。
- UI タイミング・物理・シーン構成・入力レイテンシは PlayMode テスト（任意）で例示的に検証する。
- チェックポイントで段階的に検証し、Domain → Data → Presentation の順に統合する。

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["2.1", "14.1"] },
    { "id": 1, "tasks": ["2.2", "3.1", "14.2", "15.2"] },
    { "id": 2, "tasks": ["3.2", "15.1", "15.3"] },
    { "id": 3, "tasks": ["3.3", "3.4", "3.5", "3.6", "3.7", "3.8", "3.9", "3.10", "4.1"] },
    { "id": 4, "tasks": ["4.2", "4.3", "5.1", "7.1"] },
    { "id": 5, "tasks": ["5.2", "5.3", "5.4", "5.5", "7.2", "7.3"] },
    { "id": 6, "tasks": ["5.6", "5.7", "6.1", "9.1"] },
    { "id": 7, "tasks": ["6.2", "9.2", "9.3", "10.1"] },
    { "id": 8, "tasks": ["10.2", "10.3", "10.4", "11.1"] },
    { "id": 9, "tasks": ["10.5", "10.6", "10.7", "10.8", "10.9", "10.10", "10.11", "11.2", "12.1"] },
    { "id": 10, "tasks": ["11.3", "11.4", "11.5", "12.2", "12.3", "16.1"] },
    { "id": 11, "tasks": ["16.2", "18.1", "19.2"] },
    { "id": 12, "tasks": ["18.2", "19.1", "20.1"] },
    { "id": 13, "tasks": ["18.3", "19.3", "20.2", "20.3"] },
    { "id": 14, "tasks": ["20.4", "21.1", "21.2"] },
    { "id": 15, "tasks": ["21.3"] }
  ]
}
```
