using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using FlipLogic.Logic;

namespace FlipLogic.Core
{
    /// <summary>
    /// ターン終了時の共通処理（ルール評価、効果適用、タグ処理）を行う非同期プロセッサ。
    /// フィールド（TurnManager）とバトル（BattleManager）の両方から呼び出される。
    /// </summary>
    public static class TurnResolutionProcessor
    {
        public static async UniTask<List<EvaluationResult>> ExecuteAsync()
        {
            // 1. ルール一斉評価
            List<EvaluationResult> results = null;
            if (RuleEvaluator.Instance != null)
            {
                results = RuleEvaluator.Instance.EvaluateAll();
            }

            // 2. 絶対法則（タグの振る舞い）の実行
            if (TagBehaviorRunner.Instance != null)
            {
                TagBehaviorRunner.Instance.ExecuteTurnEndBehaviors();
            }

            // 3. 各エンティティのタグ期限更新
            if (EntityRegistry.Instance != null)
            {
                var entities = EntityRegistry.Instance.GetAllEntities();
                foreach (var entity in entities)
                {
                    entity.Tags.TickDurations();
                }
            }

            // 4. マスのタグ期限更新
            if (Grid.GridMap.Instance != null)
            {
                Grid.GridMap.Instance.TickAllCellTags();
            }

            // 今後の拡張として、ここで発動したエフェクトや死亡演出の一斉待機を挿入可能
            // 例: await VisualEffectManager.WaitForAllEffectsToFinish();
            
            // 最低でも1フレーム待機して視覚的更新を保証する
            await UniTask.Yield();

            return results;
        }
    }
}
