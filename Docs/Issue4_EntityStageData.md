# Issue 4: エンティティ生成のデータ駆動化とステージ構成のハイブリッド化

### タイトル
`refactor: エンティティ生成のデータ駆動化とステージ構成のハイブリッド化`

## 概要 (Overview)
- **発生箇所:** `FlipLogic.Core`, `FlipLogic.Explore`, `FlipLogic.Tutorial`, `FlipLogic.Data`
- **事象:** 敵の生成やステージのセットアップが `TutorialSetup` や `ProceduralMapGenerator` にハードコードされている。`EnemyData` SOが存在するが、実際のエンティティ生成に活用されていない。加えて `EnemySymbol.DoChase` と `EncounterTrigger.CheckEncounter` に Issue 3 で排除すべきだった `FindObjectsByType` が残存している。

## 現在の動作 (Current Behavior)
- **[敵生成]:** `TutorialSetup` でインスペクタ配置済みの `GameEntity` に対し、タグやステータスをコードで直接設定。`EnemyData` SOは使われていない。
- **[ステージ構築]:** `ProceduralMapGenerator` で壁座標をハードコード（`map[4,3]=1` 等）。マップサイズも固定値。
- **[検索残存]:** `EnemySymbol.DoChase` が `FindObjectsByType<PlayerController>()` を毎ターン呼び出し。`EncounterTrigger` も同様に `FindObjectsByType<EnemySymbol>()` を使用。

## 期待される動作 (Expected Behavior)
- `EntityFactory` が `EnemyData` SOをもとに敵エンティティを動的生成し、ステータスと初期タグが自動適用されること。
- ステージ構成は **ハイブリッド方式**：
  - **メタ情報** (`StageConfig` SO): ステージ名、使用ルールブック等
  - **地形**: Tilemap（既存のまま）またはScene上で配置
  - **敵・ギミック配置**: `EnemySpawner` / `CellTagSetter` コンポーネントをScene上に直接配置
- `FindObjectsByType` の残存箇所が `EntityRegistry` 経由に置換されること。

## 修正方針 (Fix Strategy)

### Phase A: EntityFactory と FindObjectsByType 排除
1. `EntityFactory` を実装。`EnemyData` と座標を引数に取り、Prefabのインスタンス化〜`GameEntity`初期化〜`EnemySymbol` 付与〜初期タグ流し込みを一括処理。
2. `EnemySymbol.DoChase` の `FindObjectsByType<PlayerController>` → `EntityRegistry.Instance.GetEntities(EntityType.Player)` に置換。
3. `EncounterTrigger.CheckEncounter` の `FindObjectsByType<EnemySymbol>` → `EntityRegistry.Instance.GetEntities(EntityType.Enemy)` に置換。

### Phase B: Scene配置用コンポーネント
4. `EnemySpawner` (MonoBehaviour) 作成。`EnemyData` 参照を持ち、`Start()` で `EntityFactory` 経由の敵生成後に自身を破棄。
5. `CellTagSetter` (MonoBehaviour) 作成。`TagDefinition` リストを持ち、`Start()` で `GridMap` にタグ付与後に自身を破棄。
6. `StageConfig` (ScriptableObject) 作成。ステージ名、`RulebookAsset` 参照などメタ情報のみ保持。

### Phase C: チュートリアルへの適用（軽量）
7. `TutorialSetup` の敵初期化部分を `EnemySpawner` + `EntityFactory` に移行。

## 該当ファイル (Relevant Files)
| コンテキスト | ファイルパス | 備考 |
| :--- | :--- | :--- |
| [新規] | `Assets/Scripts/Core/EntityFactory.cs` | EnemyData → GameEntity 生成 |
| [新規] | `Assets/Scripts/Explore/EnemySpawner.cs` | Scene配置型スポナー |
| [新規] | `Assets/Scripts/Grid/CellTagSetter.cs` | Scene配置型タグ付与 |
| [新規] | `Assets/Scripts/Data/StageConfig.cs` | ステージメタ情報SO |
| [修正] | `Assets/Scripts/Explore/EnemySymbol.cs` | FindObjectsByType排除 |
| [修正] | `Assets/Scripts/Explore/EncounterTrigger.cs` | FindObjectsByType排除 |

## 確認事項 (Acceptance Criteria)
- [ ] `EntityFactory.Create(enemyData, gridPos)` で敵が正しく生成され、ステータスとタグが `EnemyData` と一致すること。
- [ ] `EnemySpawner` をScene上に配置し再生すると、指定位置に敵が出現すること。
- [ ] `CellTagSetter` をScene上に配置し再生すると、その座標のセルにタグが付与されること。
- [ ] `FindObjectsByType` がスクリプト内から完全に排除されていること。
- [ ] 既存のチュートリアルフローが破綻しないこと。
