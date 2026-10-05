# Requirements Document

## Introduction

本ドキュメントは、Unity で開発する**横スクロール 2D アクションローグライク**ゲーム「Blackjack Roguelike（仮称）」の要件を定義する。視点はトップダウンではなく横視点であり、ジャンプ・段差・横移動を伴うプラットフォーマー寄りの手触り（『Dead Cells』系）を目指す。

コアコンセプトは、トランプのブラックジャックをモチーフにした戦闘強化システムである。ゲームは数字を持つ要素（**カード**）と、攻撃アクションを担う要素（**武器**）を明確に分離する。

- **武器**は 4 スート（Spade / Diamond / Club / Heart）に対応する 4 種類で、各スートが固有のアクションパターン（攻撃挙動）を持つ。武器には数字が付与されず、バスト判定・合計計算の対象外である。
- **カード**は 4 スート × 13 ランク = 52 種類で構成され、各カードがランクに対応する数字を持つ。カードはキャラクターの能力上昇や特殊効果を付与する。
- ランで入手したカードは**自動的に**手札（合計計算の対象）へ加わる。プレイヤーが装備操作で選んで加えるのではない。カードの数字の合計を Target_Value（デフォルト 21）に近づけるほど強化され、21 を超える（バストする）と制限付き強化状態へ移行する。
- カードの自動加入により、入手するほど合計が 21 に近づき、やがて超過（バスト）リスクが高まるという、能動的に避けづらい緊張感が核になる。

プレイヤーはこのリスク・リワード構造を軸に、繰り返し挑戦（ローグラン）して少しずつ恒久的に強くなっていく。

本ドキュメントは企画具体化の初期段階にあり、未確定の論点について妥当な**デフォルト案**を要件として明文化する。各デフォルト案は後の調整を前提とし、該当箇所に「DEFAULT（調整可能）」と注記する。

> **DEFAULT（調整可能）な論点一覧:** 以下の数値・ルールは暫定値であり、プレイテストを通じて調整される前提である。
> - 目標値（21）、バスト閾値、強化係数などの具体値
> - バスト時の挙動（デフォルト: 即死ではなく「制限付き強化状態」。強力だがデメリット付き。具体的な制限・強化内容は後で詳細を詰める）
> - 強さの反映先（デフォルト: 攻撃力を主軸に複数ステータスへ配分）
> - エース可変（デフォルト: 採用する）
> - 武器スートとカードスートのシナジー（デフォルト: 一致スートのカード所持で当該武器のアクションを強化。具体値は調整可能）
> - 手札上限の扱い（デフォルト: 上限なし。将来、上限を設ける場合でも超過時に即死はしない）
> - 手札からのカード除去手段（デフォルト: 特定マス／ショップ／コストでの除去を救済・調整手段として提供）
> - 横スクロール要素（デフォルト: ジャンプ・段差・横移動を採用）
> - メタプログレッション（デフォルト: 恒久通貨によるアンロック制）
> - ジョーカー（**将来拡張**。現時点では必須要件に含めない）

## Glossary

