using System.Collections.Generic;
using UnityEngine;
using FlipLogic.Data;

namespace FlipLogic.Core
{
    /// <summary>
    /// タグの物理法則・絶対法則を実行するエンジン。
    /// ターン終了時やタグ付与時に、TagBehaviorDefに基づいて実際の効果（ダメージ、即死等）を適用する。
    /// </summary>
    public class TagBehaviorRunner : MonoBehaviour
    {
        public static TagBehaviorRunner Instance { get; private set; }

        private readonly Dictionary<string, TagBehaviorDef> _behaviorRegistry = new Dictionary<string, TagBehaviorDef>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SetupWorldLaws();
        }

        public void RegisterBehavior(TagBehaviorDef def)
        {
            _behaviorRegistry[def.BehaviorId] = def;
        }

        private void SetupWorldLaws()
        {
            // 即死の法則
            RegisterBehavior(new TagBehaviorDef
            {
                BehaviorId = "InstantDeath",
                DisplayName = "即死",
                Trigger = TagTrigger.TurnEnd,
                Effect = TagBehaviorType.SetHpZero,
                Description = "ターン終了時にHPを0にする。"
            });
        }

        /// <summary>
        /// ターン終了時の全タグ振る舞いを実行する。
        /// </summary>
        public void ExecuteTurnEndBehaviors()
        {
            var entities = UnityEngine.Object.FindObjectsByType<GameEntity>(UnityEngine.FindObjectsSortMode.None);
            
            // 1. マスとエンティティの相互作用（絶対法則）— 先に実行してタグを付与
            ExecuteTileInteractions(entities);

            // 2. エンティティのタグによる効果（付与されたタグも含めて即時発動）
            foreach (var entity in entities)
            {
                ExecuteEntityBehaviors(entity, TagTrigger.TurnEnd);
            }

            // 3. HP0のエンティティを処理
            CleanupDeadEntities(entities);
        }

        private void ExecuteEntityBehaviors(GameEntity entity, TagTrigger trigger)
        {
            var currentTags = entity.Tags.Tags;
            foreach (var tag in currentTags)
            {
                if (string.IsNullOrEmpty(tag.BehaviorId)) continue;
                if (!_behaviorRegistry.TryGetValue(tag.BehaviorId, out var def)) continue;

                if (def.Trigger == trigger)
                {
                    ApplyEffect(entity, def);
                }
            }
        }

        private void ExecuteTileInteractions(GameEntity[] entities)
        {
            if (Grid.GridMap.Instance == null) return;

            foreach (var entity in entities)
            {
                var tileTags = Grid.GridMap.Instance.GetCellTags(entity.GridPosition);
                // ※ ここにあった固定の属性相互作用（火と氷など）は、
                // ※ 全てRuleEvaluatorとRuleDataによる動的評価に移行しました。
            }
        }

        private void ApplyEffect(GameEntity target, TagBehaviorDef def)
        {
            switch (def.Effect)
            {
                case TagBehaviorType.SetHpZero:
                    target.Hp = 0;
                    Debug.Log($"[WorldLaw] {target.EntityName} は {def.DisplayName} により倒れた。");
                    break;
            }
        }

        /// <summary>HP0のエンティティをシーンから除去する。</summary>
        private void CleanupDeadEntities(GameEntity[] entities)
        {
            foreach (var entity in entities)
            {
                if (entity == null) continue;
                if (entity.Type == EntityType.Player) continue; // プレイヤーは別処理
                if (!entity.IsAlive)
                {
                    Debug.Log($"[WorldLaw] {entity.EntityName} をフィールドから除去。");
                    if (Grid.GridMap.Instance != null)
                        Grid.GridMap.Instance.UnregisterEntity(entity);
                    Destroy(entity.gameObject);
                }
            }
        }
    }
}
