# Issue 2: ルール機構 (Rule Hack) のデータ駆動化

## 1. 現状の課題
- **ハードコード**: `GameManager.SetupTestRules()` 内で `RuleData` インスタンスを直接生成しており、ルールの追加・変更にコード修正が必要。
- **データ構造の分離不足**: `RuleData` が POCO (Plain Old CLR Object) であり、インスペクター上での単体編集やアセット化が困難。
- **バリデーション不在**: `TagCondition` や `TagEffect` で使用される文字列（Key, Value）が、Issue 1 で導入した `TagKeyRegistry` と連携していない。
- **ページ管理の固定化**: `RulePage`（章）の構成が動的に変更できず、デザイナーが章立てを調整しにくい。

## 2. リファクタリング方針

### A. ScriptableObject によるアセット化
- `RuleData` をラップする `RuleAsset` (ScriptableObject) を作成。
- `RulePage` をラップする `RulePageAsset` (ScriptableObject) を作成。
- 最終的なルール一式を管理する `RulebookAsset` (ScriptableObject) を作成。

### B. RulebookManager の改修
- 起動時に `RulebookAsset` を読み込み、ランタイム用の `RuleData` インスタンスを生成する仕組みに変更。

### C. バリデーションの統合
- `TagCondition` および `TagEffect` のインスペクター表示またはコンストラクタにおいて、`TagKeyRegistry` を用いたバリデーション（警告表示）を導入。

## 3. 実装ステップ

### ステップ 1: SO クラスの定義
- [ ] `RuleAsset.cs`: 個別ルールの定義アセット。
- [ ] `RulePageAsset.cs`: 章単位のルールリスト管理アセット。
- [ ] `RulebookAsset.cs`: ゲーム全体のルールセット管理アセット。

### ステップ 2: RulebookManager への統合
- [ ] `RulebookManager` が `RulebookAsset` を受け取り、初期化できるように変更。

### ステップ 3: GameManager のクリーンアップ
- [ ] `GameManager.SetupTestRules()` を廃止し、アセットからロードする方式に移行。

### ステップ 4: バリデーション対応
- [ ] `PropositionData.cs` 内の各クラスに `TagKeyRegistry` による検証を追加。

## 4. 期待される効果
- デザイナーがコードに触れることなく、Unity Editor 上で新しいルールや「章」を作成・調整できる。
- タイポ等による「動作しないルール」を事前に検知できるようになる。
