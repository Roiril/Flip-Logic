# Issue 1: タグシステムの型安全化とデータ駆動化

### タイトル
`refactor: タグシステムの型安全化とデータ駆動化`

## 概要 (Overview)
* **発生箇所:** `FlipLogic.Core` のタグシステム (`TagDefinition`, `TagContainer`, `TagBehaviorRunner`)
* **事象:** タグのKeyとValueがシステム全体に単なる文字列（マジック文字列）として散在しており、タイプミスによる不具合誘発や新タグ追加時の保守性が著しく低下している。また、タグに紐づく挙動（絶対法則）がスクリプト内でハードコードされており、ゲームデザイナーがエディタから自由に調整・追加できない状態になっている。

## 現在の動作 (Current Behavior)
* **[タグの定義と付与]:**  `new TagDefinition("Element", "Fire", 0, "Source")` のように、どこからでも任意の文字列でタグを生成・付与できてしまう。
* **[タグの振る舞い実行]:** `TagBehaviorRunner.cs` 内で `TagBehaviorDef` がハードコードで1件(`InstantDeath`)登録されており、追加するにはスクリプト修正が必須。
* **[未実装の振る舞い]:** `TagBehaviorType` には `DealDamage`, `Heal`, `AddTag` などが定義されているが、`ApplyEffect` における分岐実装は `SetHpZero` しか存在しない。

## 期待される動作 (Expected Behavior)
* `TagKeyRegistry` (ScriptableObject) を用いて、使用可能な Key と Value のリストがマスタ管理され、エディタおよびコード上で型安全に参照できること。
* `TagBehaviorDef` が ScriptableObject 化され、ゲームデザイナーが Unity Inspector から「どのタグがついたら」「何の条件で」「どういう効果を発揮するか」をノンコーディングで定義できること。
* `TagBehaviorRunner` が、`TagBehaviorType` の定義済み全効果を正しく実行できる状態になっていること。

## 原因分析 (Root Cause Analysis)
* **[アーキテクチャ設計]**: データ駆動 (Data-Driven) を想定すべき設定群が、初期プロトタイピングの名残でクラス内に直書きされている。型の制約が存在しないため、存在しない Key や Value へのアクセスがコンパイルエラーにならず、無音でのバグを生み出す。
* コード例:
  ```csharp
  // Assets/Scripts/Tutorial/TutorialSetup.cs:65
  // マジック文字列による不安全なタグ付与
  _enemyEntity.Tags.AddTag(new TagDefinition("Element", "Ice", 0, "Nature"));
  ```

## 該当ファイル (Relevant Files)
| コンテキスト | ファイルパス | 備考 |
| :--- | :--- | :--- |
| [データ定義] | `Assets/Scripts/Core/TagDefinition.cs` | 文字列管理からの脱却と Duration 指定の明確化 |
| [挙動定義] | `Assets/Scripts/Core/TagBehaviorDef.cs` | ScriptableObject 化と項目の整理 |
| [挙動実行] | `Assets/Scripts/Core/TagBehaviorRunner.cs` | 未実装の列挙型処理を全て追加、SOの読み込み機構 |
| [登録マスタ] | `Assets/Scripts/Data/TagKeyRegistry.cs` (新規) | 全タグKey/Valueの一元管理用SOクラス作成 |
| [参照箇所全般] | `BattleManager`, `TutorialSetup`, `GridMap` 等 | リファクタリングによるコンパイルエラー箇所の修正 |

## 修正方針 (Fix Strategy)
1. `Assets/Scripts/Data/TagKeyRegistry.cs` (ScriptableObject) を作成し、許容される Key と Value のリストを定義する。
2. `TagBehaviorDef.cs` を ScriptableObject を継承するように改修する。
3. `TagDefinition` のコンストラクタおよびプロパティを修正し、文字列直打ちではなく `TagKeyRegistry` によって検証済み（または定数/Enumベース）の値を扱うように変更する。
4. `TagBehaviorRunner` の `ApplyEffect` メソッド内に、`TagBehaviorType` の全バリエーションの処理（回復、ダメージ、タグ付与・剥奪）を実装する。
5. エラーとなった既存の呼び出し箇所を新しい API に合わせて修正する。

## 比較表 (Comparison Matrix)
| 機能 / 条件 | 観点 | 状態 |
| :--- | :--- | :--- |
| **[タグの新規追加]** | C#スクリプトの直接編集 | ❌ 異常 (デザイナーが触れない) |
| **[タグの新規追加]** | Inspectorからの SO 編集のみ | ✅ 正常 (作業の分担・効率化) |
| **[タグ入力時のミス]** | 実行時まで気付かない / 無音で失敗 | ❌ 異常 |
| **[タグ入力時のミス]** | エディタのドロップダウン等で制約される | ✅ 正常 (堅牢性の確保) |

## 確認事項 (Acceptance Criteria)
- [ ] [タグマスタ機能]: Unity Inspector 上で `TagKeyRegistry` に新しいキー・バリューを追加できること。
- [ ] [振る舞い定義機能]: Unity Inspector 上で `TagBehaviorDef` SO を新規作成し、各種パラメータを設定できること。
- [ ] [振る舞い実行]: テストシーンにおいて、新しく作成した `Heal` や `DealDamage` などのタグ効果がターン終了時に正しく発動すること。
- [ ] [リグレッション]: 既存のシナリオ（チュートリアル等）やバトルにおける即死タグ (`InstantDeath`) の挙動が壊れていないこと。
