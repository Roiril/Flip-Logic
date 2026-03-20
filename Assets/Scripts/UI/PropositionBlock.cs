using System;
using UnityEngine;
using UnityEngine.UI;
using FlipLogic.Data;

namespace FlipLogic.UI
{
    /// <summary>
    /// 命題ブロックUI。テキスト表示、否定トグルボタン、ドラッグ用ハンドルを含む。
    /// ドラッグ操作（IDragHandler）とクリック操作（Button.OnClick）の処理領域を
    /// GameObjectレベルで物理的に分離する設計。
    /// </summary>
    public class PropositionBlock : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Text _contentText;
        [SerializeField] private Button _negateButton;
        [SerializeField] private Text _negateButtonText;
        [SerializeField] private Image _blockBackground;
        [SerializeField] private GameObject _dragHandle;

        private PropositionData _data;
        private Action _onChanged;

        // 否定状態の視覚設定
        private readonly Color _normalBgColor = new Color(0.2f, 0.2f, 0.25f);
        private readonly Color _negatedBgColor = new Color(0.35f, 0.1f, 0.1f);
        private readonly Color _normalTextColor = Color.white;
        private readonly Color _negatedTextColor = new Color(1f, 0.5f, 0.5f);

        /// <summary>
        /// 命題データでブロックを初期化する。
        /// </summary>
        public void Setup(PropositionData data, Action onChanged)
        {
            _data = data;
            _onChanged = onChanged;

            if (_negateButton != null)
            {
                _negateButton.onClick.RemoveAllListeners();
                _negateButton.onClick.AddListener(OnNegateClicked);
            }

            UpdateDisplay();
        }

        private void OnNegateClicked()
        {
            if (_data == null) return;
            _data.ToggleNegation();
            UpdateDisplay();
            _onChanged?.Invoke();
        }

        private void UpdateDisplay()
        {
            if (_data == null) return;

            if (_contentText != null)
            {
                _contentText.text = _data.CurrentText;
                _contentText.color = _data.IsNegated ? _negatedTextColor : _normalTextColor;
                _contentText.fontStyle = _data.IsNegated ? FontStyle.Italic : FontStyle.Bold;
            }

            if (_blockBackground != null)
                _blockBackground.color = _data.IsNegated ? _negatedBgColor : _normalBgColor;

            if (_negateButtonText != null)
                _negateButtonText.text = _data.IsNegated ? "肯定" : "否定";
        }

        public PropositionData Data => _data;
    }
}