- **Game**: 本ゲームシステム全体。Unity 上で動作する横スクロール 2D アクションローグライクアプリケーション。
- **Player_Character**: プレイヤーが操作する 2D キャラクター。横移動・ジャンプ・攻撃・被弾の主体。
- **Weapon**: Player_Character が装備して攻撃に用いる要素。4 スート（Spade / Diamond / Club / Heart）に対応する 4 種類が存在し、各スートが固有のアクションパターンを持つ。数字（Card_Value）を持たず、Hand_Total の算出および Bust 判定の対象外である。
- **Weapon_Suit**: Weapon が属する 4 種類のスート（Spade / Diamond / Club / Heart）。Weapon のアクションパターンを決定する。
- **Equipped_Weapon**: Player_Character が現在装備している Weapon。
- **Card**: ランで入手し、入手時に自動的に Hand へ加わる要素。1 つの Suit と 1 つの Rank を持ち、Rank に対応する数字（Card_Value）を持つ。キャラクターの能力上昇や特殊効果を付与する。4 スート × 13 ランク = 52 種類で構成される。
- **Suit**: Card が持つ 4 種類の属性（Spade / Diamond / Club / Heart）。Weapon_Suit との一致判定（シナジー）に用いる。Card の Suit は独立した戦闘属性ではなく、Weapon_Suit との一致でシナジーを生むためのタグである。
- **Rank**: Card が持つ 13 種類の序列（A, 2〜10, J, Q, K）。Card_Value を決定する。
- **Card_Value**: Card の Rank に対応する整数値。数字カード（2〜10）は額面、J / Q / K は 10、A は Ace として 1 または 11（Ace_Card を参照）。
- **Ace_Card**: Rank が A の Card。Card_Value を 1 または 11 のいずれかとして扱える。ブラックジャックのエースに相当する。
- **Hand**: Player_Character が現在保持している Card の集合。Run 中に入手した Card が自動的に加わる。合計計算の対象である。
- **Hand_System**: Hand を管理し、Hand_Total と Bust 状態を算出するサブシステム。
- **Hand_Total**: Hand_System が算出する、Hand 内の全 Card の Card_Value の合計値。Weapon は含まれない。
- **Target_Value**: Hand_Total の理想到達点。デフォルトは 21（DEFAULT（調整可能））。
- **Bust**: Hand_Total が Target_Value を超えた状態。即死ではなく「制限付き強化状態（Bust_State）」へ移行する契機となる。
- **Bust_State**: Bust 発生時に Player_Character が移行する、強力だがデメリットを伴う制限付き強化状態（DEFAULT（調整可能）。詳細は後で詰める）。
- **Strength_Bonus**: Hand_Total と Target_Value の近さに応じて Player_Character に付与される強化値。
- **Max_Strength_Bonus**: Strength_Bonus が取りうる最大値。Hand_Total が Target_Value と等しいときに付与される。
- **Blackjack 状態**: Hand_Total が Target_Value と完全一致したときに発動する一時的な追加ブースト状態。
- **Suit_Synergy**: Equipped_Weapon の Weapon_Suit と同一の Suit を持つ Card を Hand に保有しているときに、当該 Weapon のアクションへ付与される強化（DEFAULT（調整可能））。
- **Card_Removal**: Hand から Card を取り除く手段（特定マス／ショップ／コスト等）。合計調整・バスト回避の救済手段。
- **Run**: 1 回の挑戦（開始から Player_Character の死亡またはクリアまで）。ローグライクの 1 プレイ単位。
- **Stage**: Run を構成する横スクロール戦闘エリア。敵と地形（段差・足場）を含む。
- **Enemy**: Stage 内でプレイヤーに敵対する 2D キャラクター。
- **Meta_Progression**: Run をまたいで恒久的に蓄積・解放される要素（恒久通貨とアンロック）。
- **Meta_Currency**: Run の結果として獲得し、Run をまたいで保持される恒久通貨。
- **Chip**: Run 内で使用する一時的な通貨。Run 終了時に失われる。

## Requirements

### Requirement 1: カードの自動加入と手札管理

**User Story:** プレイヤーとして、ランで入手したカードが自動的に手札へ加わってほしい。それにより、入手するほど合計が 21 に近づき、避けづらい緊張感が生まれるから。

#### Acceptance Criteria

1. THE Hand_System SHALL Hand 内の全 Card の Card_Value の合計を Hand_Total として算出する。
2. WHEN Card が入手される、THE Hand_System SHALL 当該 Card を Hand へ自動的に加え、Hand_Total を再計算する。
3. WHEN Card が Card_Removal によって Hand から取り除かれる、THE Hand_System SHALL Hand_Total を再計算する。
4. THE Hand_System SHALL Weapon を Hand_Total の算出対象に含めない。
5. THE Hand_System SHALL Hand に保持できる Card の数を上限なしとして扱う（DEFAULT（調整可能）: 将来上限を設ける場合でも、上限超過を理由に Player_Character を即死させない）。
6. THE Game SHALL 現在の Hand_Total と Target_Value を UI 上に常時表示する。

### Requirement 2: カードによる手札からの除去（救済・調整）

