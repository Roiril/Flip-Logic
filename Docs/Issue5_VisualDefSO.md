# [COMPLETED] Issue 5: ビジュアル定義の SO 化

### タイトル
`refactor: ビジュアル定義の SO 化`

## 概要 (Overview)
- **発生箇所:** `FlipLogic.Core` (エンティティ生成/描画), `FlipLogic.Grid` (タイル描画), UI関連
- **事象:** 色やスプライト（PやEといった文字の動的出力、Fire=オレンジ等）がコード内にハードコードされており、アーティストが直接デザインを調整・変更しにくい状態になっている。

## 現在の動作 (Current Behavior)
- **[エンティティ描画]:** `EntitySpriteFactory` などを用いてスクリプトで動的に文字入りスプライトを作ってアサインしている。
- **[タイル描画]:** `TileOverlayRenderer` などで、特定の属性（例: Fire）に対する色設定がハードコードされている。
- **[UI群]:** UIの色や設定がスクリプトやプレハブに散在しており、一元管理されていない。設定によってはLegacy Textへの依存が残っている。

## 期待される動作 (Expected Behavior)
- `EntityVisualDef`, `TileVisualDef`, `UITheme` などの ScriptableObject を通じて、オブジェクトのビジュアル（スプライト、カラー、フォント等）が設定・管理されること。
- 動的なスプライト生成を廃止（あるいはフォールバック用のみに限定）し、アセットとして用意されたスプライトをSO経由で利用すること。
- プログラマがコードを修正せずとも、インスペクタやSOの数値を調整するだけでゲーム全体のトーン＆マナーを変更可能になること。

## 修正方針 (Fix Strategy)
1. `EntityVisualDef` (ScriptableObject) を作成し、エンティティの種別やIDをキーにスプライトアセットを返す仕組みを構築。
2. `TileVisualDef` (ScriptableObject) を作成し、タグの属性名などをキーにして、タイル用のカラーやパターン画像の参照を返す仕組みを構築。
3. `UITheme` (ScriptableObject) を作成し、UIの共通カラーやフォント（TextMeshPro移行も視野に）設定を一元管理する。
4. 既存の `EntitySpriteFactory` やハードコードによる色指定を各Renderer/Generatorから削除し、SOからデータを引くように置換する。

## 該当ファイル (Relevant Files)
| コンテキスト | ファイルパス | 備考 |
| :--- | :--- | :--- |
| [新規] | `Assets/Scripts/Data/EntityVisualDef.cs` |  |
| [新規] | `Assets/Scripts/Data/TileVisualDef.cs` |  |
| [新規] | `Assets/Scripts/Data/UITheme.cs` |  |
| [修正] | `Assets/Scripts/Grid/TileOverlayRenderer.cs` | SOを参照して色・アルファを決定 |
| [修正] | 各種初期化スクリプトやエンティティ生成部 | スプライト直指定部分をSO経由に変更 |

## 確認事項 (Acceptance Criteria)
- [x] 各 VisualDef (SO) がエディタから作成・編集可能であること。
- [x] タイル属性やエンティティスプライトがコードではなくSOから割り当てられていること。
- [x] ゲームを再生した際、既存の（あるいは新しい）ビジュアルがエラーなく適用されること。
- [x] `EntitySpriteFactory` のようなハードコードスプライト生成に依存していないこと。
