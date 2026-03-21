using System;
using UnityEngine;
using FlipLogic.Data;

namespace FlipLogic.Logic
{
    /// <summary>
    /// ルール評価の1回分の結果を表現する構造体
    /// </summary>
    public struct RuleEvalEvent
    {
        public DateTime Timestamp;
        public int TurnNumber;
        public string PhaseName;
        public string RuleId;
        public string RuleName;
        public bool ConditionMet;
        public string TargetEntityInfo;
        public TagCondition ConditionData;
        public TagEffect AppliedEffectData;
    }

    /// <summary>
    /// ルール評価の結果を受け取るロガーインターフェース
    /// </summary>
    public interface IRuleEventLogger
    {
        void LogEvent(RuleEvalEvent evalEvent);
    }
}
