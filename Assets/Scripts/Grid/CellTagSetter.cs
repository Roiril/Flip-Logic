using System.Collections.Generic;
using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Grid;

namespace FlipLogic.Explore
{
    /// <summary>
    /// Scene上に配置してセルにタグを付与するコンポーネント。
    /// Start() で GridMap にタグを登録後、自身を破棄する。
    /// </summary>
    public class CellTagSetter : MonoBehaviour
    {
        [Header("Tags to Apply")]
        [SerializeField] private List<TagDefinition> _tags = new List<TagDefinition>();

        [Header("Options")]
        [Tooltip("生成後にこのGameObjectを破棄するか")]
        [SerializeField] private bool _destroyAfterApply = true;

        private void Start()
        {
            if (GridMap.Instance == null)
            {
                Debug.LogWarning($"[CellTagSetter] {gameObject.name}: GridMap が見つかりません。");
                return;
            }

            if (_tags.Count == 0)
            {
                Debug.LogWarning($"[CellTagSetter] {gameObject.name}: タグが未設定です。");
                return;
            }

            // 自身のワールド座標からグリッド座標を算出
            var gridPos = GridMap.Instance.WorldToGrid(transform.position);

            // タグを付与
            foreach (var tag in _tags)
            {
                GridMap.Instance.AddCellTag(gridPos, tag);
            }

            Debug.Log($"[CellTagSetter] セル ({gridPos.x}, {gridPos.y}) に {_tags.Count} 件のタグを付与しました。");

            // 自身を破棄
            if (_destroyAfterApply)
            {
                Destroy(gameObject);
            }
        }

        // エディタ上でタグ位置を視覚化
        private void OnDrawGizmos()
        {
            bool hasTags = _tags != null && _tags.Count > 0;
            Gizmos.color = hasTags ? new Color(1f, 0.6f, 0f, 0.5f) : Color.gray;
            Gizmos.DrawCube(transform.position, new Vector3(0.8f, 0.8f, 0.01f));

            if (hasTags)
            {
#if UNITY_EDITOR
                string label = _tags.Count == 1
                    ? $"{_tags[0].Key}:{_tags[0].Value}"
                    : $"{_tags.Count} tags";
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.6f,
                    label,
                    new GUIStyle { normal = { textColor = new Color(1f, 0.6f, 0f) }, fontSize = 10, alignment = TextAnchor.MiddleCenter }
                );
#endif
            }
        }
    }
}
