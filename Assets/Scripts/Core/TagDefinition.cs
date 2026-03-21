using System;

namespace FlipLogic.Core
{
    /// <summary>
    /// タグの定義。エンティティや地形セルに付与される属性の最小単位。
    /// Key-Value形式で種別と値を表現し、Durationで持続ターン数を管理する。
    /// </summary>
    [Serializable]
    public struct TagDefinition : IEquatable<TagDefinition>
    {
        /// <summary>タグの種別キー（例: "Element", "Damage", "Status"）</summary>
        public string Key;

        /// <summary>タグの値（例: "Fire", "Physical", "Poison"）</summary>
        public string Value;

        /// <summary>残りターン数。0 = 永続。毎ターン減算され、0になると除去。</summary>
        public int Duration;

        /// <summary>タグの発生源エンティティ名（トレーサビリティ用）</summary>
        public string Source;

        /// <summary>タグの振る舞い定義（世界法則）へのID参照</summary>
        public string BehaviorId;

        public TagDefinition(string key, string value, int duration = 0, string source = "", string behaviorId = "")
        {
            Key = key;
            Value = value;
            Duration = duration;
            Source = source;
            BehaviorId = behaviorId;
        }

        /// <summary>永続タグか。</summary>
        public bool IsPermanent => Duration == 0;

        /// <summary>Key-Valueの一致で同一タグと判定する。</summary>
        public bool Equals(TagDefinition other)
        {
            return Key == other.Key && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is TagDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Key ?? "").GetHashCode() ^ (Value ?? "").GetHashCode();
        }

        public override string ToString()
        {
            string dur = IsPermanent ? "永続" : $"{Duration}T";
            return $"[{Key}:{Value}]({dur})";
        }

        public static bool operator ==(TagDefinition a, TagDefinition b) => a.Equals(b);
        public static bool operator !=(TagDefinition a, TagDefinition b) => !a.Equals(b);
    }
}