**User Story:** プレイヤーとして、手札からカードを取り除く手段がほしい。それにより、自動加入で高まったバストリスクを能動的に調整できるから。

#### Acceptance Criteria

1. THE Game SHALL Card_Removal の手段（デフォルト: 特定マス／ショップ／コスト消費、DEFAULT（調整可能））を Run 内で提供する。
2. WHEN プレイヤーが Card_Removal を実行する、THE Hand_System SHALL 指定された Card を Hand から取り除き、Hand_Total を再計算する。
3. WHERE Card_Removal がコストを伴う、THE Game SHALL 当該コスト（デフォルト: Chip、DEFAULT（調整可能））を消費する。
4. IF Card_Removal のコストが不足している、THEN THE Game SHALL 当該除去を拒否し、不足している旨を提示する。
5. WHEN Card_Removal により Hand_Total が Target_Value 以下へ戻る、THE Hand_System SHALL Bust_State を解除し、Strength_Bonus の算出を再開する。

### Requirement 3: Hand_Total による強化（コアシステム）

**User Story:** プレイヤーとして、手札の合計を 21 に近づけるほど強くなりたい。それにより、カードを積むか除去するかの判断に意味が生まれるから。

#### Acceptance Criteria

1. THE Hand_System SHALL Target_Value の初期値を 21 とする（DEFAULT、1 以上 99 以下の整数で調整可能）。
2. WHILE Hand_Total が 0 以上 Target_Value 以下である、THE Hand_System SHALL Strength_Bonus を 0 以上 Max_Strength_Bonus 以下の範囲で、(Hand_Total ÷ Target_Value) に比例して単調増加する値として算出する。
3. WHEN Hand_Total が Target_Value と等しくなる、THE Hand_System SHALL Strength_Bonus を Max_Strength_Bonus（最大値）に設定する。
4. WHEN Strength_Bonus が算出される、THE Hand_System SHALL Strength_Bonus のうち攻撃力へ 60%、移動速度へ 20%、最大 HP へ 20% を配分して Player_Character の各ステータスに反映する（デフォルト配分、各比率は合計が 100% となる範囲で調整可能）。
5. WHEN Strength_Bonus が変化する、THE Game SHALL 変化後の攻撃力、移動速度、最大 HP の各値を 1000 ミリ秒以内に UI へ反映する。
6. WHILE Hand_Total が Target_Value と等しい、THE Hand_System SHALL Blackjack 状態を有効化し、攻撃力に対して Blackjack_Boost_Rate（デフォルト値、0% 以上 500% 以下で調整可能）の追加ブーストを付与する。
7. WHEN Blackjack 状態が有効化されてから Blackjack_Duration（デフォルト値、1 ミリ秒以上 600000 ミリ秒以下で調整可能）が経過する、THE Hand_System SHALL Blackjack 状態を無効化し、Blackjack_Boost_Rate による追加ブーストを解除する。

### Requirement 4: バスト時の挙動（制限付き強化状態）

**User Story:** プレイヤーとして、合計が 21 を超えたときに即ゲームオーバーにならず、代わりにリスクのある強化状態になってほしい。それにより、積みすぎが即敗北ではなく、別のプレイスタイルへの転換点になるから。

#### Acceptance Criteria

1. WHEN Hand_Total が Target_Value を超える、THE Hand_System SHALL Player_Character を Bust_State にする。
2. IF Player_Character が Bust_State である、THEN THE Game SHALL 当該状態を理由に Player_Character を即時死亡させてはならない。
3. WHILE Player_Character が Bust_State である、THE Hand_System SHALL 強力な強化効果とデメリット効果の双方を付与する（デフォルト: 攻撃力に追加ブーストを付与する一方で被ダメージを増加させる、DEFAULT（調整可能）。具体的な制限・強化内容は後で詳細を詰める）。
4. WHEN Player_Character が Bust_State になる、THE Game SHALL Bust_State を示す UI 表示（警告色およびバストラベル）を 1000 ミリ秒以内に有効化する。
5. WHEN Card_Removal により Hand_Total が Target_Value 以下へ戻る、THE Hand_System SHALL Bust_State を解除する。
6. WHEN Bust_State が解除される、THE Game SHALL Bust_State を示す UI 表示を 1000 ミリ秒以内に無効化する。

