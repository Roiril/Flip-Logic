# Flip Logic システム仕様書

本ドキュメントは、Unity 2D思考型パズルRPG『Flip Logic』の全体アーキテクチャ、コアシステム、データ構造、各視点（プレイヤー、クリエイター、デザイナー）からの運用方法を定義する最新の仕様書である。

---

## 1. コア設計思想

*   **タグベースシステム (Tag-Based System):** ゲーム内のステータス、属性、ギミック、法則はすべて文字列ベースの「タグ (`TagDefinition`)」として表現される。
*   **絶対法則と可変ルール:**
    *   **絶対法則 (`TagBehaviorDef`):** HP0で死亡、燃焼でダメージなど、ゲームの根幹をなす変えられない物理法則。
    *   **可変ルール (`RuleData`):** 「PならばQである (P → Q)」の形式で定義され、プレイヤーが論理操作（逆・裏・対偶など）によって改変可能な法則。
*   **データ駆動 (Data-Driven):** 敵、アイテム、ルール、ステージ構成などはすべてScriptableObject（`EnemyData`, `RuleAsset`, `StageConfig`など）として定義され、デザイナーがコード編集なしで調整可能。
*   **同期ターン制:** 探索・バトルの区別なく、すべてが同一のグリッドマップ上で同期ターン制（Player → Enemy → Rule Eval → Tag Tick）で進行する。

---

## 2. システム・アーキテクチャ

### 2.1. ターン処理機構 (Turn System)
ゲーム進行は `TurnManager` と `TurnResolutionProcessor` によって管理され、以下の固定フェーズでループする。

1.  **WaitingForInput:** プレイヤーの入力（移動、バトルコマンド等）待ち。入力に応じてターン消費処理（`ConsumeTurn`）が呼ばれる。
2.  **PlayerAction:** プレイヤーキャラクターの行動が実行される。
3.  **EnemyAction:** マップ上の全ての生存敵 (`EnemySymbol`) がAI（巡回、追従など）に基づき行動する。
4.  **RuleEvaluation:** `RuleEvaluator` がアクティブルール（P→Q）を全エンティティに対して評価し、条件(P)を満たす対象に結果(Q)のタグを付与/剥奪する。
5.  **TagTick:** 全エンティティおよびセルの所持タグの残りターン数（Duration）を減算し、期限切れのタグを削除する。
6.  **End:** 各フェーズの終了処理やクリーンアップ。

### 2.2. エンティティとオブジェクト管理
*   **GameEntity:** プレイヤー、敵、障害物など全オブジェクトの基底コンポーネント。HP、攻撃力、防御力、自身が持つタグのコレクション (`TagContainer`) を管理する。
*   **EntityRegistry:** 全 `GameEntity` インスタンスを集中管理するシングルトン。型 (`EntityType`) や InstanceID による `O(1)` の高速検索を提供し、毎フレームの無駄な全検索（`FindObjectsByType`等）を排除している。

### 2.3. タグシステム (Tag System)
*   **TagContainer:** 各 `GameEntity` およびグリッドの各セルが持つ。タグの追加、削除、存在判定、残りターン数管理、変更時のイベント発火を行う。
*   **TagDefinition:** タグの実体。`Key`（例: "Element"）と `Value`（例: "Fire"）、効果時間 `Duration`（1以上のターン数。-1は永続）、付与元 `Source`、挙動ID `BehaviorId` で構成される。
*   **TagKeyRegistry:** 意図しないタグ名を防ぐため、使用可能な `Key` と `Value` のペアをScriptableObjectで事前定義・バリデーションする辞書。

### 2.4. ルールエンジン (Logic Engine)
*   **PropositionData:** ルールの条件（P）と結果（Q）を構成するデータ。否定（Negate: true/false）の状態を持ち、肯定文・否定文を動的に生成する。
*   **RuleData:** 1つのルール（P → Q）オブジェクト。
    *   `SubjectFilterP`: 対象フィルター（例: "Enemyのみ"）
    *   `TagConditionP`: 条件となるタグ（例: "Element:Fire" を持っているか）
    *   `TagEffectQ`: 結果となるタグ操作（例: "Status:InstantDeath" を追加）
*   **RuleEvaluator:** 行動フェーズの最後に全エンティティを走査し、アクティブルール群の条件判定と結果適用（タグの付与・削除）を一括で行う。
*   **TagBehaviorRunner:** 「絶対法則」を処理する実行器。`TurnEnd` などのタイミングで、タグに紐づく `TagBehaviorDef`（例: "HPを0にする"、"ダメージを与える"）を実行する。ルール適用（Qのタグ付与）→ 絶対法則の実行 という順序で処理が連鎖する。

---

## 3. レイヤー別仕様と操作ガイド

### 3.1. プレイヤー視点 (Player Experience)

プレイヤーはグリッド上を移動し、ルールの論理を改変して困難を突破する。

*   **移動とエンカウント:**
    *   十字キー/WASDで1マス移動（1ターン消費）。
    *   敵キャラクター（シンボル）に隣接・衝突すると、その場でシームレスにバトル（`BattleManager`）へと移行する。
*   **バトルコマンド:** 実質的に**全てがタグ付与行為**として処理される。
    *   `たたかう`: 対象に `[Damage:Physical]` タグを付与。ターン終了時の絶対法則でダメージ処理が行われる。
    *   `ぼうぎょ`: 自身に `[Status:Defending]` タグを付与。被ダメージを軽減する。
    *   `アイテム`: アイテムデータに定義された効果タグを対象に付与する。
    *   `にげる`: バトル状態を解除する。
