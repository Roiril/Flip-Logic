using System.Collections.Generic;
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

            var entities = FindObjectsByType<Core.GameEntity>(FindObjectsSortMode.None);
            if (entities.Length == 0) return _lastResults;

            foreach (var rule in _activeRules)
            {
                if (!rule.IsActive) continue;

                var logicState = rule.EvaluateState();
                if (logicState == LogicState.Invalid) continue;

                // 現在の条件と結果を取得（スワップ反映済み）
                var condition = rule.CurrentTagCondition;
                var effect = rule.CurrentTagResult;
                if (condition == null || effect == null) continue;

                // 否定状態の判定
                bool conditionNegated = IsConditionNegated(rule);
                bool resultNegated = IsResultNegated(rule);

                foreach (var entity in entities)
                {
                    // 条件Pを評価
                    if (condition.Evaluate(entity.Tags, conditionNegated))
                    {
                        // 結果Qを強制適用
                        effect.Apply(entity.Tags, resultNegated, $"Rule:{rule.RuleId}");

                        var result = new EvaluationResult
                        {
                            Rule = rule,
                            TargetEntity = entity,
                            LogicState = logicState,
                            AppliedEffect = effect
                        };
                        _lastResults.Add(result);
                        OnRuleApplied?.Invoke(result);

                        Debug.Log($"[RuleEvaluator] {rule.RuleName}({LogicEvaluator.GetStateDisplayText(rule)}) → {entity.EntityName} に適用: [{effect.Key}:{effect.Value}]");
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
