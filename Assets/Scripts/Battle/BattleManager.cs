using System;
using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Data;

namespace FlipLogic.Battle
{
    /// <summary>
    /// バトルの進行管理。フィールド統合型。
    /// タグベースの判定とコマンド実行を統括する。
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private BattleUIController _uiController;

        private GameEntity _playerEntity;
        private GameEntity _enemyEntity;
        private BattlePhase _currentPhase = BattlePhase.None;
        private RuleData _activeRule;
        private bool _isInBattle;
        private bool _hasUsedRulebook;
        private bool _showHackResult;

        public event Action<BattleResult> OnBattleEnd;

        public BattlePhase CurrentPhase => _currentPhase;
        public bool IsInBattle => _isInBattle;
        public bool IsTutorialBattle { get; set; }
        public GameEntity EnemyEntity => _enemyEntity;
        public GameEntity PlayerEntity => _playerEntity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // UIControllerが未設定なら自動検出
            if (_uiController == null)
                _uiController = FindAnyObjectByType<BattleUIController>();
        }

        /// <summary>UIControllerを外部から設定する。</summary>
        public void SetUIController(BattleUIController controller)
        {
            _uiController = controller;
        }

        /// <summary>
        /// バトルを開始する。フィールド上でシームレスに移行。
        /// </summary>
        public void StartBattle(GameEntity player, GameEntity enemy, RuleData rule = null, bool isTutorial = false)
        {
            _playerEntity = player;
            _enemyEntity = enemy;
            _activeRule = rule;
            IsTutorialBattle = isTutorial;
            _isInBattle = true;
            _hasUsedRulebook = false;
            _showHackResult = false;

            SetPhase(BattlePhase.Start);

            if (_uiController != null)
            {
                _uiController.gameObject.SetActive(true);
                _uiController.Initialize(this);
                _uiController.UpdateHp(player, enemy);
            }

            string msg = enemy.Tags.HasKey("EncounterMsg")
                ? enemy.Tags.GetValue("EncounterMsg")
                : $"{enemy.EntityName} が現れた！";

            ShowMessage(msg, () => SetPhase(BattlePhase.PlayerCommand));
        }

        /// <summary>プレイヤーコマンドを実行する。</summary>
        public void ExecuteCommand(BattleCommandType cmd)
        {
            if (_currentPhase != BattlePhase.PlayerCommand) return;

            switch (cmd)
            {
                case BattleCommandType.Attack:
                    DoAttack();
                    break;
                case BattleCommandType.Defend:
                    DoDefend();
                    break;
                case BattleCommandType.Flee:
                    DoFlee();
                    break;
                case BattleCommandType.OpenRulebook:
                    DoOpenRulebook();
                    break;
            }
        }

        private void DoAttack()
        {
            SetPhase(BattlePhase.PlayerAction);
            BattleCommand.ExecuteAttack(_playerEntity, _enemyEntity);

            if (_uiController != null)
                _uiController.UpdateHp(_playerEntity, _enemyEntity);

            if (!_enemyEntity.IsAlive)
            {
                EndBattle($"{_enemyEntity.EntityName}を倒した！", BattleResult.Victory);
            }
            else
            {
                int damage = _playerEntity.Attack;
                ShowMessage($"{damage}のダメージを与えた！", () => DoEnemyTurn());
            }
        }

        private void DoDefend()
        {
            SetPhase(BattlePhase.PlayerAction);
            BattleCommand.ExecuteDefend(_playerEntity);
            ShowMessage("防御姿勢をとった", () => DoEnemyTurn());
        }

        private void DoFlee()
        {
            SetPhase(BattlePhase.PlayerAction);
            if (UnityEngine.Random.value > 0.5f)
            {
                EndBattle("うまく逃げ切れた！", BattleResult.Fled);
            }
            else
            {
                ShowMessage("逃げられなかった！", () => DoEnemyTurn());
            }
        }

        private void DoOpenRulebook()
        {
            if (_activeRule == null)
            {
                ShowMessage("ルールブックを開いた…（空白のページだ）",
                    () => SetPhase(BattlePhase.PlayerCommand));
                return;
            }

            if (_hasUsedRulebook)
            {
                ShowMessage("もうルールブックは使えない…",
                    () => SetPhase(BattlePhase.PlayerCommand));
                return;
            }

            _hasUsedRulebook = true;
            SetPhase(BattlePhase.RuleHack);

            if (_uiController != null)
                _uiController.ShowRuleHackUI(_activeRule, OnRuleHackComplete);
        }

        private void OnRuleHackComplete()
        {
            string proposition = Logic.LogicEvaluator.FormatCurrentProposition(_activeRule);

            _showHackResult = true; // ルール改変直後フラグ

            ShowMessage(
                $"ルールを改変した…\n「{proposition}」\n世界の法則が設定された",
                () => DoEnemyTurn());
        }

        private void DoEnemyTurn()
        {
            SetPhase(BattlePhase.EnemyTurn);

            if (!_enemyEntity.IsAlive)
            {
                EndBattle($"{_enemyEntity.EntityName}を倒した！", BattleResult.Victory);
                return;
            }

            // 敵の攻撃（タグベース）
            BattleCommand.ExecuteAttack(_enemyEntity, _playerEntity);

            if (_uiController != null)
                _uiController.UpdateHp(_playerEntity, _enemyEntity);

            int damage = Mathf.Max(1, _enemyEntity.Attack - _playerEntity.Defense);
            string msg = $"{_enemyEntity.EntityName}の攻撃！\n{damage}のダメージを受けた！";

            if (!_playerEntity.IsAlive)
            {
                EndBattle(msg + "\n\n力尽きた…", BattleResult.Defeat);
            }
            else
            {
                ShowMessage(msg, () => DoTurnEnd());
            }
        }

        private void DoTurnEnd()
        {
            SetPhase(BattlePhase.TurnEnd);

            float preHp = _enemyEntity.Hp;

            // ターン終了時のルール・タグ評価
            var results = Logic.RuleEvaluator.Instance?.EvaluateAll();
            Core.TagBehaviorRunner.Instance?.ExecuteTurnEndBehaviors();

            _playerEntity.Tags.TickDurations();
            _enemyEntity.Tags.TickDurations();
            Grid.GridMap.Instance?.TickAllCellTags();

            // 評価後に敵が死んだ場合（即死ルールなどの影響）
            if (preHp > 0 && !_enemyEntity.IsAlive)
            {
                string ruleName = "未知の現象";
                if (results != null)
                {
                    foreach (var res in results)
                    {
                        if (res.TargetEntity == _enemyEntity && res.AppliedEffect.Key == "Status")
                        {
                            ruleName = $"ルールの効果「{Logic.LogicEvaluator.FormatCurrentProposition(res.Rule)}」";
                            break;
                        }
                    }
                }
                
                _showHackResult = false;
                string msg = IsTutorialBattle 
                    ? $"{ruleName} により、\n氷スライムは死んだ！"
                    : $"{ruleName} の影響により、\n{_enemyEntity.EntityName}は力尽きた！";
                
                ShowMessage(msg, () => { EndBattle($"{_enemyEntity.EntityName}を倒した！", BattleResult.Victory); });
                return;
            }

            if (!_playerEntity.IsAlive)
            {
                EndBattle("力尽きた…", BattleResult.Defeat);
                return;
            }

            // チュートリアル時、かつ、ルール改変したが死ななかった場合のアナウンス
            if (IsTutorialBattle && _showHackResult && _enemyEntity.IsAlive)
            {
                _showHackResult = false;
                ShowMessage("ルール改変の結果、\n氷スライムは生き延びた！", () =>
                {
                    SetPhase(BattlePhase.PlayerCommand);
                });
                return;
            }

            _showHackResult = false;
            SetPhase(BattlePhase.PlayerCommand);
        }

        private void EndBattle(string msg, BattleResult result)
        {
            SetPhase(BattlePhase.BattleResult);
            ShowMessage(msg, () =>
            {
                _isInBattle = false;
                SetPhase(BattlePhase.End);
                if (_uiController != null)
                    _uiController.gameObject.SetActive(false);
                OnBattleEnd?.Invoke(result);
            });
        }

        private void SetPhase(BattlePhase phase)
        {
            _currentPhase = phase;
            if (_uiController != null)
                _uiController.OnPhaseChanged(phase);
        }

        private void ShowMessage(string message, Action onComplete)
        {
            if (_uiController != null)
            {
                _uiController.ShowMessage(message, onComplete);
            }
            else
            {
                Debug.Log($"[Battle] {message}");
                onComplete?.Invoke();
            }
        }
    }
}
