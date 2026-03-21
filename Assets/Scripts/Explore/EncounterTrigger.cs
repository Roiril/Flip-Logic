using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Grid;

namespace FlipLogic.Explore
{
    /// <summary>
    /// プレイヤーが敵の隣接マスに移動した時にバトルを開始する。
    /// </summary>
    public class EncounterTrigger : MonoBehaviour
    {
        private void Start()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerActionComplete += CheckEncounter;
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnPlayerActionComplete -= CheckEncounter;
        }

        private void CheckEncounter()
        {
            if (Battle.BattleManager.Instance == null) return;
            if (Battle.BattleManager.Instance.IsInBattle) return;

            var player = GameManager.Instance?.PlayerEntity;
            if (player == null) return;

            // 隣接4マスとプレイヤー位置をチェック（EntityRegistry経由）
            var enemies = EntityRegistry.Instance.GetEntities(EntityType.Enemy);
            foreach (var entity in enemies)
            {
                if (entity == null || !entity.IsAlive) continue;

                var diff = entity.GridPosition - player.GridPosition;
                int dist = Mathf.Abs(diff.x) + Mathf.Abs(diff.y);

                if (dist <= 1)
                {
                    // 隣接 or 同一マス → バトル開始
                    var rule = GameManager.Instance.RulebookData?.GetActiveRules()?.Count > 0
                        ? GameManager.Instance.RulebookData.GetActiveRules()[0]
                        : null;
                    bool isTutorial = entity.EntityName != null && entity.EntityName.StartsWith("IceSlime");
                    Battle.BattleManager.Instance.StartBattle(player, entity, rule, isTutorial);
                    break;
                }
            }
        }
    }
}
