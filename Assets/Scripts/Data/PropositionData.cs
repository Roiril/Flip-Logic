using System;
using FlipLogic.Core;

namespace FlipLogic.Data
{
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

        // --- Tag Condition (P) ---
        /// <summary>条件P: このタグを持つエンティティが評価対象。</summary>
        public TagCondition TagConditionP;

        // --- Tag Result (Q) ---
        /// <summary>帰結Q: 条件が真のとき適用するタグ操作。</summary>
        public TagEffect TagResultQ;

        /// <summary>ルールが有効か（プレイヤーが取得済み・ページ解放済み）。</summary>
        public bool IsActive = true;

        /// <summary>ルールの章番号（ページ概念）。</summary>
        public int Chapter = 1;

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

        /// <summary>現在の状態から論理状態を評価する。</summary>
        public LogicState EvaluateState()
        {
            bool swapped = IsSwapped;
            bool pNegated = Condition.IsNegated;
            bool qNegated = Result.IsNegated;

            // 元の命題: P→Q (swap=false, pNeg=false, qNeg=false)
            if (!swapped && !pNegated && !qNegated) return LogicState.Original;
            // 逆: Q→P (swap=true, pNeg=false, qNeg=false)
            if (swapped && !pNegated && !qNegated) return LogicState.Converse;
            // 裏: ¬P→¬Q (swap=false, pNeg=true, qNeg=true)
            if (!swapped && pNegated && qNegated) return LogicState.Inverse;
            // 対偶: ¬Q→¬P (swap=true, pNeg=true, qNeg=true)
            if (swapped && pNegated && qNegated) return LogicState.Contrapositive;

            // それ以外は不完全な状態
            return LogicState.Invalid;
        }

        /// <summary>タグ効果をタグ条件に変換する（スワップ用）。</summary>
        private TagCondition ConvertEffectToCondition(TagEffect effect)
        {
            return new TagCondition
            {
                Key = effect.Key,
                Value = effect.Value,
                RequirePresence = (effect.Operation == TagOperation.Add)
            };
        }

        /// <summary>タグ条件をタグ効果に変換する（スワップ用）。</summary>
        private TagEffect ConvertConditionToEffect(TagCondition condition)
        {
            return new TagEffect
            {
                Key = condition.Key,
                Value = condition.Value,
                Operation = condition.RequirePresence ? TagOperation.Add : TagOperation.Remove
            };
        }
    }

    /// <summary>
    /// タグ条件。ルール評価時にエンティティが条件を満たすかを判定する。
    /// </summary>
    [Serializable]
    public class TagCondition
    {
        /// <summary>判定対象のタグKey。</summary>
        public string Key;
        /// <summary>判定対象のタグValue（空=Keyのみで判定）。</summary>
        public string Value;
        /// <summary>true: タグが存在する場合に真。false: タグが存在しない場合に真。</summary>
        public bool RequirePresence = true;

        /// <summary>否定状態を考慮した条件判定。</summary>
        public bool Evaluate(TagContainer tags, bool isNegated)
        {
            bool hasTag;
            if (string.IsNullOrEmpty(Value))
                hasTag = tags.HasKey(Key);
            else
                hasTag = tags.HasTag(Key, Value);

            bool baseResult = RequirePresence ? hasTag : !hasTag;

            // 否定時は結果を反転
            return isNegated ? !baseResult : baseResult;
        }
    }

    /// <summary>
    /// タグ効果。ルール評価結果として対象エンティティに適用されるタグ操作。
    /// </summary>
    [Serializable]
    public class TagEffect
    {
        public string Key;
        public string Value;
        public TagOperation Operation;
        public int Duration;

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
                    tags.AddTag(new TagDefinition(Key, Value, Duration, source));
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
        Original,       // P→Q（元の命題）
        Converse,       // Q→P（逆）
        Inverse,        // ¬P→¬Q（裏）
        Contrapositive, // ¬Q→¬P（対偶）
        Invalid         // 不完全な操作状態
    }
}
