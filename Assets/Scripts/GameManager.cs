using UnityEngine;
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
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) _playerEntity = playerObj.GetComponent<GameEntity>();
            }

            // テスト用ルールを設定
            SetupTestRules();

            // バトルイベント登録
            if (_battleManager != null)
                _battleManager.OnBattleEnd += OnBattleEnd;
        }

        /// <summary>テスト用ルール設定。</summary>
        private void SetupTestRules()
        {
            // 第1章: 状態異常の法則
            var rule1 = new RuleData
            {
                RuleId = "rule_fire_ice",
                RuleName = "炎と氷の法則",
                Description = "火のマスに乗った氷スライムは即死する。\n※論理を書き換えることでスライムとマスの因果関係が変化します。",
                Chapter = 1,
                IsActive = true,
                Condition = new PropositionData("氷スライムが火のマスにいる", "氷スライムが火のマスにいない"),
                Result = new PropositionData("氷スライムは死ぬ", "氷スライムは死なない"),
                SubjectFilterP = new TagCondition { Target = RuleTarget.Entity, Key = "Element", Value = "Ice", RequirePresence = true },
                TagConditionP = new TagCondition { Target = RuleTarget.TileOfEntity, Key = "Element", Value = "Fire", RequirePresence = true },
                TagResultQ = new TagEffect { Target = RuleTarget.Entity, Key = "Status", Value = "InstantDeath", Operation = TagOperation.Add, Duration = 1, BehaviorId = "InstantDeath" },
            };

            var page1 = new RulePage
            {
                Chapter = 1,
                Title = "状態異常の法則",
                Description = "世界における属性と状態異常のルール"
            };
            page1.Rules.Add(rule1);

            _rulebookManager.AddPage(page1);
            _rulebookManager.UnlockChapter(1);

            // RuleEvaluatorにルールを供給
            if (_ruleEvaluator != null)
                _ruleEvaluator.SetRules(_rulebookManager.GetActiveRules());

            // ルール改変能力を解放（テスト用）
            _gameState.IsRuleHackUnlocked = true;

            Debug.Log($"[GameManager] テストルール設定完了: {_rulebookManager.Count}件");
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
