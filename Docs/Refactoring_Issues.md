# Flip Logic - リファクタリング・イシュー一覧

アーキテクチャ分析レポート（`Architecture_Analysis.md`）に基づき、プロジェクトをよりシステマチックかつスケーラブルにするための改修要素を分割し、順序立ててイシュー化しました。

---

## 優先度 (P0) 基盤のデータ駆動化

### [DONE] Issue 1: タグシステムの型安全化とデータ駆動化
- **状態**: 完了 (2026-03-21)
- **内容**: 
  - `TagKeyRegistry` (ScriptableObject) によるマスタ管理導入。
  - `TagDefinition` の `Duration` 仕様を -1=永続にリファクタリング。
  - `TagBehaviorDef` の SO 化と `TagBehaviorRunner` の全挙動実装。
  - マジック文字列の完全置換は Issue 2 以降へ延期。

### [DONE] Issue 2: ルール機構（Rule Hack）のデータ駆動化
- **状態**: 完了 (2026-03-21)
- **目的**: ルール定義をコード直書きから解放し、エディタで構築可能にする。
- **タスク**:
  - [x] `RuleAsset` (ScriptableObject) の作成。
  - [x] `RulePageAsset` (ScriptableObject) の作成。
  - [x] `RulebookAsset` (ScriptableObject) の作成。
  - [x] `RulebookManager` のアセット読み込み対応。
  - [x] `GameManager` の初期ルールロード移行。

---

## 優先度 (P1) エンティティとステージの再構築

### [DONE] Issue 3: エンティティ管理とパフォーマンス最適化
- **状態**: 完了 (2026-03-21)
- **目的**: `FindObjectsByType` などの重い検索を排除し、O(1) での素早い参照を確立する。
- **タスク**:
  - `EntityRegistry` (ServiceLocator/Singleton) の導入。
  - `GameEntity` の `OnEnable`/`OnDisable` での自動登録・解除処理実装。
  - `GridMap._entityMap` に代わる位置やタグベースのエンティティ高速検索APIの提供。
  - `EntityType.Terrain` の見直し (HP/Attackなど余分なフィールドを持たないようにする)。

### [DONE] Issue 4: エンティティおよびステージのデータ（SO）活用化
- **状態**: 完了 (2026-03-21)
- **目的**: `EnemyData` などの未活用SOを活かし、ステージと敵を一括ロードできるようにする。
- **タスク**:
  - [x] `EntityFactory` の実装 (`EnemyData` と座標から `GameEntity` を動的生成)。
  - [x] `StageConfig` (ScriptableObject) の作成 (メタ情報)。
  - [x] Scene上の `EnemySpawner` / `CellTagSetter` による配置。
  - [x] `Player.prefab` の動的生成と `CameraFollow` 追従 (Issue 4.5)。
  - [x] 不要になったシーン上の `PlayerObj`, `EnemySymbolObj` の削除。

---

## 優先度 (P2) ビジュアルと UI アーキテクチャ

### [DONE] Issue 5: ビジュアル定義の SO 化
- **状態**: 完了 (2026-03-21)
- **目的**: 色やスプライトのハードコードを排除し、アーティストが変更容易にする。
- **タスク**:
  - [x] `EntityVisualDef` (ScriptableObject) の作成。
  - [x] `TileVisualDef` (ScriptableObject) の作成。
  - [x] `UITheme` (ScriptableObject) の作成。
  - [x] 各 Renderer/Generator との結びつけ。
  - [x] 実行時の自動デフォルトアセットロード機能の実装。

### [DONE] Issue 6: ターン制処理機構のモダン化
- **状態**: 完了 (2026-03-21)
- **目的**: バトルとフィールドの二重実装を解消し、演出を挟める非同期なフェーズ進行（Phase Pipeline）を構築。
- **タスク**:
  - [x] `IPhaseHandler` と `TurnContext` の定義。
  - [x] `TurnManager` のフェーズ実行処理を非同期処理（UniTask等）へ置き換え。
  - [x] フィールド用フェーズ、バトル用フェーズのハンドラ実装。
  - [x] フェーズ間のイベント発行と解除の統一。

---

## 優先度 (P3) デザイナー用拡張パネルツール

### [DONE] Issue 7: フリップロジック・デザイナーパネル (EditorWindow)
- **状態**: 完了 (2026-03-22)
- **目的**: ゲームデザイナーがコードを書かずに全コンテンツをテスト・構築できる環境の提供。
- **タスク**:
  - [x] **ウィンドウ骨格**: `EditorWindow` + タブ切り替え基盤（Tags, Rules, Enemies, Stages, Debug）。
  - [x] **Tag Registry Panel**: `TagKeyRegistry` のタグ一覧・編集GUI。
  - [x] **Rule Editor Panel**: `RuleAsset` のフォームベースGUI構築、P/Q条件設定、`TagKeyRegistry` 連動ドロップダウン、LogicState プレビュー。
  - [x] **Enemy Editor Panel**: `EnemyData` のステータスと初期タグ設定。
  - [x] **Stage Editor Panel**: `StageConfig` のメタ情報編集。
  - [x] **Debug Panel（軽量版）**: Play中のエンティティ/タグ一覧表示、手動タグ操作、アクティブルール一覧。

---

## 優先度 (P4) デザイナー拡張ツール（高度機能）

### [DONE] Issue 8: マップエディタ補助とデバッグログ機能 (2026-03-22)
**目的:** Designer Panelを拡張し、実際にステージを作成・デバッグするための補助機能（マップ配置、ルール適用ログ閲覧、即時テスト開始）を実装する。
**前提:** Issue 7のクリア。
**タスク:**
- [x] MapPlacementDataとMapEditorTabの実装（SceneView連携による敵・タグ配置）
- [x] RuleEvaluatorからDebugTabへのリアルタイムログ送信機能（適用ルールの可視化）
- [x] StageEditorTabへの即時ロード（Play）機能追加

---

## 優先度 (P5) ルールロジックの洗練と特殊ギミック

### [NEW] Issue 9: 毒システムの再定義とルール結合の柔軟化
- **状態**: 新規
- **目的**: 毒状態とダメージを分離し、否定条件を含む高度なパズルルールを構築可能にする。
- **タスク**:
  - `rule_poison_swamp` の更新（デカップリング対応）。
  - `rule_poison_damage` の新規作成。
  - 特殊ルール（!Damage -> Poison Swamp）の動作確認。

### [NEW] Issue 10: ビジュアル強化（ドット絵・低コスト・プログラマブル）
- **状態**: 新規
- **目的**: プレイヤー、敵、ブロックの見た目をゲームらしくリッチにする。
- **タスク**:
  - プログラマブル・アニメーション（Tween等による呼吸・移動演出）の導入。
  - エネミー画像の一括インポート・自動リサイズ機能（デザイナーパネル拡張）。
  - ブロック・タイルの立体感や自動結合などのプログラマブルなディテールアップ。
