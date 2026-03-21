# Issue 7: フリップロジック・デザイナーパネル (EditorWindow)

## 概要 (Overview)
- **目的:** ゲームデザイナーがC#コードを直接編集することなく、ゲーム内のデータ（タグ、ルール、敵、ステージ）を統合的に管理・構築できる専用ツールを構築する。
- **対象:** `FlipLogic.Editor.DesignerPanel`

## 背景 (Background)
現在の開発フローでは、ScriptableObjectを個別に作成・設定する必要があり、特に関係性の深い「タグ」と「ビジュアル」、「ルール」と「タグ」の設定においてミスが発生しやすい。
また、ステージ配置や敵のパラメータ調整をシーンビューのみで行うのは非効率であるため、専用のGUIパネルが必要となっている。

## 期待される機能 (Core Features)

### 1. Tag Registry Panel (タグ管理)
- `TagKeyRegistry` に登録されているタグの一覧表示。
- 各タグの Key / AllowedValues のインライン編集。
- 新規タグの追加とマスタSOへの自動登録。

### 2. Rule Editor Panel (ルール構築)
- `RuleAsset` のフォームベース構築GUI。
- 命題（P/Q）の肯定形・否定形テキスト編集。
- タグ条件・タグ操作の `TagKeyRegistry` 連動ドロップダウン選択。
- 現在の LogicState の読み取り専用プレビュー。
- 新規 `RuleAsset` の作成ボタン。

### 3. Enemy & Stage Editor Panel (データ設定)
- `EnemyData` のステータス（HP, ATK等）と初期付与タグの設定。
- `StageConfig` のメタデータ編集（ステージ名、ルールブック、マップサイズ、難易度）。
- 新規SO作成ボタン。

### 4. Debug Panel (軽量版)
- Play中のみ動作。
- `EntityRegistry` からの全エンティティ一覧とタグ表示。
- 選択エンティティへのタグ手動付与・削除。
- `RulebookManager.GetActiveRules()` のアクティブルール一覧表示。

> [!NOTE]
> 以下の機能は **Issue 8** に先送りとした:
> - マップエディタ補助（座標への敵・ギミックの一括流し込み）
> - ルール評価のリアルタイムログ表示
> - 指定ステージの即時ロードボタン

## 技術的アプローチ
- **IMGUI (EditorGUILayout)**: `SerializedObject`/`SerializedProperty` によるUndo/Redo対応、`ReorderableList` によるリスト編集。既存のInspectorと統一された操作感を提供する。
- **Service Locator**: `EntityRegistry` 等のランタイムAPIと連携し、Play中デバッグ機能を統合する。

## 該当ファイル
- `Assets/Scripts/Editor/DesignerPanel/DesignerPanelWindow.cs`
- `Assets/Scripts/Editor/DesignerPanel/Tabs/TagRegistryTab.cs`
- `Assets/Scripts/Editor/DesignerPanel/Tabs/RuleEditorTab.cs`
- `Assets/Scripts/Editor/DesignerPanel/Tabs/EnemyEditorTab.cs`
- `Assets/Scripts/Editor/DesignerPanel/Tabs/StageEditorTab.cs`
- `Assets/Scripts/Editor/DesignerPanel/Tabs/DebugTab.cs`

## 確認事項 (Acceptance Criteria)
- [x] `Window > Flip Logic > Designer Panel` からウィンドウが開けること。
- [x] タブ切り替え（Tags, Rules, Enemies, Stages, Debug）が機能すること。
- [x] 各パネルから対象の ScriptableObject が正しく検索・表示されること。
- [x] パネル上での変更がアセットに即座に反映（Undo/Redo対応）されること。
- [x] 既存のランタイムロジックに一切影響しないこと（Editor名前空間に閉じていること）。
