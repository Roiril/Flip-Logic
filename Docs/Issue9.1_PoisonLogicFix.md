# Issue 9.1: 毒ロジックの修正とタグ登録の追加

## 概要 (Overview)
Issue 9 の実装後、テストプレイ中に判明した以下の問題を修正する。
1. **スワップ時の移動不具合**: ルールをスワップ（NOT ダメージ → 毒沼へ移動）させた際、正しい地形タグ（`Element: Poison`）を検索できず移動が発生しない。
2. **タグ未登録警告**: `Trigger: Damage` タグが `TagKeyRegistry` に登録されていないため、実行時に警告ログが表示される。

## 修正内容 (Changes)

### 1. タグ登録の追加
- **対象**: `Assets/Resources/TagKeyRegistry.asset`
- **内容**: `Trigger: Damage` を許容されるタグとして登録する。

### 2. 移動先タグの明示指定機能
- **対象**: `Assets/Scripts/Data/PropositionData.cs`
- **内容**: `TagCondition` クラスに `MoveTargetKey` / `MoveTargetValue` フィールドを追加。
- **反映**: `RuleData.ConvertConditionToEffect` にて、上記フィールドが設定されている場合はスワップ後の移動先タグとして優先採用するように修正。

### 3. ルールアセットの更新
- **対象**: `Assets/Data/Rules/Stage1/rule_poison_damage.asset`
- **設定**: `TagConditionP` に `MoveTargetKey: "Element"`, `MoveTargetValue: "Poison"` を設定。

## 検証項目 (Verification)
- 起動時に `Trigger: Damage` に関するログ警告が出ないこと。
- ルールを「スワップ + 条件反転」にした状態でターン終了時、正常に最寄りの毒沼タイルへ移動すること。
