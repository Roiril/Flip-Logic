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

### Issue 7: フリップロジック・デザイナーパネル (EditorWindow)
- **目的**: ゲームデザイナーがコードを書かずに全コンテンツをテスト・構築できる環境の提供。
- **タスク**:
  - **Tag Registry Panel**: タグマスタとビジュアル定義の編集。
  - **Rule Editor Panel**: `RuleDataAsset` のGUI構築、P/Q条件設定、倫理状態(LogicState)のプレビュー。
  - **Enemy Editor Panel**: `EnemyData` のステータスと初期タグ設定。
  - **Stage Editor Panel**: `StageData` 用の設定。マップエディタ、初期配置などの設定機能。
  - **Play Test Panel**: ルール評価ログやステージの即時ロードボタンの提供。