*   **ルール改変 (Rule Hack):**
    *   `ルールブック` コマンドからルール画面を開き、現在アクティブなルール（P→Q）の論理構造を操作する。
    *   **Swap (逆):** PとQを入れ替える（Q → P）。
    *   **Negate (裏):** PとQの肯定/否定を反転する（Not P → Not Q）。
    *   **Contrapositive (対偶):** SwapとNegateを両方行う（Not Q → Not P）。
    *   一部のルールは改変に「ロジックポイント（LPやコスト）」を要する場合がある（今後の拡張）。

### 3.2. クリエイター視点 (System/Logic Creator)

本プロジェクトでは**マスターデータ操作＝ScriptableObjectの作成**である。C#の主要コードには触れずともゲームを拡張できる。

*   **新規ルールの作成:**
    1.  `Assets/Data/Rules/` にて `Create > FlipLogic > Rulebook > Rule Asset` を実行。
    2.  `SubjectFilterP`（対象）、`TagConditionP`（発生条件のタグ）、`TagEffectQ`（結果として付与/削除するタグ）を設定する。
    3.  既存のタグ以外を使いたい場合は、先に `TagKeyRegistry` (Assets/Data/TagKeyRegistry.asset) にKeyとValueの組み合わせを登録する。
*   **ルールの束ね方 (RulebookAsset):**
    *   `RulePageAsset` に複数ルールを登録し、それを `RulebookAsset` に登録することで、ステージや進行度ごとに解放されるルールブックを定義できる。
*   **絶対法則（状態異常など）の実装:**
    *   既存のRuleを拡張するのではなく、システムの根幹挙動を作る場合は `Create > FlipLogic > Core > Tag Behavior Def` で作成。
    *   "TurnEnd" 時に特定のタグを持つ対象にダメージを与える、等の振る舞いを定義し、`TagDefinition` の `BehaviorId` に指定する。

### 3.3. デザイナー視点 (Level/Visual Designer)

ステージ構築、敵配置、ビジュアル設定を行う。専用の「Designer Panel (Ctrl+Shift+D)」エディタ拡張機能でより直感的に操作可能。

*   **新規ステージ（マップ）の作成手順:**
    1.  新規Sceneを作成し、`Grid` と `Tilemap` (Ground, Walls等) を配置する。
    2.  Sceneに `MapPlacementSpawner` コンポーネントを持つGameObjectを配置する。
    3.  `Create > FlipLogic > Explore > Map Placement Data` でアセットを作成する。
    4.  作成した `MapPlacementData` 内にて、敵の配置（座標と使用する `EnemyData`）と、初期配置されるセルタグ（毒沼、炎など）を定義する。
    5.  `MapPlacementSpawner` にそのデータを紐付ける。
    6.  必要に応じて `StageConfig` を作成し、シーンとルールブック、マップ構成情報を紐付け、ゲームマネージャーからロード可能にする。
*   **敵 (Enemy) の作成:**
    1.  `Create > FlipLogic > Data > Enemy Data` アセットを作成。
    2.  名前、最大HP、攻撃・防御力、初期所持タグ、およびAIタイプ（待機、ランダム巡回、プレイヤー追従など）を設定する。
*   **ビジュアル（見た目）の設定:**
    *   **エンティティ表示 (`EntityVisualDef`):** 敵の見た目を定義する。Sprite画像を指定できるほか、プロトタイプ用に「文字が書かれた色付きの丸」を自動生成する機能 (`UseDynamicSprite`) を持つ。
    *   **セル属性表示 (`TileVisualDef`):** "Element:Fire" などのタグを持つマスの見た目（オーバーレイ色設定やカスタムスプライト）を定義する。`TileOverlayRenderer` がこれを読み取り、マップ上のタグを視覚化する。
    *   **UIテーマ (`UITheme`):** 文字色やフォントを定義し、一貫したUIデザインを適用する。

---

## 4. プログラム・ディレクトリ構造

```text
Assets/
├── Scripts/
│   ├── Core/         : GameEntity, TurnManager, EntityRegistryなど基盤システム
│   ├── Data/         : EnemyData, RuleData, StageConfig などのデータ構造とSO定義
│   ├── Logic/        : RuleEvaluator, LogicEvaluator などルールエンジン関連
│   ├── Explore/      : PlayerController, EnemySymbol, MapPlacementSpawner 探索ロジック
│   ├── Battle/       : BattleManager, BattleCommand バトル専用ロジック
│   ├── Grid/         : GridMap, TileOverlayRenderer グリッド・空間管理
│   ├── Rulebook/     : RulebookManager, RuleAsset などルールデータ管理
│   ├── Editor/       : DesignerPanelWindow などのエディタ拡張ツール群
│   └── UI/           : UIToolkit連携や各種UI表示コンポーネント
├── Data/             : 作成済みのScriptableObjectアセット群（敵、ルール等）
├── Prefabs/          : 再利用可能な構成済みGameObject
├── Scenes/           : 探索用、バトル用（統合予定）、タイトルなどのシーン群
└── Materials/        : マテリアルデータ
```

## 5. 開発時の注意事項

*   **タグの直打ち厳禁:** ロジック内で `"Status:Poison"` などと文字列をハードコードせず、必ず定数クラスまたはインスペクター入力（あるいは機能化されたSO）経由で扱うこと。`TagKeyRegistry`の存在を尊重する。
*   **シーン上の動的検索の禁止:** ターン進行中などに `FindObjectsByType` 等を実行しないこと。エンティティの取得には必ず `EntityRegistry` (O(1)検索) を使用する。
*   **分離と再利用:** `GameEntity` はMVCのModelに近く、それ自体はUnityの描画やUIを密に持たない。表示系は `EntitySpriteFactory` やUI層、`TileOverlayRenderer`がそれを購読/走査して実現する設計を保つ。
