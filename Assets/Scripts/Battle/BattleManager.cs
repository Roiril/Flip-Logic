using System;
using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Data;
using Cysharp.Threading.Tasks;

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

        public void SetUIController(BattleUIController controller)
        {
            _uiController = controller;
        }

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

            StartBattleFlowAsync(msg).Forget();
        }

        private async UniTaskVoid StartBattleFlowAsync(string msg)
        {
            await ShowMessageAsync(msg);
            SetPhase(BattlePhase.PlayerCommand);
        }

        public void ExecuteCommand(BattleCommandType cmd)
        {
            if (_currentPhase != BattlePhase.PlayerCommand) return;

            switch (cmd)
            {
                case BattleCommandType.Attack:
                    DoAttackAsync().Forget();
                    break;
                case BattleCommandType.Defend:
                    DoDefendAsync().Forget();
                    break;
                case BattleCommandType.Flee:
                    DoFleeAsync().Forget();
                    break;
                case BattleCommandType.OpenRulebook:
                    DoOpenRulebook();
                    break;
            }
        }

        private async UniTaskVoid DoAttackAsync()
        {
            SetPhase(BattlePhase.PlayerAction);
            BattleCommand.ExecuteAttack(_playerEntity, _enemyEntity);

            if (_uiController != null)
                _uiController.UpdateHp(_playerEntity, _enemyEntity);

            if (!_enemyEntity.IsAlive)
            {
                await EndBattleAsync($"{_enemyEntity.EntityName}を倒した！", BattleResult.Victory);
                return;
            }

            int damage = _playerEntity.Attack;
            await ShowMessageAsync($"{damage}のダメージを与えた！");

            await DoEnemyTurnAsync();
        }

        private async UniTaskVoid DoDefendAsync()
        {
            SetPhase(BattlePhase.PlayerAction);
            BattleCommand.ExecuteDefend(_playerEntity);
            await ShowMessageAsync("防御姿勢をとった");
            await DoEnemyTurnAsync();
        }

        private async UniTaskVoid DoFleeAsync()
        {
            SetPhase(BattlePhase.PlayerAction);
            if (UnityEngine.Random.value > 0.5f)
            {
                await EndBattleAsync("うまく逃げ切れた！", BattleResult.Fled);
            }
            else
            {
                await ShowMessageAsync("逃げられなかった！");
                await DoEnemyTurnAsync();
            }
        }

        private void DoOpenRulebook()
        {
            if (_activeRule == null)
            {
                ShowMessageAsync("ルールブックを開いた…（空白のページだ）").ContinueWith(() => SetPhase(BattlePhase.PlayerCommand)).Forget();
                return;
            }

            if (_hasUsedRulebook)
            {
                ShowMessageAsync("もうルールブックは使えない…").ContinueWith(() => SetPhase(BattlePhase.PlayerCommand)).Forget();
                return;
            }

            _hasUsedRulebook = true;
            SetPhase(BattlePhase.RuleHack);

            if (_uiController != null)
                _uiController.ShowRuleHackUI(_activeRule, OnRuleHackComplete);
        }

        private void OnRuleHackComplete()
        {
            OnRuleHackCompleteAsync().Forget();
        }

        private async UniTaskVoid OnRuleHackCompleteAsync()
        {
            string proposition = Logic.LogicEvaluator.FormatCurrentProposition(_activeRule);
            _showHackResult = true; // ルール改変直後フラグ

            await ShowMessageAsync($"ルールを改変した…\n「{proposition}」\n世界の法則が設定された");
            await DoEnemyTurnAsync();
        }

        private async UniTask DoEnemyTurnAsync()
        {
            SetPhase(BattlePhase.EnemyTurn);

            if (!_enemyEntity.IsAlive)
            {
                await EndBattleAsync($"{_enemyEntity.EntityName}を倒した！", BattleResult.Victory);
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
                await EndBattleAsync(msg + "\n\n力尽きた…", BattleResult.Defeat);
            }
            else
            {
                await ShowMessageAsync(msg);
                await DoTurnEndAsync();
            }
        }

        private async UniTask DoTurnEndAsync()
        {
            SetPhase(BattlePhase.TurnEnd);

            float preHp = _enemyEntity.Hp;

            // ターン終了時の共通処理（ルール評価・タグ期限更新）
            var results = await TurnResolutionProcessor.ExecuteAsync();

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
                
                await ShowMessageAsync(msg);
                await EndBattleAsync($"{_enemyEntity.EntityName}を倒した！", BattleResult.Victory);
                return;
            }

            if (!_playerEntity.IsAlive)
            {
                await EndBattleAsync("力尽きた…", BattleResult.Defeat);
                return;
            }

            // チュートリアル時、かつ、ルール改変したが死ななかった場合のアナウンス
            if (IsTutorialBattle && _showHackResult && _enemyEntity.IsAlive)
            {
                _showHackResult = false;
                await ShowMessageAsync("ルール改変の結果、\n氷スライムは生き延びた！");
                SetPhase(BattlePhase.PlayerCommand);
                return;
            }

            _showHackResult = false;
            SetPhase(BattlePhase.PlayerCommand);
        }

        private async UniTask EndBattleAsync(string msg, BattleResult result)
        {
            SetPhase(BattlePhase.BattleResult);
            await ShowMessageAsync(msg);
            
            _isInBattle = false;
            SetPhase(BattlePhase.End);
            if (_uiController != null)
                _uiController.gameObject.SetActive(false);
            OnBattleEnd?.Invoke(result);
        }

        private void SetPhase(BattlePhase phase)
        {
            _currentPhase = phase;
            if (_uiController != null)
                _uiController.OnPhaseChanged(phase);
        }

        private async UniTask ShowMessageAsync(string message)
        {
            if (_uiController != null)
            {
                bool isCompleted = false;
                _uiController.ShowMessage(message, () => isCompleted = true);
                await UniTask.WaitUntil(() => isCompleted);
            }
            else
            {
                Debug.Log($"[Battle] {message}");
                await UniTask.Yield();
            }
        }

        // 古い互換性用（一部外部から呼ばれる可能性を考慮）
        private void ShowMessage(string message, Action onComplete)
        {
            ShowMessageAsync(message).ContinueWith(onComplete).Forget();
        }
    }
}
