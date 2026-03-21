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
- 各タグに紐づく `EntityVisualDef` / `TileVisualDef` の一括設定・プレビュー。
- 新規タグの追加とマスタSOへの自動登録。

### 2. Rule Editor Panel (ルール構築)
- `RuleAsset` のノードベースまたはフォームベースの構築GUI。
- 命題（P/Q）の論理演算、タグ条件のプルダウン選択。
- 特定のルール適用時における挙動（LogicState）の簡易シミュレーション/プレビュー。

### 3. Enemy & Stage Editor Panel (データ設定)
- `EnemyData` のステータス（HP, ATK等）と初期付与タグの設定。
- `StageConfig` のメタデータ構築（背景、BGM、クリア条件等）。
- マップエディタ補助（特定座標への敵・ギミックの一括流し込み機能）。

### 4. Play Test & Debug Panel (デバッグ)
- 実行中ルールの評価ログ（どのルールがなぜ発動したか）のリアルタイム表示。
- 指定したステージの即時ロードボタン。
- プレイヤー/敵へのデバッグ用タグ付与・削除ツール。

## 技術的アプローチ
- **UI Toolkit**: Unity 2021/2022以降の標準である UI Toolkit (UXML/USS) を全面的に採用し、レスポンシブで保守性の高いパネルにする。
- **Service Locator**: `EntityRegistry` 等のランタイムAPIと連携し、実行時デバッグ機能を統合する。

## 該当ファイル
- `Assets/Scripts/Editor/DesignerPanel/DesignerPanelWindow.cs`
- `Assets/UI/Editor/DesignerPanel.uxml`
- `Assets/UI/Editor/DesignerPanel.uss`

## 確認事項 (Acceptance Criteria)
- [ ] `Window > Flip Logic > Designer Panel` からウィンドウが開けること。
- [ ] タブ切り替え（Tags, Rules, Enemies, Stages, Debug）が機能すること。
- [ ] 各パネルから対象の ScriptableObject が正しく検索・表示されること。
- [ ] パネル上での変更がアセットに即座に反映（または保存ボタンで反映）されること。
