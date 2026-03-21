# Issue 8: マップエディタ補助とデバッグログ機能

## 概要 (Overview)
- **目的:** Issue 7 で先送りした高度なデザイナー向け機能を実装し、ステージ構築とルールデバッグの効率を大幅に向上させる。
- **対象:** `FlipLogic.Editor.DesignerPanel` (既存パネルへのタブ追加・機能拡張)
- **前提:** Issue 7（Designer Panel 基盤）が完了していること。

## 背景 (Background)
Issue 7 により基本的なデータ管理パネルは完成したが、以下の作業はまだエディタ外や手作業に依存している:
- **マップ配置**: 敵やギミックの座標指定を `EnemySpawner` / `CellTagSetter` で個別に行うため、大量配置時に非効率。
- **ルールデバッグ**: `RuleEvaluator` の評価結果がコンソールログに埋もれ、ルールの発火順や条件の成否を直感的に追えない。
- **ステージテスト**: 特定ステージの即時テストにはシーン切り替えやスクリプト修正が必要で、イテレーション速度が低い。

## 期待される機能 (Core Features)

### 1. マップエディタ補助 (Map Editor Assist)
- **座標指定による一括配置**: 敵やギミック（タイル属性設定）をリスト形式で複数座標に一括流し込む機能。
  - `EnemyData` ドロップダウン + 座標リスト入力 → `EnemySpawner` 相当の配置データを自動生成。
  - タイルタグ（例: Fire, Ice）のエリア塗り機能（矩形範囲選択 + タグ付与）。
- **SceneView 連携**: SceneView 上でクリックした座標をリストに追加するインタラクティブモード。
- **配置プレビュー**: 設定内容を SceneView の Gizmo / Handles で可視化し、確定前に確認できるようにする。

### 2. ルール評価リアルタイムログ (Rule Evaluation Log)
- **構造化イベント記録**: `RuleEvaluator` の各ルール評価結果（条件判定、タグ変更、対象エンティティ）を構造化データとして記録するシステム。
  - タイムスタンプ、ターン番号、フェーズ名、ルール名、条件成否（P/Q 各方向）、適用されたタグ操作を含む。
- **デザイナーパネル表示**: Debug タブに「Rule Log」サブ表示を追加。
  - フィルタリング（ルール名、ターン番号、成否）。
  - 発火したルールのハイライト表示。
  - ログのクリア / エクスポート機能。
- **パフォーマンス配慮**: 記録はリングバッファ方式とし、メモリ消費を制限する（デフォルト上限 500 件）。

### 3. ステージ即時ロード (Quick Stage Load)
- **ワンクリック遷移**: Designer Panel の Stages タブに「▶ Play」ボタンを追加。選択中の `StageConfig` に対応するステージを即時ロードして Play モードに入る。
- **ロードフロー**: `StageConfig` → 対応シーンの特定 → `EditorSceneManager` でシーンを開く → `EditorApplication.EnterPlaymode()` の自動実行。
- **前提条件チェック**: 必要なアセット（Rulebook, EnemyData 等）が StageConfig に正しく設定されているかを事前バリデーション。

## 技術的アプローチ

### マップエディタ補助
- `SceneView.duringSceneGui` を利用した座標ピッキング。
- `Handles.DrawWireCube` / `Handles.Label` による配置プレビュー描画。
- 配置データは `StageConfig` のサブアセットまたは専用の `MapPlacementData` (ScriptableObject) として永続化。

### ルール評価ログ
- `RuleEvaluator` に `IRuleEventLogger` インターフェースを導入し、評価処理に記録フックを追加。
- ランタイム側は軽量な `struct RuleEvalEvent` をリングバッファに蓄積するのみ。
- エディタ側では `EditorApplication.update` でバッファをポーリングし、IMGUI `ReorderableList` + スクロールビューで描画。

### ステージ即時ロード
- `EditorSceneManager.OpenScene` + `EditorApplication.EnterPlaymode()` の連携。
- `StageConfig` にシーンパス参照フィールドを追加（`SceneAsset` 型）。

## 該当ファイル（予定）
- `Assets/Scripts/Editor/DesignerPanel/Tabs/MapEditorTab.cs` [NEW]
- `Assets/Scripts/Editor/DesignerPanel/Tabs/DebugTab.cs` [MODIFY] — ルールログ表示追加
- `Assets/Scripts/Editor/DesignerPanel/Tabs/StageEditorTab.cs` [MODIFY] — 即時ロードボタン追加
- `Assets/Scripts/Runtime/Rules/IRuleEventLogger.cs` [NEW]
- `Assets/Scripts/Runtime/Rules/RuleEvalEvent.cs` [NEW]
- `Assets/Scripts/Runtime/Rules/RuleEvaluator.cs` [MODIFY] — ログフック追加
- `Assets/ScriptableObjects/MapPlacementData.cs` [NEW] — 配置データSO（候補）

## 確認事項 (Acceptance Criteria)
- [x] MapEditorTabがDesigner Panelで開けるか
- [x] SceneView上でセルをクリックして、敵やタグのプレビューが配置できるか（`MapPlacementData`へのデータ保存）
- [x] 配置済みのデータが、ゲーム再生時に正しくフィールドに流し込まれるか (`MapPlacementSpawner`)
- [x] ルール評価時に `DebugTab` へログがリアルタイムに表示されるか (特定のルールがどのエンティティに適用・不適用だったか)
- [x] `StageEditorTab` にPlayボタンが表示され、設定された `SceneAsset` をロードして即座にPlayModeに入れるか
- [x] 既存の Issue 7 パネル機能に影響がないこと。
