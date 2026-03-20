using FlipLogic.Data;

namespace FlipLogic.Logic
{
    /// <summary>
    /// RuleDataの論理状態を評価し、表示用テキストを生成するユーティリティ。
    /// RuleEvaluatorの補助として使用する。
    /// </summary>
    public static class LogicEvaluator
    {
        /// <summary>RuleDataの現在のスワップ・否定状態からLogicStateを返す。</summary>
        public static LogicState Evaluate(RuleData rule)
        {
            return rule.EvaluateState();
        }

        /// <summary>指定ルールの現在状態が、要求される論理状態と一致するかを判定する。</summary>
        public static bool CheckState(RuleData rule, LogicState requiredState)
        {
            return rule.EvaluateState() == requiredState;
        }

        /// <summary>ルールの現在状態を日本語テキストとして返す。</summary>
        public static string GetStateDisplayText(RuleData rule)
        {
            var state = rule.EvaluateState();
            switch (state)
            {
                case LogicState.Original:
                    return "元の命題（P→Q）";
                case LogicState.Converse:
                    return "逆（Q→P）";
                case LogicState.Inverse:
                    return "裏（¬P→¬Q）";
                case LogicState.Contrapositive:
                    return "対偶（¬Q→¬P）";
                default:
                    return "不完全な状態";
            }
        }

        /// <summary>ルールの現在状態を命題文として整形する。</summary>
        public static string FormatCurrentProposition(RuleData rule)
        {
            var p = rule.DisplayCondition;
            var q = rule.DisplayResult;
            return $"{p.CurrentText} ならば {q.CurrentText}";
        }
    }
}
