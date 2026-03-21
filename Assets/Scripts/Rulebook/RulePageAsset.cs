using System.Collections.Generic;
using UnityEngine;

namespace FlipLogic.Rulebook
{
    [CreateAssetMenu(fileName = "NewRulePageAsset", menuName = "FlipLogic/Rulebook/Rule Page Asset")]
    public class RulePageAsset : ScriptableObject
    {
        public int Chapter = 1;
        public string Title;
        [TextArea] public string Description;

        public List<RuleAsset> Rules = new List<RuleAsset>();

        public RulePage CreateRulePage()
        {
            var page = new RulePage
            {
                Chapter = this.Chapter,
                Title = this.Title,
                Description = this.Description
            };

            foreach (var ruleAsset in Rules)
            {
                if (ruleAsset != null)
                {
                    page.Rules.Add(ruleAsset.CreateRuleData());
                }
            }
            return page;
        }
    }
}
