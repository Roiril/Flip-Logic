using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Grid;

namespace FlipLogic.Explore
{
    /// <summary>
    /// フィールド上の敵シンボル。GameEntityを継承し、
    /// ターン進行時にAI移動を実行する。
    /// </summary>
    [RequireComponent(typeof(GameEntity))]
    public class EnemySymbol : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private EnemyAIType _aiType = EnemyAIType.Patrol;
        [SerializeField] private int _detectRange = 3;

        public EnemyAIType AIType { get => _aiType; set => _aiType = value; }

        private GameEntity _entity;
        private Vector3 _targetWorldPos;
        private bool _isMoving;
        private int _patrolDir = 1;

        private void Awake()
        {
            _entity = GetComponent<GameEntity>();
        }

        private void Start()
        {
            if (GridMap.Instance != null)
            {
                _targetWorldPos = GridMap.Instance.GridToWorld(_entity.GridPosition);
                transform.position = _targetWorldPos;
                GridMap.Instance.RegisterEntity(_entity, _entity.GridPosition);
            }

            // TurnManagerの敵フェーズイベントに登録
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnEnemyPhase += OnEnemyTurn;
            }
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnEnemyPhase -= OnEnemyTurn;
            }
        }

        private void Update()
        {
            // 移動アニメーション
            if (_isMoving)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, _targetWorldPos, 8f * Time.deltaTime);
                if (Vector3.Distance(transform.position, _targetWorldPos) < 0.01f)
                {
                    transform.position = _targetWorldPos;
                    _isMoving = false;
                }
            }
        }

        /// <summary>敵ターン時に呼ばれるAI行動。</summary>
        private void OnEnemyTurn()
        {
            if (!_entity.IsAlive) return;

            switch (_aiType)
            {
                case EnemyAIType.Stationary:
                    break;
                case EnemyAIType.Patrol:
                    DoPatrol();
                    break;
                case EnemyAIType.Chase:
                    DoChase();
                    break;
            }
        }

        private void DoPatrol()
        {
            var newPos = _entity.GridPosition + new Vector2Int(_patrolDir, 0);
            if (GridMap.Instance != null && GridMap.Instance.IsWalkable(newPos))
            {
                MoveTo(newPos);
            }
            else
            {
                _patrolDir = -_patrolDir;
            }
        }

        private void DoChase()
        {
            // プレイヤーを EntityRegistry から取得
            var players = EntityRegistry.Instance.GetEntities(EntityType.Player);
            if (players.Count == 0) return;

            var playerEntity = players[0];
            if (playerEntity == null) return;

            var diff = playerEntity.GridPosition - _entity.GridPosition;
            var dist = Mathf.Abs(diff.x) + Mathf.Abs(diff.y);

            if (dist > _detectRange) return;

            // プレイヤーに向かって1マス移動
            Vector2Int moveDir = Vector2Int.zero;
            if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y))
                moveDir = new Vector2Int(diff.x > 0 ? 1 : -1, 0);
            else
                moveDir = new Vector2Int(0, diff.y > 0 ? 1 : -1);

            var newPos = _entity.GridPosition + moveDir;
            if (GridMap.Instance != null && GridMap.Instance.IsWalkable(newPos))
            {
                MoveTo(newPos);
            }
        }

        public void ForceMoveTo(Vector2Int pos)
        {
            MoveTo(pos);
        }

        private void MoveTo(Vector2Int pos)
        {
            _entity.GridPosition = pos;
            if (GridMap.Instance != null)
                GridMap.Instance.RegisterEntity(_entity, pos);
            _targetWorldPos = GridMap.Instance.GridToWorld(pos);
            _isMoving = true;
        }
    }

    /// <summary>敵AIの行動パターン。</summary>
    public enum EnemyAIType
    {
        Stationary, // 動かない
        Patrol,     // 往復巡回
        Chase,      // プレイヤー追跡
    }
}