### Requirement 5: エース（可変数字）要素

**User Story:** プレイヤーとして、1 と 11 を切り替えられるエースカードを使いたい。それにより、合計の微調整とバスト回避の戦略が生まれるから。

#### Acceptance Criteria

1. THE Hand_System SHALL Ace_Card の Card_Value を 1 または 11 のいずれかとして扱う（デフォルト: 採用する、DEFAULT（調整可能））。
2. WHILE Ace_Card が Hand に含まれている、THE Hand_System SHALL Hand_Total が Target_Value を超えない範囲で最大となるよう Ace_Card の値を自動的に選択する。
3. IF Ace_Card を 11 として算入すると Hand_Total が Target_Value を超える、THEN THE Hand_System SHALL 当該 Ace_Card の値を 1 として算入する。
4. THE Game SHALL 各 Ace_Card が現在 1 と 11 のどちらとして算入されているかを UI 上に表示する。

### Requirement 6: 武器のスート別アクション

**User Story:** プレイヤーとして、装備した武器のスートごとに異なる攻撃アクションで戦いたい。それにより、数字（カード）とは独立した手触りの違いが戦略要素になるから。

#### Acceptance Criteria

1. THE Game SHALL Spade / Diamond / Club / Heart の 4 種類の Weapon を提供する。
2. THE Game SHALL 各 Weapon_Suit に固有のアクションパターン（攻撃挙動）を割り当てる。
3. THE Game SHALL 各 Weapon に Card_Value を割り当てない。
4. WHEN プレイヤーが攻撃入力を行う、THE Player_Character SHALL Equipped_Weapon の Weapon_Suit に対応するアクションパターンで攻撃を実行する。
5. THE Game SHALL Equipped_Weapon の Weapon_Suit をアイコンまたは色で UI 上に表示する。

### Requirement 7: 武器スートとカードスートのシナジー

**User Story:** プレイヤーとして、装備中の武器と同じスートのカードを持っているときに武器が強化されてほしい。それにより、カードのスートを意識した手札編成に意味が生まれるから。

#### Acceptance Criteria

1. WHILE Hand が Equipped_Weapon の Weapon_Suit と同一の Suit を持つ Card を 1 枚以上含む、THE Hand_System SHALL 当該 Weapon のアクションへ Suit_Synergy を付与する（DEFAULT（調整可能））。
2. THE Hand_System SHALL Suit_Synergy の強化量を、一致する Suit の Card 枚数に応じて増加させる（デフォルト: 枚数比例、DEFAULT（調整可能））。
3. WHEN Equipped_Weapon が変更される、THE Hand_System SHALL 新しい Weapon_Suit を基準に Suit_Synergy を再算出する。
4. WHEN Hand 内の一致 Suit の Card 枚数が変化する、THE Hand_System SHALL Suit_Synergy を再算出する。
5. THE Game SHALL 現在適用中の Suit_Synergy の有無と強度を UI 上に表示する。

### Requirement 8: 横スクロール 2D アクションの基本操作

**User Story:** プレイヤーとして、横視点でキャラクターを走らせ、ジャンプし、攻撃したい。それにより、横スクロールアクションゲームとして遊べるから。

#### Acceptance Criteria

1. WHEN プレイヤーが左右の移動入力を行う、THE Player_Character SHALL 入力方向へ横方向に移動する。
2. WHEN プレイヤーがジャンプ入力を行う、THE Player_Character SHALL 上方向へジャンプし、重力により接地まで降下する。
3. WHILE Player_Character が足場または地形の上に接地している、THE Game SHALL Player_Character の落下を停止させる。
4. WHEN プレイヤーが攻撃入力を行う、THE Player_Character SHALL Equipped_Weapon のアクションパターンに基づく攻撃を実行する。
5. WHEN Player_Character の攻撃が Enemy に命中する、THE Game SHALL Player_Character の現在攻撃力に基づくダメージを Enemy に適用する。
6. WHEN Enemy の攻撃が Player_Character に命中する、THE Game SHALL ダメージを Player_Character の HP から減算する。
7. THE Game SHALL プレイヤー入力から画面上の対応アクション反映までの遅延を 100ms 以内に保つ。
8. WHILE Player_Character の HP が 0 より大きい、THE Game SHALL プレイヤー操作を受け付ける。

