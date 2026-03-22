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
        private bool _isInBattle;

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

        public void StartBattle(GameEntity player, GameEntity enemy, bool isTutorial = false)
        {
            _playerEntity = player;
            _enemyEntity = enemy;
            IsTutorialBattle = isTutorial;
            _isInBattle = true;

            StartBattleSequenceAsync().Forget();
        }

        private async UniTask StartBattleSequenceAsync()
        {
            SetPhase(BattlePhase.Start);

            _uiController?.gameObject.SetActive(true);
            _uiController?.Initialize(this);
            _uiController?.UpdateHp(_playerEntity, _enemyEntity);

            await ShowMessageAsync(IsTutorialBattle ? "氷スライムが現れた！" : $"{_enemyEntity.EntityName}が現れた！");
            
            SetPhase(BattlePhase.PlayerCommand);
        }

        private void SetPhase(BattlePhase phase)
        {
            _currentPhase = phase;
            if (_uiController != null)
                _uiController.OnPhaseChanged(phase);
        }

        private async UniTask ShowMessageAsync(string message)
        {
            var tcs = new UniTaskCompletionSource();
            if (_uiController != null)
            {
                _uiController.ShowMessage(message, () => tcs.TrySetResult());
                await tcs.Task;
            }
        }

        public void ExecuteCommand(BattleCommandType type)
        {
            if (_currentPhase != BattlePhase.PlayerCommand) return;
            HandleCommandAsync(type).Forget();
        }

        private async UniTask HandleCommandAsync(BattleCommandType type)
        {
            switch (type)
            {
                case BattleCommandType.Attack:
                    await ExecuteAttackAsync();
                    break;
                case BattleCommandType.Defend:
                    BattleCommand.ExecuteDefend(_playerEntity);
                    await ShowMessageAsync("身を固めて防御した！");
                    await DoTurnEndAsync();
                    break;
                case BattleCommandType.Flee:
                    await EndBattleAsync("戦いから逃げ出した！", BattleResult.Fled);
                    break;
            }
        }

        private async UniTask ExecuteAttackAsync()
        {
            SetPhase(BattlePhase.PlayerAction);
            BattleCommand.ExecuteAttack(_playerEntity, _enemyEntity);
            
            if (_uiController != null)
                _uiController.UpdateHp(_playerEntity, _enemyEntity);

            await ShowMessageAsync($"{_enemyEntity.EntityName}に攻撃した！");
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

            // 全体ターン進行（ルール評価・タグ処理）を実行
            await TurnManager.Instance.OnPlayerActionAsync();

            // 評価後に敵が死んだ場合（即死ルールなどの影響）
            if (!_enemyEntity.IsAlive)
            {
                await EndBattleAsync($"{_enemyEntity.EntityName}は力尽きた！", BattleResult.Victory);
                return;
            }

            if (!_playerEntity.IsAlive)
            {
                await EndBattleAsync("力尽きた…", BattleResult.Defeat);
                return;
            }

            SetPhase(BattlePhase.PlayerCommand);
        }

        private async UniTask EndBattleAsync(string message, BattleResult result)
        {
            SetPhase(BattlePhase.BattleResult);
            await ShowMessageAsync(message);

            _isInBattle = false;
            if (_uiController != null)
                _uiController.gameObject.SetActive(false);

            OnBattleEnd?.Invoke(result);
            SetPhase(BattlePhase.End);
        }
    }
}
