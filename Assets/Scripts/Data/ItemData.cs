using System.Collections.Generic;
using UnityEngine;
using FlipLogic.Core;

namespace FlipLogic.Data
{
    /// <summary>
    /// アイテムの定義データ。効果をタグのリストとして表現する。
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "FlipLogic/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("基本情報")]
        public string ItemName;
        [TextArea(2, 4)]
        public string Description;
        public Sprite Icon;

        [Header("効果タグ")]
        [Tooltip("使用時に対象に付与するタグのリスト")]
        public List<TagDefinition> EffectTags = new List<TagDefinition>();

        [Header("対象")]
        [Tooltip("自分に使用するか、対象に使用するか")]
        public ItemTargetType TargetType = ItemTargetType.SingleEnemy;

        [Header("制約")]
        [Tooltip("バトル中に使用可能か")]
        public bool UsableInBattle = true;
        [Tooltip("フィールドで使用可能か")]
        public bool UsableInField = false;
        [Tooltip("消費アイテムか")]
        public bool IsConsumable = true;
    }

    /// <summary>アイテムの対象種別。</summary>
    public enum ItemTargetType
    {
        Self,           // 自分
        SingleEnemy,    // 敵単体
        AllEnemies,     // 敵全体
        Tile,           // 対象マス
    }
}
