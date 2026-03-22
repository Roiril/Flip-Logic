using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Grid;
using FlipLogic.UI;

namespace FlipLogic.Explore
{
    /// <summary>
    /// ステージ上に配置されるルールボードのインタラクト制御。
    /// プレイヤーが接触してボタン等でインタラクトすると、ルール改変UIを開く。
    /// </summary>
    [RequireComponent(typeof(GameEntity))]
    public class RuleBoardInteractable : MonoBehaviour, IInteractable
    {
        [Header("Settings")]
        [SerializeField] private string _boardName = "掲示板";
        [SerializeField] private bool _canInteract = true;
        [SerializeField] private Vector2Int _gridPosition;

        private GameEntity _entity;

        private void Awake()
        {
            _entity = GetComponent<GameEntity>();
        }

        /// <summary>表示名。</summary>
        public string InteractionName => _boardName;

        /// <summary>インタラクト可能か。</summary>
        public bool CanInteract => _canInteract;

        /// <summary>初期化。MapPlacementSpawnerから呼ばれる。</summary>
        public void Initialize(Vector2Int gridPos)
        {
            _gridPosition = gridPos;
            
            // GameEntityの初期化（GridMapへの登録含む）
            if (_entity != null)
            {
                _entity.Initialize(_boardName, EntityType.Object, gridPos, maxHp: 9999);
            }

            // 物理位置を同期
            if (GridMap.Instance != null)
            {
                transform.position = GridMap.Instance.GridToWorld(_gridPosition);
                GridMap.Instance.RegisterEntity(_entity, _gridPosition);
            }
        }

        /// <summary>
        /// プレイヤーからのインタラクト実行。
        /// Globalなルール改変UI（RuleBoardUIController）を呼び出す。
        /// </summary>
        public void OnInteract(GameEntity player)
        {
            if (!_canInteract) return;

            Debug.Log($"[RuleBoard] {player.EntityName} が {_boardName} を調べた。");

            // UIを開く
            if (RuleBoardUIController.Instance != null)
            {
                RuleBoardUIController.Instance.OpenBoard();
            }
            else
            {
                Debug.LogWarning("[RuleBoard] RuleBoardUIController.Instance が見つかりません。");
            }
        }

        public void SetInteractable(bool state)
        {
            _canInteract = state;
        }
    }
}
