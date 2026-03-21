using System;
using FlipLogic.Core;

namespace FlipLogic.Data
{
    public enum RuleTarget
    {
        Entity,       // エンティティ自身
        Tile,         // 特定のマス（現在は未使用だが将来用）
        TileOfEntity  // エンティティが現在いるマス
    }

    /// <summary>
    /// 命題の単位データ。肯定形/否定形テキストと現在の否定状態を保持する。
    /// </summary>
    [Serializable]
    public class PropositionData
    {
        public string AffirmativeText;
        public string NegativeText;
        public bool IsNegated;

        public string CurrentText => IsNegated ? NegativeText : AffirmativeText;

        public PropositionData(string affirmative, string negative)
        {
            AffirmativeText = affirmative;
            NegativeText = negative;
            IsNegated = false;
        }

        /// <summary>否定状態をトグルする。</summary>
        public void ToggleNegation()
        {
            IsNegated = !IsNegated;
        }

        /// <summary>状態をリセットする。</summary>
        public void Reset()
        {
            IsNegated = false;
        }

        /// <summary>ディープコピーを返す。</summary>
        public PropositionData Clone()
        {
            return new PropositionData(AffirmativeText, NegativeText)
            {
                IsNegated = IsNegated
            };
        }
    }

    /// <summary>
    /// ルール（命題）のデータ。条件P→帰結Qの構造と、タグ条件/タグ操作を統合管理する。
    /// </summary>
    [Serializable]
    public class RuleData
    {
        public string RuleId;
        public string RuleName;
        public string Description;
        public PropositionData Condition; // P
        public PropositionData Result;    // Q
        public bool IsSwapped;           // PとQが入れ替わっているか

        // --- Subject Filter ---
        /// <summary>このルールが適用されるエンティティの条件（例：氷属性を持つ対象のみ）。</summary>
        public TagCondition SubjectFilterP;

        // --- Tag Condition (P) ---
        /// <summary>条件P: このタグを持つ・マスの状態がこうであるか等を評価する。</summary>
        public TagCondition TagConditionP;

        // --- Tag Result (Q) ---
        /// <summary>帰結Q: 条件が真のとき適用するタグ操作。</summary>
        public TagEffect TagResultQ;

        /// <summary>ルールが有効か（プレイヤーが取得済み・ページ解放済み）。</summary>
        public bool IsActive = true;

        /// <summary>ルールの章番号（ページ概念）。</summary>
        public int Chapter = 1;

        /// <summary>タグ設定のバリデーションを実行する</summary>
        public void ValidateTags()
        {
            SubjectFilterP?.Validate();
            TagConditionP?.Validate();
            TagResultQ?.Validate();
        }

        /// <summary>現在のP側（表示上の条件側）を返す。</summary>
        public PropositionData DisplayCondition => IsSwapped ? Result : Condition;

        /// <summary>現在のQ側（表示上の帰結側）を返す。</summary>
        public PropositionData DisplayResult => IsSwapped ? Condition : Result;

        /// <summary>現在の条件タグ（スワップ反映）。</summary>
        public TagCondition CurrentTagCondition => IsSwapped ? ConvertEffectToCondition(TagResultQ) : TagConditionP;

        /// <summary>現在の結果タグ操作（スワップ反映）。</summary>
        public TagEffect CurrentTagResult => IsSwapped ? ConvertConditionToEffect(TagConditionP) : TagResultQ;

        /// <summary>PとQの位置を入れ替える（逆の操作）。</summary>
        public void ToggleSwap()
        {
            IsSwapped = !IsSwapped;
        }

        /// <summary>全状態をリセットする。</summary>
        public void ResetAll()
        {
            IsSwapped = false;
            Condition.Reset();
            Result.Reset();
        }

        /// <summary>現在の論理状態を判定する。すべて独立して扱えるよう拡張。</summary>
        public LogicState GetCurrentLogicState()
        {
            bool swapped = IsSwapped;
            bool pNeg = Condition.IsNegated;
            bool qNeg = Result.IsNegated;

            if (!swapped)
            {
                if (!pNeg && !qNeg) return LogicState.Original;
                if (pNeg && qNeg) return LogicState.Inverse;
                if (pNeg) return LogicState.PNegatedOnly;
                return LogicState.QNegatedOnly;
            }
            else
            {
                if (!pNeg && !qNeg) return LogicState.Converse;
                if (pNeg && qNeg) return LogicState.Contrapositive;
                if (pNeg) return LogicState.SwappedPNeg;
                return LogicState.SwappedQNeg;
            }
        }

