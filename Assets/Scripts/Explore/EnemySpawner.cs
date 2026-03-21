using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Data;
using FlipLogic.Grid;

namespace FlipLogic.Explore
{
    /// <summary>
    /// Scene上に配置して敵の出現位置を定義するコンポーネント。
    /// Start() で EntityFactory 経由の敵生成を行い、自身を破棄する。
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy Definition")]
        [SerializeField] private EnemyData _enemyData;

        [Header("Options")]
        [Tooltip("生成後にこのGameObjectを破棄するか")]
        [SerializeField] private bool _destroyAfterSpawn = true;

        private void Start()
        {
            if (_enemyData == null)
            {
                Debug.LogWarning($"[EnemySpawner] {gameObject.name}: EnemyData が未設定です。");
                return;
            }

            // 自身のワールド座標からグリッド座標を算出
            Vector2Int gridPos;
            if (GridMap.Instance != null)
            {
                gridPos = GridMap.Instance.WorldToGrid(transform.position);
            }
            else
            {
                gridPos = new Vector2Int(
                    Mathf.FloorToInt(transform.position.x),
                    Mathf.FloorToInt(transform.position.y)
                );
            }

            // EntityFactory で敵を生成
            var entity = EntityFactory.CreateEnemy(_enemyData, gridPos, transform.parent);
            if (entity != null)
            {
                Debug.Log($"[EnemySpawner] '{_enemyData.EnemyName}' を ({gridPos.x}, {gridPos.y}) に生成しました。");
            }

            // 自身を破棄
            if (_destroyAfterSpawn)
            {
                Destroy(gameObject);
            }
        }

        // エディタ上でスポーン位置を視覚化
        private void OnDrawGizmos()
        {
            Gizmos.color = _enemyData != null ? new Color(1f, 0.3f, 0.3f, 0.7f) : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.4f);

            if (_enemyData != null)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.6f,
                    _enemyData.EnemyName,
                    new GUIStyle { normal = { textColor = Color.red }, fontSize = 10, alignment = TextAnchor.MiddleCenter }
                );
#endif
            }
        }
    }
}
