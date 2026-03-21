using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Grid;

namespace FlipLogic.Explore
{
    /// <summary>
    /// プレイヤーのグリッド移動制御。
    /// 1マス移動 = 1ターン消費。TurnManagerと連携する。
    /// </summary>
    [RequireComponent(typeof(GameEntity))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveAnimSpeed = 8f;

        private GameEntity _entity;
        private Vector3 _targetWorldPos;
        private bool _isMoving;

        /// <summary>移動中か。</summary>
        public bool IsMoving => _isMoving;

        private void Awake()
        {
            _entity = GetComponent<GameEntity>();
        }

        private void Start()
        {
            // 初期位置を同期
            if (GridMap.Instance != null)
            {
                _targetWorldPos = GridMap.Instance.GridToWorld(_entity.GridPosition);
                transform.position = _targetWorldPos;
                GridMap.Instance.RegisterEntity(_entity, _entity.GridPosition);
            }
        }

        private void Update()
        {
            // 移動アニメーション中は入力を受け付けない
            if (_isMoving)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, _targetWorldPos, _moveAnimSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, _targetWorldPos) < 0.01f)
                {
                    transform.position = _targetWorldPos;
                    _isMoving = false;
                }
                return;
            }

            // バトル中は移動不可
            if (Battle.BattleManager.Instance != null && Battle.BattleManager.Instance.IsInBattle) return;

            // シナリオによる移動ブロック
            if (Scenario.ScenarioRunner.Instance != null && Scenario.ScenarioRunner.Instance.IsMovementBlocked) return;

            // ターン待ち状態でなければ入力を受け付けない
            if (TurnManager.Instance == null) return;
            if (TurnManager.Instance.CurrentPhase != TurnPhase.WaitingForInput) return;

            // 入力判定（グリッド1マス移動）
            Vector2Int dir = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                dir = Vector2Int.up;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                dir = Vector2Int.down;
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                dir = Vector2Int.left;
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                dir = Vector2Int.right;

            if (dir != Vector2Int.zero)
            {
                TryMove(dir);
            }
        }

        /// <summary>指定方向に1マス移動を試みる。</summary>
        private void TryMove(Vector2Int direction)
        {
            if (GridMap.Instance == null) return;

            var newPos = _entity.GridPosition + direction;

            // 通行可能判定
            if (!GridMap.Instance.IsWalkable(newPos)) return;

            // 移動実行
            _entity.GridPosition = newPos;
            GridMap.Instance.RegisterEntity(_entity, newPos);
            _targetWorldPos = GridMap.Instance.GridToWorld(newPos);
            _isMoving = true;

            // 1ターン消費
            TurnManager.Instance.OnPlayerAction();
        }
    }
}
