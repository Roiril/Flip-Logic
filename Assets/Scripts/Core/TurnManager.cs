using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

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
        public async UniTaskVoid OnPlayerActionAsync()
        {
            if (CurrentPhase != TurnPhase.WaitingForInput) return;

            _currentTurn++;
            CurrentPhase = TurnPhase.PlayerAction;
            OnTurnStart?.Invoke(_currentTurn);

            OnPlayerActionComplete?.Invoke();

            // バトルが発生した場合は、バトル終了までフィールドのターン進行を一時停止する
            if (Battle.BattleManager.Instance != null && Battle.BattleManager.Instance.IsInBattle)
            {
                await UniTask.WaitWhile(() => Battle.BattleManager.Instance.IsInBattle);
            }

            CurrentPhase = TurnPhase.EnemyAction;
            OnEnemyPhase?.Invoke();

            // アニメーション用の簡易待機（エンティティが移動を終える猶予）
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

            // もし敵の行動によってバトルなどが発生した場合の念のための待機
            if (Battle.BattleManager.Instance != null && Battle.BattleManager.Instance.IsInBattle)
            {
                await UniTask.WaitWhile(() => Battle.BattleManager.Instance.IsInBattle);
            }

            CurrentPhase = TurnPhase.RuleEvaluation;
            
            // ターン終了判定・タグ処理の共通パイプライン呼び出し
            var results = await TurnResolutionProcessor.ExecuteAsync();
            OnRuleEvaluationComplete?.Invoke(results);

            CurrentPhase = TurnPhase.TagTick;

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