        /// <summary>タグ効果をタグ条件に変換する（スワップ用）。</summary>
        private TagCondition ConvertEffectToCondition(TagEffect effect)
        {
            return new TagCondition
            {
                Key = effect.Key,
                Value = effect.Value,
                RequirePresence = (effect.Operation == TagOperation.Add),
                Target = effect.Target
            };
        }

        /// <summary>タグ条件をタグ効果に変換する（スワップ用）。</summary>
        private TagEffect ConvertConditionToEffect(TagCondition condition)
        {
            return new TagEffect
            {
                Key = condition.Key,
                Value = condition.Value,
                Operation = condition.RequirePresence ? TagOperation.Add : TagOperation.Remove,
                Target = condition.Target
            };
        }
    }

    /// <summary>
    /// タグ条件。特定のタグが存在しているかどうかを判定。
    /// </summary>
    [Serializable]
    public class TagCondition
    {
        public RuleTarget Target = RuleTarget.Entity;
        public string Key;
        public string Value;
        public bool RequirePresence = true; // trueなら「存在する」、falseなら「存在しない」

        public TagCondition() { }

        public TagCondition(string key, string value, bool requirePresence = true, RuleTarget target = RuleTarget.Entity)
        {
            Key = key;
            Value = value;
            RequirePresence = requirePresence;
            Target = target;
            Validate();
        }

        public void Validate()
        {
            if (TagKeyRegistry.Instance != null && !TagKeyRegistry.Instance.IsValid(Key, Value))
            {
                UnityEngine.Debug.LogWarning($"[TagCondition] 未定義のタグまたは値が指定されています。Key: {Key}, Value: {Value}");
            }
        }

        /// <summary>否定状態を考慮した評価。</summary>
        public bool Evaluate(TagContainer tags, bool isNegated)
        {
            bool hasTag = tags.HasTag(Key, Value);
            bool baseResult = RequirePresence ? hasTag : !hasTag;

            // 否定時は結果を反転
            return isNegated ? !baseResult : baseResult;
        }
    }

    /// <summary>
    /// タグ効果。ルール評価結果として対象に適用されるタグ操作。
    /// </summary>
    [Serializable]
    public class TagEffect
    {
        public RuleTarget Target = RuleTarget.Entity;
        public string Key;
        public string Value;
        public TagOperation Operation;
        public int Duration = -1;
        public string BehaviorId = "";

        public TagEffect() { }

        public TagEffect(string key, string value, TagOperation operation, int duration = -1, string behaviorId = "", RuleTarget target = RuleTarget.Entity)
        {
            Key = key;
            Value = value;
            Operation = operation;
            Duration = duration;
            BehaviorId = behaviorId;
            Target = target;
            Validate();
        }

        public void Validate()
        {
            if (TagKeyRegistry.Instance != null && !TagKeyRegistry.Instance.IsValid(Key, Value))
            {
                UnityEngine.Debug.LogWarning($"[TagEffect] 未定義のタグまたは値が指定されています。Key: {Key}, Value: {Value}");
            }
        }

        /// <summary>否定状態を考慮した効果適用。</summary>
        public void Apply(TagContainer tags, bool isNegated, string source = "")
        {
            var effectiveOp = Operation;

            // 否定時は操作を反転（Add↔Remove）
            if (isNegated)
            {
                effectiveOp = (effectiveOp == TagOperation.Add) ? TagOperation.Remove : TagOperation.Add;
            }

            switch (effectiveOp)
            {
                case TagOperation.Add:
                    tags.AddTag(new TagDefinition(Key, Value, Duration, source, BehaviorId));
                    break;
                case TagOperation.Remove:
                    tags.RemoveTag(Key, Value);
                    break;
            }
        }
    }

    /// <summary>タグ操作種別。</summary>
    public enum TagOperation
    {
        Add,
        Remove
    }

    /// <summary>論理状態の列挙。</summary>
    public enum LogicState
    {
        Original,       // P→Q
        Converse,       // Q→P
        Inverse,        // ¬P→¬Q
        Contrapositive, // ¬Q→¬P
        PNegatedOnly,   // ¬P→Q
        QNegatedOnly,   // P→¬Q
        SwappedPNeg,    // ¬Q→P
        SwappedQNeg,    // Q→¬P
        Invalid         // (旧)
    }
}
