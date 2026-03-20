using System;
using UnityEngine;
using UnityEngine.UI;
using FlipLogic.Core;
using FlipLogic.Data;

namespace FlipLogic.Battle
{
    /// <summary>
    /// バトル画面のUI制御。コマンドメニュー、HP表示、メッセージウィンドウを管理する。
    /// </summary>
    public class BattleUIController : MonoBehaviour
    {
        [Header("Message")]
        [SerializeField] private Text _messageText;
        [SerializeField] private GameObject _messagePanel;

        [Header("Command Menu")]
        [SerializeField] private GameObject _commandPanel;
        [SerializeField] private Button _attackButton;
        [SerializeField] private Button _defendButton;
        [SerializeField] private Button _fleeButton;
        [SerializeField] private Button _rulebookButton;

        [Header("HP Display")]
        [SerializeField] private Text _playerHpText;
        [SerializeField] private Text _enemyHpText;
        [SerializeField] private Text _enemyNameText;

        [Header("Rule Hack UI")]
        [SerializeField] private GameObject _ruleHackPanel;

        private BattleManager _battleManager;
        private Action _messageCallback;
        private bool _waitingForTap;

        public void Initialize(BattleManager battleManager)
        {
            _battleManager = battleManager;

            if (_attackButton != null)
            {
                _attackButton.onClick.RemoveAllListeners();
                _attackButton.onClick.AddListener(() => _battleManager.ExecuteCommand(BattleCommandType.Attack));
            }
            if (_defendButton != null)
            {
                _defendButton.onClick.RemoveAllListeners();
                _defendButton.onClick.AddListener(() => _battleManager.ExecuteCommand(BattleCommandType.Defend));
            }
            if (_fleeButton != null)
            {
                _fleeButton.onClick.RemoveAllListeners();
                _fleeButton.onClick.AddListener(() => _battleManager.ExecuteCommand(BattleCommandType.Flee));
            }
            if (_rulebookButton != null)
            {
                _rulebookButton.onClick.RemoveAllListeners();
                _rulebookButton.onClick.AddListener(() => _battleManager.ExecuteCommand(BattleCommandType.OpenRulebook));
            }
        }

        /// <summary>バトルフェーズ変更時の表示切替。</summary>
        public void OnPhaseChanged(BattlePhase phase)
        {
            bool showCommands = phase == BattlePhase.PlayerCommand;
            if (_commandPanel != null) _commandPanel.SetActive(showCommands);

            bool showRuleHack = phase == BattlePhase.RuleHack;
            if (_ruleHackPanel != null) _ruleHackPanel.SetActive(showRuleHack);
        }

        /// <summary>メッセージを表示し、タップで次へ進む。</summary>
        public void ShowMessage(string message, Action onComplete)
        {
            if (_messagePanel != null) _messagePanel.SetActive(true);
            if (_messageText != null) _messageText.text = message;

            _messageCallback = onComplete;
            _waitingForTap = true;
        }

        private void Update()
        {
            if (_waitingForTap && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                _waitingForTap = false;
                if (_messagePanel != null) _messagePanel.SetActive(false);
                var callback = _messageCallback;
                _messageCallback = null;
                callback?.Invoke();
            }
        }

        /// <summary>HP表示を更新する（GameEntityベース）。</summary>
        public void UpdateHp(GameEntity player, GameEntity enemy)
        {
            if (_playerHpText != null)
                _playerHpText.text = $"HP: {player.Hp}/{player.MaxHp}";
            if (_enemyHpText != null)
                _enemyHpText.text = $"HP: {enemy.Hp}/{enemy.MaxHp}";
            if (_enemyNameText != null)
                _enemyNameText.text = enemy.EntityName;
        }

        /// <summary>ルール改変UIを表示する。</summary>
        public void ShowRuleHackUI(RuleData rule, Action onComplete)
        {
            if (_ruleHackPanel != null) _ruleHackPanel.SetActive(true);

            var controller = _ruleHackPanel.GetComponent<RuleHackPanelController>();
            if (controller != null)
            {
                controller.Initialize(rule, () =>
                {
                    if (_ruleHackPanel != null) _ruleHackPanel.SetActive(false);
                    onComplete?.Invoke();
                });
            }
        }
    }
}
