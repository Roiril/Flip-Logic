# Issue 4: エンティティおよびステージデータ化の再評価 (Re-evaluation)

Issue 4（エンティティおよびステージのデータ化）を「本当に今やるべきか」「どのようなアプローチが最適か」という視点で再チェックしました。

## 結論
**やるべきですが、当初予定していた「StageData（ScriptableObject）への一元化」は Unity の思想（コンポーネント指向・ビジュアルエディタ）に反するため、アプローチを変更すべきです。**

## 分析と理由

### 1. 敵の生成 (EnemyData の活用)
* **現状**: `EnemyData` SO は存在しますが、`TutorialSetup` 等で設定が直接上書きされておりデータとして機能していません。
* **評価**: **必須 (P1)**。RPG/ストラテジーにおいて、敵のベースステータスや初期タグを SO (`EnemyData`) で管理し、それを元にエンティティを動的生成（`EntityFactory`）する仕組みは、バランス調整のために不可欠です。

### 2. ステージの構築 (StageData SO vs Scene + Component)
* **当初のIssue 4案**: `StageData` (ScriptableObject) を作成し、壁の座標、敵の座標、イベントマスの座標を数値リストとして保持する。
* **問題点**: 
  * 座標を数値でポチポチ入力するのは**全くデザイナーフレンドリーではありません**。
  * すでに `GridMap` が `Tilemap` に対応しているため、Unity の Scene 上で直接タイルを塗るほうが直感的です。
* **新しい提案 (Scene-based Approach)**:
  * **EnemySpawner (MonoBehaviour)**: `EnemyData` をセットして Scene 上に配置すると、開始時に敵を生成するコンポーネント。
  * **CellTagSetter (MonoBehaviour)**: 「火属性」などのタグ情報を持ち、Scene 上に配置しておくと開始時にそのマスの `GridMap` にタグを付与して自身は消えるコンポーネント。
  * **StageController (MonoBehaviour)**: そのステージ固有の `RulebookAsset` などを設定し、初期化を担うコンポーネント。

## 修正後の実行計画 (Revised Plan for Issue 4)

この方針でIssue 4を進める場合、以下のステップで実装します。

1. **`EntityFactory` の実装**
   - `EnemyData` を元に `GameEntity` プレハブを生成し、ステータスと初期タグを流し込む処理を作成。
2. **ビジュアル配置用コンポーネントの作成**
   - `EnemySpawner.cs`: 配置した座標に `EntityFactory` 経由で敵をスポーンさせる。
   - `CellTagSetter.cs`: 配置した座標のセルに指定の `TagDefinition`（属性など）を付与する。
   - `StageController.cs`: ステージ開始時のルール読み込み（`RulebookAsset`）等を統括。
3. **チュートリアルシーンの移行**
   - 現在コードで無理やり生成している `ProceduralMapGenerator` や `TutorialSetup` の初期配置ロジックを非推奨化（または削除）。
   - 代わりに、実際の Scene 上に `EnemySpawner` などを配置して、コンポーネントとデータ（SO）の組み合わせでチュートリアルステージが構築されるようにリファクタリングする。

---
**ユーザーへの確認事項:**
ScriptableObjectにステージの**座標データ**まで持たせる巨大な「StageData」を作るよりも、UnityのSceneViewを使って直感的に敵やギミックを配置できる「コンポーネントベース」の方が、後続のIssue 7（デザイナー用パネル作成）にも繋がりやすいと考えます。
この「Scene配置型」への計画変更で進めてよろしいでしょうか？
