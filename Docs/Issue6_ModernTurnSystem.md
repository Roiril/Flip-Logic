# Issue 6: ターン処理の非同期化と解決フェーズの分離

### タイトル
`refactor: ターン進行の非同期パイプライン化と重複ロジックの解消`

## 概要 (Overview)
- **発生箇所:** `FlipLogic.Core.TurnManager`, `FlipLogic.Battle.BattleManager`
- **事象:** 
  - フィールド（`TurnManager`）とバトル（`BattleManager`）でターン終了時処理（ルール評価やタグの期限処理）が二重実装されている。
  - フィールド進行は完全同期（イベント一斉発火）であり、敵の移動や死亡演出、ルール発動エフェクトなどをシステムとして待機できない。
  - バトル進行はUIのコールバックを利用した擬似的なステップ進行になっており、見通しが悪い。

## 根本的な課題 (Root Causes)
1. **演出待機機構の不在**: 同期イベントで進行するため、視覚表現（アニメーション・Tween）の完了を待たずに次ターンや裏のロジックが走ってしまう（フィールド）。
2. **ロジックの重複と散在**: `RuleEvaluator.EvaluateAll` や `TagBehaviorRunner` などの重要な評価処理が各マネージャーに散らばっているため、修正漏れのバグを生みやすい。
3. **同期と非同期モデルの混在**: バトルはコールバックベース、フィールドはイベントベースと記述方針が統一されていない。

## 期待される動作 (Expected Behavior)
- **非同期（UniTask）による待機**: フィールド・バトル問わず、各行動や演出の完了を `await` で直感的に待機できるようになる。
- **ターン解決フェーズの共通化**: `TurnResolutionProcessor` 等の共有クラスが、ターン終了時のルール評価・効果適用・タグ更新を一手に引き受ける。
- **適材適所の独立したマネージャー**: パイプラインを完全統合（単一エンジン化）するのではなく、フィールド（入力駆動）とバトル（ステートマシン）の異なる特性を維持したまま、それぞれ非同期化＆部品の再利用を行う。

## 修正方針 (Fix Strategy)
1. **UniTaskの導入**: 
   - Unity WebGL環境で安全に非同期処理を扱うため、`com.cysharp.unitask` を導入する。
2. **ターン終了処理のパッケージ化**:
   - `TurnResolutionProcessor` を新規作成し、両マネージャーから `await TurnResolutionProcessor.ExecuteAsync()` として呼び出せるようにする。
3. **各マネージャーの非同期リファクタリング**:
   - `TurnManager.OnPlayerAction` を非同期化し、「プレイヤー行動」→「敵全体行動」→「解決フェーズ」を直列待機する仕組みへ。
   - `BattleManager` の進行メソッド群（`DoEnemyTurn`, `DoTurnEnd` 等）を非同期化し、巨大なコールバックチェーンから抜け出す。

## 該当ファイル (Relevant Files)
| コンテキスト | ファイルパス | 備考 |
| :--- | :--- | :--- |
| [新規] | `Assets/Scripts/Core/TurnResolutionProcessor.cs` | ターン終了時の共通処理 |
| [修正] | `Assets/Scripts/Core/TurnManager.cs` | 同期イベントからUniTaskパイプラインへ |
| [修正] | `Assets/Scripts/Battle/BattleManager.cs` | コールバック地獄からUniTaskベースの進行へ |
| [関連] | `Packages/manifest.json` | UniTaskパッケージの追加 |

## 確認事項 (Acceptance Criteria)
- [x] UniTaskパッケージがインストールされ、エラーなくコンパイルできること。
- [x] `TurnResolutionProcessor` でルール評価とタグ処理が共通化されていること。
- [x] `TurnManager` と `BattleManager` がそれぞれ非同期（UniTask）で進行を制御していること。
- [x] （既存検証）ルール改変、各種アクション、タグのTickが従来通り機能していること。
