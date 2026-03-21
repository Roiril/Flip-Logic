using System.Collections.Generic;
using UnityEngine;

namespace FlipLogic.Rulebook
{
    [CreateAssetMenu(fileName = "NewRulebookAsset", menuName = "FlipLogic/Rulebook/Rulebook Asset")]
    public class RulebookAsset : ScriptableObject
    {
        public List<RulePageAsset> Pages = new List<RulePageAsset>();
    }
}
