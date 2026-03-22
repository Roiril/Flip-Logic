# Issue 11: ルールシステムの刷新 — フィールド常設化とターン統合

## 概要 (Overview)
ルール改変を「バトル中のみ・1回限り」から「フィールド上でいつでも・何度でも」に変更する。
バトルとフィールドのターンを統合し、ルールの効果が両方で一貫して機能するようにする。

## 現状の問題点
1. **ルール改変がバトル限定**: `RuleHackPanelController` はバトルUIの一部で、1バトルにつき1回・1ルールのみ操作可能。
2. **ターンが分離**: フィールド(`TurnManager`)とバトル(`BattleManager`)が独立したターン概念を持つ。バトル中はフィールドのターン進行が停止する。
3. **ルール評価は既に共有済み**: `TurnResolutionProcessor.ExecuteAsync()` がバトル・フィールド両方から呼ばれており、`RuleEvaluator` のルールリストは共通。**この部分は変更不要。**

## 変更内容

### 1. ルール看板オブジェクト（フィールド常設）
- フィールド上に「ルールボード」オブジェクトを配置する。
- プレイヤーが近づいてインタラクトすると、全アクティブルールを一覧表示する。
- 各ルールに対して、否定トグル・スワップ操作を自由に行える。
- **回数制限なし**: 何度でも参照・改変可能とする。
- ルールボードはステージごとに `MapPlacementData` または専用データで配置位置を定義する。

#### 必要なコンポーネント
- **[NEW] `RuleBoardInteractable.cs`**: フィールド上のルール看板オブジェクト。プレイヤー接近時にインタラクト可能。
- **[NEW] `RuleBoardUIController.cs`**: 全ルール一覧表示と改変操作のUI。
- **[MODIFY] `MapPlacementData.cs`**: ルールボードの配置位置を定義するフィールドを追加。

### 2. ターンの統合（バトルとフィールドの共有）
- バトル中もフィールドの `TurnManager` を使用する。
- プレイヤーの行動（攻撃/防御/逃走）= 1ターン経過。
- 敵の行動 = 1ターン経過。
- ターン末のルール評価は `TurnResolutionProcessor` の既存パイプラインをそのまま利用する。
- `BattleManager` の独自ターン管理（`DoTurnEndAsync` 内の `TurnResolutionProcessor` 直接呼び出し）を `TurnManager` 経由に統合する。

#### 必要な変更
- **[MODIFY] `TurnManager.cs`**: バトル中の一時停止ロジック（L65-68）を削除し、バトル行動もターンとしてカウントする。
- **[MODIFY] `BattleManager.cs`**: `DoTurnEndAsync` の `TurnResolutionProcessor.ExecuteAsync()` 直接呼び出しを、`TurnManager` へのターン進行通知に置き換える。
- **[MODIFY] `EncounterTrigger.cs`**: バトル開始時にルールを渡す処理を削除（バトル専用ルールの概念を廃止）。

### 3. バトル中のルール改変の廃止
- `BattleManager` の `OpenRulebook` コマンドを削除する。
- `RuleHackPanelController` はバトルUIから切り離し、ルールボードUI専用にリファクタリング（または新規作成）。
- バトルUI上のルールブックボタンを「ルール確認（読み取り専用）」に変更する案もある。

#### 必要な変更
- **[MODIFY] `BattleManager.cs`**: `OpenRulebook` / `_hasUsedRulebook` / `_activeRule` を削除。
- **[MODIFY] `BattleUIController.cs`**: ルールハックボタンの削除またはルール閲覧専用化。

## 設計上のポイント

### ルールボードの配置
```
[ルールボード] ← プレイヤーが近づくとUI表示
  ├── ルール1: 毒沼にいるならば → 毒状態になる [スワップ|否定]
  ├── ルール2: 毒状態であるならば → ダメージを受ける [スワップ|否定]
  └── ...
```

### ターン統合後のフロー
```
フィールド:
  プレイヤー移動 → ターン+1 → 敵行動 → ターン+1 → ルール評価 → タグ更新

バトル中:
  プレイヤーコマンド(攻撃等) → ターン+1 → 敵攻撃 → ターン+1 → ルール評価 → タグ更新
```

### 変更しないもの
- `RuleEvaluator` / `TurnResolutionProcessor` / `RulebookManager`: ルール評価パイプラインには変更なし。
- `RuleData` / `PropositionData`: データ構造そのものは変更なし。
- `TagBehaviorRunner` / `TagKeyRegistry`: タグシステムは変更なし。

## タスク一覧
- [x] Phase 1: ルールボード（フィールド常設UI）
    - [x] `RuleBoardInteractable` の作成
    - [x] `RuleBoardUIController` の作成（全ルール一覧 + 改変操作）
    - [x] `MapPlacementData` への配置定義追加
    - [x] Stage1 への配置
- [x] Phase 2: ターン統合
    - [x] `TurnManager` のバトル中停止ロジック削除
    - [x] `BattleManager` のターン処理を `TurnManager` 経由に変更
    - [x] `EncounterTrigger` からルール受け渡し処理の削除
- [x] Phase 3: バトルUI整理
    - [x] `BattleManager` からルールハック関連コードの削除
    - [x] バトルUI からルール改変ボタンの削除（または閲覧専用化）
- [x] 検証
    - [x] フィールド上のルールボードでルール改変が機能すること
    - [x] バトル中にルール効果が正しく適用されること
    - [x] ターンカウントがバトル・フィールドで一貫していること
