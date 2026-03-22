# Issue 9: 毒システムの再定義とルール結合の柔軟化（アセットベース実装）

## 概要 (Overview)
- **目的**: 毒状態（Status）とダメージ（TagBehavior）を分離し、ルールを介して結合することで、デザイナーがより複雑で動的なギミック（例：ダメージを受けていない者を毒沼へ引き寄せる）を構築できるようにする。
- **対象**: `Status: Poison`, `element: Poison`, `rule_poison_swamp.asset`
- **実装方針**: 既存の `RuleEvaluator` エンジンの機能をフル活用し、**コード変更なし、アセット操作のみ**で実現する。

---

## 背景 (Background)
現在の毒システムは、`Status: Poison` タグ自体に `PoisonDamage` 挙動が紐付いており（`BehaviorId`設定済み）、以下の制約がある：
1. **結合の固定**: 毒状態＝ダメージが直結しているため、「毒状態だがダメージは受けず、別の効果（鈍足など）が出る」といったバリエーションが作りにくい。
2. **ルールの単調さ**: ルール改変（スワップや否定）を行った際の影響範囲が「毒状態」という1つの概念に閉じてしまい、パズル要素としての広がりが限定的。

---

## 期待される機能 (Core Features)

### 1. タグと挙動のデカップリング
- `Status: Poison` 自体は単なる「状態フラグ」とする。
- 新たに `Trigger: Damage`（1ダメージを受ける挙動を持つタグ）を導入する。

### 2. 結合ルールの新設
- ステージ1の基本ルールを以下のように再編する：
  - **ルールA**: `Tile: Element: Poison` (毒沼) → `Entity: Status: Poison`
  - **ルールB**: `Entity: Status: Poison` → `Entity: Trigger: Damage`
- これにより、一方のルールを改変しても、もう一方の性質（毒沼の定義やダメージの性質）を独立して操作可能になる。

---

## 技術的アプローチ (Technical Approach)

本件は既存のルールエンジンの機能範囲内であり、**C#スクリプトの修正は不要**。

### 1. アセット操作
- **[MODIFY] `rule_poison_swamp.asset`**: 
    - 結果Qの `BehaviorId` を削除し、単なる `Status: Poison` 付与に留める。
- **[NEW] `rule_poison_damage.asset`**: 
    - 条件P: `Entity: Status: Poison`
    - 結果Q: `Entity: Trigger: Damage`
- **[NEW] `DamageTrigger.asset` (TagBehaviorDef)**:
    - `BehaviorId`: `DamageTrigger`
    - `Effect`: `DealDamage` (1)
    - `Trigger`: `TurnEnd`

### 2. ステージ・ルールページへの登録
- `RulePageAsset` に `rule_poison_damage` を追加し、ステージ開始時に有効化されるようにする。

---

## 確認事項 (Acceptance Criteria)
- [ ] 毒沼にいる時に `Status: Poison` が付与されるか。
- [ ] `Status: Poison` がある時に `Trigger: Damage` が付与され、HPが減るか。
- [ ] ルールBを「入れ替え＋条件否定」にした際、ダメージを受けていないエンティティが毒沼へ転移するか。
- [ ] 毒状態が解除された際、転移とダメージが正しく停止するか。
