using UnityEngine;
using System.Linq;
using FlipLogic.Data;
using FlipLogic.Battle;
using FlipLogic.Rulebook;
using FlipLogic.Logic;

namespace FlipLogic.Core
{
    /// <summary>
    /// ゲーム全体の統括マネージャー。
    /// TurnManager/RuleEvaluator/RulebookManagerの初期化・連携ハブ。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player")]
        [SerializeField] private GameEntity _playerEntity;

        [Header("Systems")]
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private RuleEvaluator _ruleEvaluator;
        [SerializeField] private BattleManager _battleManager;

        [Header("Data")]
        [SerializeField] private RulebookAsset _initialRulebook;

        private GameState _gameState;
        private RulebookManager _rulebookManager;

        public GameState State => _gameState;
        public RulebookManager RulebookData => _rulebookManager;
        public GameEntity PlayerEntity => _playerEntity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _gameState = new GameState();
            _rulebookManager = new RulebookManager();
        }

        private void Start()
        {
            // システム参照の自動取得
            if (_turnManager == null) _turnManager = FindAnyObjectByType<TurnManager>();
            if (_ruleEvaluator == null) _ruleEvaluator = FindAnyObjectByType<RuleEvaluator>();
            if (_battleManager == null) _battleManager = FindAnyObjectByType<BattleManager>();
            if (_playerEntity == null)
            {
                _playerEntity = EntityRegistry.Instance.GetEntities(EntityType.Player).FirstOrDefault();
            }

            // ルールブックの読み込み
            if (_initialRulebook != null)
            {
                _rulebookManager.LoadFromAsset(_initialRulebook);
                _rulebookManager.UnlockChapter(1);
            }
            else
            {
                Debug.LogWarning("[GameManager] InitialRulebook が設定されていません。");
            }

            // RuleEvaluatorにルールを供給
            if (_ruleEvaluator != null)
            {
                _ruleEvaluator.SetRules(_rulebookManager.GetActiveRules());
            }

            // ルール改変能力を解放（テスト用。のちにシナリオ進行で制御する）
            _gameState.IsRuleHackUnlocked = true;

            // バトルイベント登録
            if (_battleManager != null)
                _battleManager.OnBattleEnd += OnBattleEnd;
        }

        /// <summary>セーフティネット: 全ルールを初期状態に戻す。</summary>
        public void ResetAllRules()
        {
            _rulebookManager.ResetAllRules();
            if (_ruleEvaluator != null)
                _ruleEvaluator.SetRules(_rulebookManager.GetActiveRules());

            Debug.Log("[GameManager] デバッグモード：設定の初期化を実行");
        }

        private void OnBattleEnd(BattleResult result)
        {
            Debug.Log($"[GameManager] バトル終了: {result}");
        }
    }
}
