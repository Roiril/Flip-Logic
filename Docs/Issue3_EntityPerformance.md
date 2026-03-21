# Issue 3: エンティティ管理の一元化と検索の高速化

### タイトル
`refactor: エンティティ管理の一元化と検索の高速化`

## 概要 (Overview)
* **発生箇所:** `FlipLogic.Core` の `GameEntity` および `FlipLogic.Grid` の `GridMap`
* **事象:** 特定のエンティティ（プレイヤーや特定の敵）を検索する際、Unityの `FindObjectsByType` 等の低速なAPIに依存している。また、`GridMap` におけるエンティティの登録解除処理（`UnregisterEntity`）が全セルを走査する O(N) 実装となっており、大規模なマップでパフォーマンスが著しく低下する。

## 現在の動作 (Current Behavior)
* **[検索方式]:** `GameManager.cs` 等で、静的参照がない場合に `FindAnyObjectByType<TurnManager>()` や `FindGameObjectWithTag("Player")` を毎度実行している。
* **[登録解除]:** `GridMap.UnregisterEntity` が `Dictionary<Vector2Int, List<GameEntity>>` を全件ループして削除対象を探している。

## 期待される動作 (Expected Behavior)
* `EntityRegistry` によって、全エンティティが O(1) で型/種別/IDごとに即座に取得できること。
* `GameEntity` の有効化・無効化に伴い、管理リストへの追加・削除が自動で行われること。
* `GridMap` の位置ベース検索が、エンティティ側が持つ現在座標を利用して O(1) で完結すること。

## 原因分析 (Root Cause Analysis)
* **[スケーラビリティの欠如]**: プロトタイプ段階では個体数が少なかったため、単純な Dictionary 走査や Unity 検索 API で問題にならなかったが、アーキテクチャ分析の結果、将来的なステージ拡大や演出の複雑化に耐えられないことが判明した。

## 該当ファイル (Relevant Files)
| コンテキスト | ファイルパス | 備考 |
| :--- | :--- | :--- |
| [一元管理] | `Assets/Scripts/Core/EntityRegistry.cs` (新規) | 高速検索用レジストリ |
| [自動登録] | `Assets/Scripts/Core/GameEntity.cs` | ライフサイクルイベントの統合とステータス分離 |
| [検索最適化] | `Assets/Scripts/Grid/GridMap.cs` | 登録解除ロジックの修正 |
| [参照修正] | `GameManager.cs`, `BattleManager.cs` 等 | `EntityRegistry` 経由の取得へ移行 |

## 修正方針 (Fix Strategy)
1. シングルトンの `EntityRegistry` を作成し、`List<GameEntity>` (EntityType別) と `Dictionary<int, GameEntity>` (ID別) を用意する。
2. `GameEntity.OnEnable`/`OnDisable` で `EntityRegistry` の `Register`/`Unregister` を呼び出す。
3. `GameEntity` に `_lastGridPosition` を保持させ、`GridMap.UnregisterEntity` 時にそれを利用して Dictionary の特定キーへ直接アクセスする。

## 比較表 (Comparison Matrix)
| 操作 | 従来の計算量 | 修正後の計算量 | 備考 |
| :--- | :--- | :--- | :--- |
| **全エンティティ検索** | O(Scene_Objects) | O(1) | `EntityRegistry` 参照 |
| **特定位置の解除** | O(Total_Cells) | O(1) | 座標メモリアクセス |
| **種別ごとの一括取得** | O(Total_Entities) | O(1) | キャッシュ済みリスト |

## 確認事項 (Acceptance Criteria)
- [ ] [自動登録]: エンティティを Instantiate/Destroy した際、`EntityRegistry` のカウントが期待通り増減すること。
- [ ] [高速検索]: `EntityRegistry.GetEntities(EntityType.Enemy)` で全敵リストが正しく取得できること。
- [ ] [リグレッション]: 既存の移動、攻撃、ルール適用が正常に動作し、「消えるはずの敵が残る」「いないはずの場所に中身がある」等の不整合が起きないこと。
