using System;
using System.Collections.Generic;
using FlipLogic.Data;

namespace FlipLogic.Core
{
    /// <summary>
    /// ゲーム全体の進行状態を保持するクラス。
    /// シナリオフラグ、所持アイテム、取得済みルール等を管理する。
    /// </summary>
    [Serializable]
    public class GameState
    {
        /// <summary>ゲーム進行フェーズ。</summary>
        public GamePhase CurrentPhase = GamePhase.Prologue;

        /// <summary>所持アイテム（アイテム名→所持数）。</summary>
        public Dictionary<string, int> Inventory = new Dictionary<string, int>();

        /// <summary>シナリオフラグ。</summary>
        public Dictionary<string, bool> StoryFlags = new Dictionary<string, bool>();

        /// <summary>ルール改変能力が解放済みか。</summary>
        public bool IsRuleHackUnlocked = false;

        public void AddItem(string itemName, int count = 1)
        {
            if (Inventory.ContainsKey(itemName))
                Inventory[itemName] += count;
            else
                Inventory[itemName] = count;
        }

        public bool UseItem(string itemName)
        {
            if (!Inventory.ContainsKey(itemName) || Inventory[itemName] <= 0)
                return false;

            Inventory[itemName]--;
            if (Inventory[itemName] <= 0)
                Inventory.Remove(itemName);
            return true;
        }

        public void SetFlag(string flagName, bool value = true)
        {
            StoryFlags[flagName] = value;
        }

        public bool GetFlag(string flagName)
        {
            return StoryFlags.ContainsKey(flagName) && StoryFlags[flagName];
        }
    }

    /// <summary>ゲーム進行フェーズ。</summary>
    public enum GamePhase
    {
        Prologue,
        Act1,       // 序盤（偽装段階）
        Act2,       // 中盤（覚醒段階）
        Act3,       // 終盤（メタ展開）
        Ending
    }
}
