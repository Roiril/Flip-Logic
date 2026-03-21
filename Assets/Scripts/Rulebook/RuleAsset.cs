using UnityEngine;
using FlipLogic.Data;

namespace FlipLogic.Rulebook
{
    [CreateAssetMenu(fileName = "NewRuleAsset", menuName = "FlipLogic/Rulebook/Rule Asset")]
    public class RuleAsset : ScriptableObject
    {
        [Header("Rule Basic Info")]
        public string RuleId;
        public string RuleName;
        [TextArea] public string Description;
        public int Chapter = 1;
        public bool IsActive = true;

        [Header("Propositions")]
        public PropositionData Condition; // P
        public PropositionData Result;    // Q

        [Header("Mechanics")]
        [Tooltip("このルールが適用されるエンティティの条件（例：氷属性を持つ対象のみ）")]
        public TagCondition SubjectFilterP;

        [Tooltip("条件P: このタグを持つ・マスの状態がこうであるか等を評価する")]
        public TagCondition TagConditionP;

        [Tooltip("帰結Q: 条件が真のとき適用するタグ操作")]
        public TagEffect TagResultQ;

        public RuleData CreateRuleData()
        {
            return new RuleData
            {
                RuleId = this.RuleId,
                RuleName = this.RuleName,
                Description = this.Description,
                Chapter = this.Chapter,
                IsActive = this.IsActive,
                // PropositionData は class なので Clone して渡す
                Condition = this.Condition?.Clone(),
                Result = this.Result?.Clone(),
                // TagCondition/TagEffect も新規インスタンスを生成してコピー
                SubjectFilterP = this.SubjectFilterP != null ? new TagCondition
                {
                    Target = this.SubjectFilterP.Target,
                    Key = this.SubjectFilterP.Key,
                    Value = this.SubjectFilterP.Value,
                    RequirePresence = this.SubjectFilterP.RequirePresence
                } : null,
                TagConditionP = this.TagConditionP != null ? new TagCondition
                {
                    Target = this.TagConditionP.Target,
                    Key = this.TagConditionP.Key,
                    Value = this.TagConditionP.Value,
                    RequirePresence = this.TagConditionP.RequirePresence
                } : null,
                TagResultQ = this.TagResultQ != null ? new TagEffect
                {
                    Target = this.TagResultQ.Target,
                    Key = this.TagResultQ.Key,
                    Value = this.TagResultQ.Value,
                    Operation = this.TagResultQ.Operation,
                    Duration = this.TagResultQ.Duration,
                    BehaviorId = this.TagResultQ.BehaviorId
                } : null
            };
        }
    }
}
