using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Data;

namespace FlipLogic.Battle
{
    /// <summary>
    /// バトルコマンドの定義。
    /// 各コマンドはタグ付与行為として再定義される。
    /// </summary>
    public enum BattleCommandType
    {
        Attack,         // たたかう → [Damage:Physical] タグ付与
        Defend,         // ぼうぎょ → [Status:Defending] タグ付与
        UseItem,        // アイテム → アイテムのタグリストを対象に付与
        OpenRulebook,   // ルールブック → ルール改変UIを開く
        Flee,           // にげる
    }

    /// <summary>
    /// バトルコマンド実行ロジック。
    /// コマンドをタグ操作に変換する。
    /// </summary>
    public static class BattleCommand
    {
        /// <summary>
        /// 「たたかう」コマンド。対象に [Damage:Physical] タグを1ターン付与。
        /// </summary>
        public static void ExecuteAttack(GameEntity attacker, GameEntity target)
        {
            int damage = target.TakeDamage(attacker.Attack);
            target.Tags.AddTag(new TagDefinition("Damage", "Physical", 1, attacker.EntityName));
            Debug.Log($"[Battle] {attacker.EntityName} が {target.EntityName} を攻撃！ {damage}ダメージ");
        }

        /// <summary>
        /// 「ぼうぎょ」コマンド。自身に [Status:Defending] タグを1ターン付与。
        /// </summary>
        public static void ExecuteDefend(GameEntity entity)
        {
            entity.Tags.AddTag(new TagDefinition("Status", "Defending", 1, entity.EntityName));
            Debug.Log($"[Battle] {entity.EntityName} は防御姿勢をとった");
        }

        /// <summary>
        /// 「アイテム」コマンド。アイテムのタグリストを対象に付与する。
        /// </summary>
        public static void ExecuteItem(GameEntity user, GameEntity target, ItemData item)
        {
            if (item == null) return;

            foreach (var tag in item.EffectTags)
            {
                target.Tags.AddTag(new TagDefinition(tag.Key, tag.Value, tag.Duration, $"Item:{item.ItemName}"));
            }
            Debug.Log($"[Battle] {user.EntityName} が {target.EntityName} に {item.ItemName} を使用");
        }
    }

    /// <summary>
    /// バトル結果。
    /// </summary>
    public enum BattleResult
    {
        Victory,
        RuleHackVictory,
        Defeat,
        Fled,
    }

    /// <summary>
    /// バトルフェーズ。
    /// </summary>
    public enum BattlePhase
    {
        None,
        Start,
        PlayerCommand,
        PlayerAction,
        EnemyTurn,
        RuleHack,
        BattleResult,
        End,
    }
}
