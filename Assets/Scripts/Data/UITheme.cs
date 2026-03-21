using UnityEngine;

namespace FlipLogic.Data
{
    /// <summary>
    /// ゲーム全体の主要な UI カラーテーマとフォントを管理する設定ファイル。
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "FlipLogic/Visual/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Panel Colors")]
        public Color MessagePanelColor = new Color(0.05f, 0.05f, 0.12f, 0.95f);
        public Color CommandPanelColor = new Color(0.08f, 0.08f, 0.15f, 0.9f);
        public Color HpPanelColor = new Color(0.03f, 0.03f, 0.08f, 0.85f);
        public Color RuleHackPanelColor = new Color(0.02f, 0.02f, 0.08f, 0.98f);
        public Color PropositionBlockColor = new Color(0.12f, 0.12f, 0.2f);

        [Header("Button Colors")]
        public Color AttackButtonColor = new Color(0.8f, 0.3f, 0.3f);
        public Color DefendButtonColor = new Color(0.3f, 0.5f, 0.8f);
        public Color RulebookButtonColor = new Color(0.1f, 0.8f, 0.7f);
        public Color FleeButtonColor = new Color(0.5f, 0.5f, 0.5f);
        
        public Color SwapButtonColor = new Color(0.3f, 0.6f, 0.3f);
        public Color ResetButtonColor = new Color(0.5f, 0.5f, 0.5f);
        public Color ConfirmButtonColor = new Color(0.2f, 0.5f, 0.8f);
        public Color NegateButtonColor = new Color(0.6f, 0.2f, 0.2f);

        [Header("Text Typography")]
        public Font BaseFont;
        
        [Header("Text Colors")]
        public Color DefaultTextColor = Color.white;
        public Color EnemyNameTextColor = new Color(1f, 0.4f, 0.4f);
        public Color EnemyHpTextColor = new Color(1f, 0.8f, 0.8f);
        public Color PlayerHpTextColor = new Color(0.8f, 1f, 0.8f);
        
        public Color RuleHackTitleColor = new Color(0f, 1f, 0.8f);
        public Color RuleHackConnectorColor = new Color(0.8f, 0.8f, 0.3f);
        public Color RuleHackStateLabelColor = new Color(0.5f, 0.8f, 1f);
        public Color RuleHackPreviewColor = new Color(0.7f, 0.7f, 0.7f);

        /// <summary>
        /// フォントの安全な取得。未設定の場合は LegacyRuntime.ttf を返す。
        /// </summary>
        public Font GetSafeFont()
        {
            if (BaseFont != null) return BaseFont;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
