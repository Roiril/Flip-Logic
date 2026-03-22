using FlipLogic.Core;

namespace FlipLogic.Explore
{
    /// <summary>
    /// プレイヤーが干渉可能なオブジェクトのインターフェース。
    /// </summary>
    public interface IInteractable
    {
        /// <summary>表示名（UI等で使用）。</summary>
        string InteractionName { get; }

        /// <summary>現在インタラクト可能か。</summary>
        bool CanInteract { get; }

        /// <summary>プレイヤーからのインタラクト実行。</summary>
        void OnInteract(GameEntity player);
    }
}
