using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlipLogic.Core
{
    /// <summary>
    /// 同期型ターン進行の管理。
    /// プレイヤーアクション → 全エンティティ行動 → Rule Evaluator一斉評価 → タグ期限処理
    /// の順でターンを回す。
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [SerializeField] private int _currentTurn;

        /// <summary>現在のターン数。</summary>
        public int CurrentTurn => _currentTurn;

        /// <summary>現在のフェーズ。</summary>
        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.WaitingForInput;

        /// <summary>ターン開始イベント。</summary>
        public event Action<int> OnTurnStart;

        /// <summary>プレイヤーアクション完了イベント。</summary>
        public event Action OnPlayerActionComplete;

        /// <summary>敵行動フェーズイベント。</summary>
        public event Action OnEnemyPhase;

        /// <summary>ルール評価完了イベント。</summary>
        public event Action<List<Logic.EvaluationResult>> OnRuleEvaluationComplete;

        /// <summary>ターン終了イベント。</summary>
        public event Action<int> OnTurnEnd;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// プレイヤーがアクション（移動 or コマンド）を実行した時に呼ぶ。
        /// 1ターンの進行サイクルを開始する。
        /// </summary>
        public void OnPlayerAction()
        {
            if (CurrentPhase != TurnPhase.WaitingForInput) return;

            _currentTurn++;
            CurrentPhase = TurnPhase.PlayerAction;
            OnTurnStart?.Invoke(_currentTurn);

            OnPlayerActionComplete?.Invoke();

            CurrentPhase = TurnPhase.EnemyAction;
            OnEnemyPhase?.Invoke();

            CurrentPhase = TurnPhase.RuleEvaluation;
            List<Logic.EvaluationResult> results = null;
            if (Logic.RuleEvaluator.Instance != null)
            {
                results = Logic.RuleEvaluator.Instance.EvaluateAll();
            }
            OnRuleEvaluationComplete?.Invoke(results);

            // 絶対法則（タグの振る舞い）の実行
            if (TagBehaviorRunner.Instance != null)
            {
                TagBehaviorRunner.Instance.ExecuteTurnEndBehaviors();
            }

            CurrentPhase = TurnPhase.TagTick;
            var entities = EntityRegistry.Instance.GetAllEntities();
            foreach (var entity in entities)
            {
                entity.Tags.TickDurations();
            }

            // マスのタグ期限処理
            if (Grid.GridMap.Instance != null)
            {
                Grid.GridMap.Instance.TickAllCellTags();
            }

            CurrentPhase = TurnPhase.WaitingForInput;
            OnTurnEnd?.Invoke(_currentTurn);
        }


        /// <summary>
        /// ターン数をリセットする（ルール初期化/セーフティネット用）。
        /// </summary>
        public void ResetTurns()
        {
            _currentTurn = 0;
            CurrentPhase = TurnPhase.WaitingForInput;
        }
    }

    /// <summary>
    /// ターン進行フェーズ。
    /// </summary>
    public enum TurnPhase
    {
        WaitingForInput,
        PlayerAction,
        EnemyAction,
        RuleEvaluation,
        TagTick,
    }
}
