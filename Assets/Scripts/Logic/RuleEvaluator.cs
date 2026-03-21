using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FlipLogic.Data;

namespace FlipLogic.Logic
{
    /// <summary>
    /// グローバル・ルール評価エンジン。
    /// ターン末に全アクティブルールを全エンティティに対して一斉評価し、
    /// 条件が真のエンティティに結果（タグ操作）を強制適用する。
    /// </summary>
    public class RuleEvaluator : MonoBehaviour
    {
        public static RuleEvaluator Instance { get; private set; }

        /// <summary>デバッグ・エディタでの評価結果動的ログ収集用</summary>
        public static IRuleEventLogger GlobalLogger { get; set; }

        /// <summary>評価対象のルールリスト。RulebookManagerから供給される。</summary>
        private readonly List<RuleData> _activeRules = new List<RuleData>();

        /// <summary>評価結果ログ（デバッグ/演出用）。</summary>
        private readonly List<EvaluationResult> _lastResults = new List<EvaluationResult>();

        /// <summary>直近の評価結果。</summary>
        public IReadOnlyList<EvaluationResult> LastResults => _lastResults;

        /// <summary>ルール適用時の通知イベント。</summary>
        public event System.Action<EvaluationResult> OnRuleApplied;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 評価対象のルールを設定する。
        /// </summary>
        public void SetRules(IEnumerable<RuleData> rules)
        {
            _activeRules.Clear();
            _activeRules.AddRange(rules);
        }

        /// <summary>
        /// ルールを1つ追加する。
        /// </summary>
        public void AddRule(RuleData rule)
        {
            if (!_activeRules.Exists(r => r.RuleId == rule.RuleId))
                _activeRules.Add(rule);
        }

        /// <summary>
        /// 全アクティブルールを全エンティティに対して一斉評価する。
        /// TurnManagerのターン末処理から呼ばれる。
        /// </summary>
        public List<EvaluationResult> EvaluateAll()
        {
            _lastResults.Clear();

            var entities = Core.EntityRegistry.Instance.GetAllEntities();
            if (!entities.Any()) return _lastResults;

            foreach (var rule in _activeRules)
            {
                if (!rule.IsActive) continue;

                var logicState = rule.GetCurrentLogicState();

                // 現在の条件と結果を取得（スワップ反映済み）
                var condition = rule.CurrentTagCondition;
                var effect = rule.CurrentTagResult;
                if (condition == null || effect == null) continue;

                bool conditionNegated = IsConditionNegated(rule);
                bool resultNegated = IsResultNegated(rule);

                foreach (var entity in entities)
                {
                    // 主語フィルタ（SubjectFilterP）がある場合、それに合致しないエンティティは無視する
                    if (rule.SubjectFilterP != null)
                    {
                        Core.TagContainer subjectTags = GetTargetTags(entity, rule.SubjectFilterP.Target);
                        if (subjectTags == null || !rule.SubjectFilterP.Evaluate(subjectTags, false))
                        {
                            continue;
                        }
                    }

                    // 条件Pを評価するためのコンテナを選択
                    Core.TagContainer conditionTags = GetTargetTags(entity, condition.Target);
                    if (conditionTags == null) continue;

                    bool conditionMet = condition.Evaluate(conditionTags, conditionNegated);

                    // ハック: 「死ぬ (Status:InstantDeath)」条件は、物理的なHP0（死亡状態）でも満たすとする
                    if (!conditionMet && condition.Key == "Status" && condition.Value == "InstantDeath")
                    {
                        bool isDead = !entity.IsAlive;
                        bool baseResult = condition.RequirePresence ? isDead : !isDead;
                        conditionMet = conditionNegated ? !baseResult : baseResult;
                    }

                    if (conditionMet)
                    {
                        // 結果Qを適用するためのコンテナを選択
                        Core.TagContainer effectTags = GetTargetTags(entity, effect.Target);
                        if (effectTags != null)
                        {
                            effect.Apply(effectTags, resultNegated, $"Rule:{rule.RuleId}");

                            // マスの属性が変化した場合、UI（オーバーレイ）を更新する
                            if (effect.Target == RuleTarget.TileOfEntity && Grid.TileOverlayRenderer.Instance != null)
                            {
                                Grid.TileOverlayRenderer.Instance.UpdateOverlay(entity.GridPosition);
                            }

                            var result = new EvaluationResult
                            {
                                Rule = rule,
                                TargetEntity = entity,
                                LogicState = logicState,
                                AppliedEffect = effect
                            };
                            _lastResults.Add(result);
                            OnRuleApplied?.Invoke(result);
                        }
                    }

                    if (GlobalLogger != null)
                    {
                        GlobalLogger.LogEvent(new RuleEvalEvent
                        {
                            Timestamp = System.DateTime.Now,
                            TurnNumber = Core.TurnManager.Instance != null ? Core.TurnManager.Instance.CurrentTurn : 0,
                            PhaseName = Core.TurnManager.Instance != null ? Core.TurnManager.Instance.CurrentPhase.ToString() : "Unknown",
                            RuleId = rule.RuleId,
                            RuleName = rule.RuleName,
                            ConditionMet = conditionMet,
                            TargetEntityInfo = $"{entity.Type}({entity.gameObject.GetInstanceID()})",
                            ConditionData = condition,
                            AppliedEffectData = conditionMet ? effect : null
                        });
                    }
                }
            }

            return _lastResults;
        }


        /// <summary>条件側が否定されているかを判定する。</summary>
        private bool IsConditionNegated(RuleData rule)
        {
            // スワップ時はResult側の否定状態が条件に適用される
            return rule.IsSwapped ? rule.Result.IsNegated : rule.Condition.IsNegated;
        }

        /// <summary>結果側が否定されているかを判定する。</summary>
        private bool IsResultNegated(RuleData rule)
        {
            // スワップ時はCondition側の否定状態が結果に適用される
            return rule.IsSwapped ? rule.Condition.IsNegated : rule.Result.IsNegated;
        }
    


        private Core.TagContainer GetTargetTags(Core.GameEntity entity, RuleTarget target)
        {
            switch (target)
            {
                case RuleTarget.Entity:
                    return entity.Tags;
                case RuleTarget.TileOfEntity:
                    return Grid.GridMap.Instance != null ? Grid.GridMap.Instance.GetCellTags(entity.GridPosition) : null;
                default:
                    return null;
            }
        }
}

    /// <summary>
    /// ルール評価の結果記録。演出やデバッグに使用する。
    /// </summary>
    public class EvaluationResult
    {
        public RuleData Rule;
        public Core.GameEntity TargetEntity;
        public LogicState LogicState;
        public TagEffect AppliedEffect;
    }
}