### Requirement 9: 敵とステージ

**User Story:** プレイヤーとして、敵と地形が配置された横スクロールステージを攻略したい。それにより、ランに挑戦する目的と手応えが生まれるから。

#### Acceptance Criteria

1. THE Game SHALL 各 Run を複数の Stage で構成する（デフォルト: 1 Run あたり 8 Stage、DEFAULT（調整可能））。
2. WHEN Stage が開始される、THE Game SHALL 当該 Stage に 1 体以上の Enemy と、段差または足場を含む地形を配置する。
3. WHEN Stage 内の全 Enemy が撃破される、THE Game SHALL 当該 Stage をクリア状態にする。
4. WHEN Stage がクリア状態になる、THE Game SHALL 次の Stage へ進む選択肢を提示する。
5. WHEN Run 内の最終 Stage がクリア状態になる、THE Game SHALL 当該 Run をクリアとして扱う。
6. WHEN Stage がクリア状態になる、THE Game SHALL 報酬として Card、Weapon、または Chip の選択肢を提示する。

### Requirement 10: ローグラン構造と死亡・リセット

**User Story:** プレイヤーとして、死んだらそのランの手札と武器を失ってやり直したい。それにより、ローグライクの緊張感と再挑戦のループが成立するから。

#### Acceptance Criteria

1. WHEN Run が開始される、THE Game SHALL Player_Character の HP を初期値に、Hand を初期状態に、Equipped_Weapon を初期武器に設定する。
2. WHEN Player_Character の HP が 0 以下になる、THE Game SHALL 当該 Run を終了（死亡）として扱う。
3. WHEN Run が死亡で終了する、THE Game SHALL 当該 Run 中に入手した Card、Weapon、および Chip を破棄する。
4. WHEN Run が終了する（死亡またはクリア）、THE Game SHALL 当該 Run の成果に応じた Meta_Currency を算出して Player に付与する。
5. WHEN Run が終了する、THE Game SHALL 新しい Run を開始する選択肢を提示する。

### Requirement 11: メタプログレッション

**User Story:** プレイヤーとして、繰り返し挑戦するうちに恒久的に強くなりたい。それにより、負けても前進している実感が得られるから。

#### Acceptance Criteria

1. THE Game SHALL Meta_Currency を Run をまたいで永続的に保存する。
2. WHERE プレイヤーが Meta_Currency を保有している、THE Game SHALL Meta_Currency を消費して恒久アンロック（新規 Card、新規 Weapon、初期ステータス向上など）を購入できるようにする（デフォルト: アンロック制、DEFAULT（調整可能））。
3. WHEN 恒久アンロックが購入される、THE Game SHALL 以降の全 Run に当該アンロック効果を適用する。
4. IF Meta_Currency が購入コストに満たない、THEN THE Game SHALL 当該アンロックの購入を拒否し、不足している旨を提示する。
5. THE Game SHALL アプリ再起動後も Meta_Progression の状態を復元する。

### Requirement 12: 状態の表示と可読性

**User Story:** プレイヤーとして、現在の合計・強さ・バストの危険度を一目で把握したい。それにより、カードを積むか除去するかの判断を素早く行えるから。

#### Acceptance Criteria

1. THE Game SHALL Hand_Total、Target_Value、現在の Strength_Bonus を同一画面上に表示する。
2. WHILE Hand_Total が Target_Value に対して Bust に近い（デフォルト: 残り 3 以内、DEFAULT（調整可能））、THE Game SHALL バスト警告を視覚的に提示する。
3. WHEN Card の入手候補が提示される、THE Game SHALL 当該 Card が自動加入した場合の Hand_Total 変化を事前に表示する。
4. THE Game SHALL 現在の HP を UI 上に表示する。
5. THE Game SHALL Equipped_Weapon の Weapon_Suit と、適用中の Suit_Synergy を UI 上に表示する。
```
