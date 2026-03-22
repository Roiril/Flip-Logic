# Issue 10: ビジュアル強化（簡略化版）

## 概要

プレイヤー・敵・地形マスの見た目を「ゲームらしくリッチ」にする。  
**AI生成カラー画像 + Tweenアニメ + 軽量エフェクト** の3本柱で、最小コストで実現する。

---

## 背景と現状

| 対象 | 現状 |
|------|------|
| プレイヤー | チュートリアル流用の青い円（「P」文字入り）。スケール調整のみ |
| 敵 | `EntityVisualDef` で単色スプライトを中央配置。アニメーションなし |
| 地形 | `TileOverlayRenderer` によるベタ塗り四角。毒沼のみ泡パターンあり |

---

## 方針

- **画像はすべてAI生成**（カラー済み）。グレースケール変換やシェーダーによる着色は行わない
- **アニメーションはTweenライブラリ**（DOTween等）で実装。自作アニメーションコンポーネントは作らない
- **既存クラスへの変更は最小限**。新規クラスの追加も最小に抑える
- **カスタムシェーダーは作らない**。見た目の差別化はAI画像側で吸収する

---

## タスク一覧

### Phase 1 — アセット準備（AI生成）

#### 1-A スプライト画像の生成・インポート

**要件:**

- プレイヤー用カラースプライト（1〜2枚）
- 敵スプライト（種類ごとに1枚、カラー済み・アウトライン付き）
- タイル画像（通常 / 毒沼 / 炎 / 氷 各1枚、軽い立体感・テクスチャ付き）
- すべて透過PNG、PixelPerUnit統一
- `Assets/Sprites/` に配置

**完了条件:** 全スプライトがインポートされ、Unity上でプレビュー確認できる

---

### Phase 2 — Tweenアニメ導入

#### 2-A Tweenライブラリの導入

**要件:**

- DOTween（無料版）をPackage Managerまたは `.unitypackage` で導入
- WebGLビルドとの互換性を確認

**完了条件:** DOTweenがプロジェクトに導入され、コンパイルエラーなし

---

#### 2-B `EntityAnimator` ヘルパークラスの新規作成

**目的:** Tweenベースのアニメーションを薄くラップし、エンティティに共通のアニメ操作を提供する。

**要件:**

- `MonoBehaviour` として実装
- 以下のpublicメソッドを持つ:
  - `PlayIdle()` — `transform.DOScale()` のYoyo無限ループで微小呼吸
  - `PlayMove(Vector3 from, Vector3 to)` — `DOJump()` or `DOMove()` + Y軸カーブで放物線移動
  - `PlayHit()` — `DOShakePosition()` + `SpriteRenderer.DOColor(赤→元色)` で被弾表現
  - `PlayDeath(System.Action onComplete)` — `DOScale(0)` + `DOFade(0)`、完了後にコールバック
- パラメータ（振幅・速度・シェイク強度等）は `[SerializeField]` でInspectorから調整可能
- `Animator` コンポーネントへの依存なし

**完了条件:** テスト用GameObjectにアタッチし、各メソッドを呼んでアニメが再生される

---

#### 2-C プレイヤービジュアルの刷新

**要件:**

- 仮スプライト（青い円）をPhase 1-AのAI生成画像に差し替え
- `EntityAnimator` をアタッチし、`PlayerController` のイベントに連動:
  - 移動入力 → `PlayMove()`
  - 被弾 → `PlayHit()`
  - 死亡 → `PlayDeath()`
- アイドル時は `PlayIdle()` を自動再生

**完了条件:** プレイヤーが移動・被弾・死亡の各タイミングで対応アニメが再生される

---

#### 2-D 敵ビジュアルへの適用

**要件:**

- `EntityFactory.CreateEnemy()` 内で `EntityAnimator` を自動アタッチ
- `EntityVisualDef.Sprite` にAI生成画像を設定
- 敵の状態変化（移動・被弾・撃破）のタイミングで対応するアニメを呼び出し
- アイドル時は `PlayIdle()` を自動再生

**完了条件:** 既存の Ghost・Slime が移動・被弾・撃破時にアニメーションする

---

### Phase 3 — エフェクト

#### 3-A ParticleSystem Prefab の作成

**要件:**

- 以下の2〜3個のPrefabを `Assets/Prefabs/Effects/` に作成:
  - **HitSpark** — 被弾時の火花（ランダム方向、短命、`Max Particles` 20以下）
  - **DeathBurst** — 撃破時の破片（多め・広め・重力あり、`Max Particles` 30以下）
  - **DustPuff**（任意） — 移動時の土煙
- 再生後に自動で停止する設定（`Stop Action: Destroy` or プール管理）
- `EntityAnimator` の各メソッド内から `Instantiate` + `Play` で呼び出し

**完了条件:** 被弾・撃破時にパーティクルエフェクトが再生される

---

### Phase 4 — 地形ビジュアル

#### 4-A タイル画像の差し替え

**要件:**

- `TileVisualDef` の各マッピングに、AI生成したタイル画像を `CustomSprite` として設定
- `UseCustomSprite = true` に変更
- 既存の `TileOverlayRenderer` のロジック変更は不要（`ApplyMappingToRenderer` で `CustomSprite` が既にサポート済み）

**完了条件:** 毒沼・炎・氷のタイルがAI生成画像で表示される

---

## 受け入れ条件（Issue 全体）

- [ ] プレイヤー・敵の仮スプライトがAI生成画像に差し替わっていること
- [ ] プレイヤー・敵にアイドル・移動・被弾・死亡のアニメーションがあること
- [ ] 被弾・撃破時にパーティクルエフェクトが出ること
- [ ] 地形タイルが立体感のあるAI生成画像で表示されること
- [ ] フレームレートへの影響が軽微であること（Particleの `Max Particles` 30以下）
- [ ] 既存のゲームロジック（移動・戦闘・スコア）が変わらず動作すること
- [ ] カスタムシェーダーの追加がゼロであること

---

## 実装優先順位

```
1-A スプライト画像生成
2-A Tweenライブラリ導入
2-B EntityAnimator作成
  └─ 2-C プレイヤー適用
  └─ 2-D 敵適用
3-A ParticleSystem Prefab
4-A タイル画像差し替え
```

`2-B → 2-C` だけでプレイヤーの動きが付き、進捗として確認しやすい。

---

## 関連

- `EntityVisualDef`（敵ビジュアル定義）
- `EntityFactory`（エンティティ生成）
- `TileOverlayRenderer` / `TileVisualDef`（地形描画）
- `PlayerController`（プレイヤー制御）