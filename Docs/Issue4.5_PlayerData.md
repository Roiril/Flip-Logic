# [COMPLETED] Issue 4.5: プレイヤーのPrefab化と動的生成

### タイトル
`refactor: プレイヤーのPrefab化と動的生成`

## 概要 (Overview)
- **発生箇所:** `FlipLogic.Core`, `FlipLogic.Tutorial`
- **事象:** Issue 4 で敵の生成がデータ駆動化されたが、プレイヤー (`PlayerObj`) は依然としてヒエラルキー上に配置される静的オブジェクトのままであり、全ステージシーンへの配置が必須となっている。

## 現在の動作 (Current Behavior)
- **[プレイヤー管理]:** `PlayerObj` がシーンに事前配置されており、`TutorialSetup` や `CameraFollow` からの直接参照によって初期化・追従されている。

## 期待される動作 (Expected Behavior)
- プレイヤーを素直にPrefabとして構築し、シーン上にオブジェクトがなくても初期化処理経由で動的生成されること。
- 生成されたインスタンスにカメラが自動的に追従すること。

## 修正方針 (Fix Strategy)
1. スプライトや `GameEntity`、`PlayerController` が設定された `PlayerObj` を `Assets/Prefabs/Player.prefab` としてプレハブ化する。
2. `TutorialSetup` 等のステージ初期化スクリプトにてプレハブから `Instantiate` する。
3. Instantiate後に `CameraFollow.Target` に対して生成したプレイヤーのTransformを割り当てる。

## 該当ファイル (Relevant Files)
| コンテキスト | ファイルパス | 備考 |
| :--- | :--- | :--- |
| [新規] | `Assets/Prefabs/Player.prefab` | プレイヤープレハブ |
| [修正] | `Assets/Scripts/Tutorial/TutorialSetup.cs` | プレイヤープレハブのロード・生成対応 |
| [修正] | `Assets/Scripts/Core/CameraFollow.cs` | ターゲットの動的アサイン対応 |

## 確認事項 (Acceptance Criteria)
- [x] `Player.prefab` を作成できること。
- [x] シーンから `PlayerObj` を削除した状態で、指定した座標にプレイヤーがPrefabから生成されること。
- [x] 生成されたプレイヤーにカメラが追従すること。
- [x] シナリオやターン進行などの既存システムがエラーなく動作すること。
