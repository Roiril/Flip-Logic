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

            Debug.Log($"[TurnManager] ターン {_currentTurn} 開始");

            // プレイヤーアクション完了通知
            OnPlayerActionComplete?.Invoke();

            // 敵行動フェーズ
            CurrentPhase = TurnPhase.EnemyAction;
            OnEnemyPhase?.Invoke();

            // Rule Evaluatorによる全ルール一斉評価
            CurrentPhase = TurnPhase.RuleEvaluation;
            List<Logic.EvaluationResult> results = null;
            if (Logic.RuleEvaluator.Instance != null)
            {
                results = Logic.RuleEvaluator.Instance.EvaluateAll();
            }
            OnRuleEvaluationComplete?.Invoke(results);

            // 全エンティティのタグ期限処理
            CurrentPhase = TurnPhase.TagTick;
            var entities = FindObjectsByType<GameEntity>(FindObjectsSortMode.None);
            foreach (var entity in entities)
            {
                var expired = entity.Tags.TickDurations();
                foreach (var tag in expired)
                {
                    Debug.Log($"[TurnManager] {entity.EntityName} のタグ {tag} が期限切れ");
                }
            }

            // ターン完了
            CurrentPhase = TurnPhase.WaitingForInput;
            OnTurnEnd?.Invoke(_currentTurn);
            Debug.Log($"[TurnManager] ターン {_currentTurn} 終了 — 入力待ち");
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
