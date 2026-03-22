using UnityEngine;
using UnityEngine.UI;
using FlipLogic.Data;
using FlipLogic.Logic;

namespace FlipLogic.UI
{
    public class RuleRowUI : MonoBehaviour
    {
        [SerializeField] private PropositionBlock _conditionBlock;
        [SerializeField] private PropositionBlock _resultBlock;
        [SerializeField] private Text _connectorText;
        [SerializeField] private Button _swapButton;

        private RuleData _rule;

        public void Setup(RuleData rule)
        {
            _rule = rule;
            RenderUI();

            if (_swapButton != null)
            {
                _swapButton.onClick.RemoveAllListeners();
                _swapButton.onClick.AddListener(() => {
                    _rule.ToggleSwap();
                    RenderUI();
                });
                _swapButton.interactable = _rule.IsSwappable;
            }
        }

        private void RenderUI()
        {
            if (_conditionBlock != null) _conditionBlock.Setup(_rule.DisplayCondition, RenderUI);
            if (_resultBlock != null) _resultBlock.Setup(_rule.DisplayResult, RenderUI);
            if (_connectorText != null) _connectorText.text = "ならば";
        }
    }
}
