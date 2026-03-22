using System;
using UnityEngine;
using UnityEngine.UI;
using FlipLogic.Data;
using FlipLogic.Logic;
using FlipLogic.UI;

namespace FlipLogic.Battle
{
    /// <summary>
    /// ルール改変UIパネルの制御。
    /// 命題ブロック（条件P・帰結Q）の配置、ドラッグ＆ドロップ入替、否定トグルを管理する。
    /// </summary>
    public class RuleHackPanelController : MonoBehaviour
    {
        [Header("Proposition Blocks")]
        [SerializeField] private PropositionBlock _conditionBlock;
        [SerializeField] private PropositionBlock _resultBlock;

        [Header("UI Elements")]
        [SerializeField] private Text _connectorText;
        [SerializeField] private Text _stateLabel;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _resetButton;
        [SerializeField] private Button _swapButton;
        [SerializeField] private Text _propositionPreview;

        private RuleData _rule;
        private Action _onComplete;

        public void Initialize(RuleData rule, Action onComplete)
        {
            _rule = rule;
            _onComplete = onComplete;
            _rule.ResetAll();

            // ブロック初期化
            if (_conditionBlock != null) _conditionBlock.Setup(_rule.Condition, OnBlockChanged);
            if (_resultBlock != null) _resultBlock.Setup(_rule.Result, OnBlockChanged);

            if (_connectorText != null) _connectorText.text = "ならば";

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveAllListeners();
                _confirmButton.onClick.AddListener(OnConfirm);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveAllListeners();
                _resetButton.onClick.AddListener(OnReset);
            }

            if (_swapButton != null)
            {
                _swapButton.onClick.RemoveAllListeners();
                _swapButton.onClick.AddListener(SwapBlocks);
                _swapButton.interactable = _rule.IsSwappable;
            }

            UpdateDisplay();
        }

        /// <summary>
        /// ブロック位置入替（逆の操作）。外部から呼ばれる。
        /// </summary>
        public void SwapBlocks()
        {
            _rule.ToggleSwap();

            // ブロックの内容を入替
            if (_conditionBlock != null) _conditionBlock.Setup(_rule.DisplayCondition, OnBlockChanged);
            if (_resultBlock != null) _resultBlock.Setup(_rule.DisplayResult, OnBlockChanged);

            UpdateDisplay();
        }

        private void OnBlockChanged()
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_stateLabel != null)
                _stateLabel.text = LogicEvaluator.GetStateDisplayText(_rule);

            if (_propositionPreview != null)
                _propositionPreview.text = LogicEvaluator.FormatCurrentProposition(_rule);
        }

        private void OnConfirm()
        {
            _onComplete?.Invoke();
        }

        private void OnReset()
        {
            _rule.ResetAll();
            if (_conditionBlock != null) _conditionBlock.Setup(_rule.Condition, OnBlockChanged);
            if (_resultBlock != null) _resultBlock.Setup(_rule.Result, OnBlockChanged);
            UpdateDisplay();
        }
    }
}
