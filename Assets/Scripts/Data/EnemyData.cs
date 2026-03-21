using System.Collections.Generic;
using UnityEngine;
using FlipLogic.Core;

namespace FlipLogic.Data
{
    /// <summary>
    /// 敵キャラクターの定義データ。タグベースの属性管理に対応。
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "FlipLogic/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("基本情報")]
        public string EnemyName;
        [TextArea(2, 4)]
        public string Description;
        public EntityVisualDef VisualDef;

        [Header("ステータス")]
        public int MaxHp = 100;
        public int Attack = 10;
        public int Defense = 5;

        [Header("初期タグ")]
        [Tooltip("この敵が生成時に持つタグのリスト")]
        public List<TagDefinition> InitialTags = new List<TagDefinition>();

        [Header("AI")]
        public Explore.EnemyAIType AIType = Explore.EnemyAIType.Patrol;

        [Header("バトルルール")]
        [Tooltip("この敵に関連するルールID")]
        public string RelatedRuleId;

        [Header("演出")]
        [TextArea(2, 4)]
        public string EncounterMessage;
        [TextArea(2, 4)]
        public string DefeatMessage;
    }
}
