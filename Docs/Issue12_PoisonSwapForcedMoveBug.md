# Issue 12: 毒沼強制移動ルールの不具合修正

## 概要 (Overview)
「継続ダメージを受けない -> 毒沼にいる (Status: Poison)」というルールにスワップしてターン経過を待っても、ダメージを受けていない敵が毒沼へ強制移動しない不具合の原因と修正方針をまとめる。

## 原因 (Causes)

コード調査の結果、根本的な原因が2点見つかりました。

1. **RuleEvaluator内の移動処理が不完全**
   - `RuleEvaluator.cs` の `effect.IsMoveToNearest` 分岐内において、`entity.GridPosition = nearest;` と内部データを書き換える処理しか行われていません。
   - `GridMap` への再登録 (`GridMap.Instance.RegisterEntity`) を行っていないため、マップデータ上は元の位置に留まっていることになっています。
   - `transform.position` など視覚的な位置を同期する処理 (`Entity.SyncWorldPosition()` や `EnemySymbol.ForceMoveTo()`) を呼び出していないため、ゲーム画面上でも一切移動が反映されません。

2. **SubjectFilterP（主語フィルタ）の未設定時の判定バグ**
   - `rule_poison_damage.asset` などのアセットで `SubjectFilterP` の `Key` が空文字列（未設定）のまま保存されています。
   - `RuleEvaluator.cs` 側で `if (rule.SubjectFilterP != null)` と判定しているため、インスタンスが存在するとフィルタリングの評価に進んでしまいます。
   - `Key` が空の場合、`HasTag("", "")` が `false` を返し、空のフィルタによってすべてのエンティティが「条件不一致」として除外処理されてしまう可能性があります（あるいは他のルールも阻害している恐れがあります）。

## 修正内容 (Planned Fixes)

1. **RuleEvaluatorの移動処理の拡充**
   - 内部位置を更新するだけでなく、`GridMap` への再登録を行う。
   - `EnemySymbol` などのコンポーネントがあればその `ForceMoveTo()` を呼び、ビジュアルと内部の同期を正常に行う。

2. **空のSubjectFilterPを無視するガード処理の追加**
   - `RuleEvaluator.cs` の条件判定を `if (rule.SubjectFilterP != null && !string.IsNullOrEmpty(rule.SubjectFilterP.Key))` に修正し、空フィルタの場合は検証をスキップするようにする。

## 確認事項 (Acceptance Criteria)
- [ ] スワップしたルール「継続ダメージを受けない -> 毒沼にいる」を有効にした際、ターン終了時にダメージを受けていない敵が毒沼へ強制移動する。
- [ ] 移動後、その位置として正しくエンティティ同士の衝突処理やインタラクトが機能する。
- [ ] 敵シンボルがスムーズに（あるいは即座に）移動アニメーションを行う。
