using System;
using UnityEngine;

namespace FlipLogic.Core
{
    /// <summary>
    /// タグの発動タイミング。
    /// </summary>
    public enum TagTrigger
    {
        TurnEnd,    // ターン終了時
        OnApplied,  // 付与された瞬間
        OnRemoved,  // 除去された瞬間
        WhilePresent // 存在している間（毎ティック）
    }

    /// <summary>
    /// タグの振る舞い（効果）の種別。
    /// </summary>
    public enum TagBehaviorType
    {
        None,
        SetHpZero,    // HPを0にする（即死）
        DealDamage,   // ダメージを与える
        Heal,         // 回復する
        AddTag,       // 別のタグを付与する
        RemoveTag,    // 特定のタグを除去する
        ClearStatus   // 全ステータス解除
    }

    /// <summary>
    /// タグの絶対法則（世界法則）の定義。
    /// プレイヤーが改変できない固定の挙動を記述する。
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NewTagBehavior", menuName = "FlipLogic/Tag Behavior")]
    public class TagBehaviorDef : ScriptableObject
    {
        public string BehaviorId;
        public string DisplayName;
        
        /// <summary>トリガータイミング</summary>
        public TagTrigger Trigger;
        
        /// <summary>実行される効果</summary>
        public TagBehaviorType Effect;
        
        /// <summary>効果パラメータ（ダメージ量、付与タグ名など）</summary>
        public string[] Params;

        /// <summary>解除条件（例: "duration=0", "has:Element:Water"）</summary>
        public string RemoveCondition;

        /// <summary>説明文</summary>
        public string Description;
    }
}
