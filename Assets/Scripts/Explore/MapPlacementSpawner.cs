using UnityEngine;
using FlipLogic.Data;
using FlipLogic.Core;
using FlipLogic.Grid;

namespace FlipLogic.Explore
{
    /// <summary>
    /// シーン開始時に MapPlacementData の内容（敵・タグ）を生成するコンポーネント。
    /// 旧 EnemySpawner / CellTagSetter の役割を一括で行う。
    /// </summary>
    public class MapPlacementSpawner : MonoBehaviour
    {
        [Header("Placement Data")]
        [SerializeField] private MapPlacementData _placementData;

        private void Start()
        {
            if (_placementData == null)
            {
                Debug.LogWarning($"[{nameof(MapPlacementSpawner)}] MapPlacementData が設定されていません。");
                return;
            }

            SpawnAll();
        }

        private void SpawnAll()
        {
            var gridMap = GridMap.Instance;
            if (gridMap == null)
            {
                Debug.LogWarning($"[{nameof(MapPlacementSpawner)}] GridMap が見つかりません。");
                return;
            }

            // セルタグの配置
            foreach (var cellTag in _placementData.CellTags)
            {
                foreach (var tagDef in cellTag.Tags)
                {
                    gridMap.AddCellTag(cellTag.GridPosition, tagDef);
                }
            }

            // 敵の配置
            foreach (var enemy in _placementData.Enemies)
            {
                if (enemy.EnemyData != null)
                {
                    EntityFactory.CreateEnemy(enemy.EnemyData, enemy.GridPosition, transform);
                }
            }

            // ルールボードの配置
            foreach (var board in _placementData.RuleBoards)
            {
                var go = new GameObject($"RuleBoard_{board.GridPosition}");
                go.transform.SetParent(transform);
                go.AddComponent<GameEntity>(); // GameEntityを必須化
                var interactable = go.AddComponent<RuleBoardInteractable>();
                interactable.Initialize(board.GridPosition);

                // 簡易的な見た目
                var sprite = go.AddComponent<SpriteRenderer>();
                sprite.color = new Color(0.6f, 0.4f, 0.2f); // 茶色
                sprite.sortingLayerName = "Default";
                sprite.sortingOrder = 5; // エンティティより手前に表示
                
                // 1x1の白スプライトを生成して設定
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                sprite.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            Debug.Log($"[{nameof(MapPlacementSpawner)}] マップ配置データを生成完了しました。（敵: {_placementData.Enemies.Count}体、タグ: {_placementData.CellTags.Count}マス、ボード: {_placementData.RuleBoards.Count}つ）");

            // レンダラーに反映
            if (TileOverlayRenderer.Instance != null)
            {
                TileOverlayRenderer.Instance.RebuildAll();
            }
        }

        private void OnDrawGizmos()
        {
            if (_placementData == null) return;

            var gridMap = GridMap.Instance;
            
            // タグのプレビュー
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
            foreach (var cellTag in _placementData.CellTags)
            {
                Vector3 pos = gridMap != null ? gridMap.GridToWorld(cellTag.GridPosition) : new Vector3(cellTag.GridPosition.x + 0.5f, cellTag.GridPosition.y + 0.5f, 0f);
                Gizmos.DrawCube(pos, new Vector3(0.8f, 0.8f, 0.01f));
                
#if UNITY_EDITOR
                if (cellTag.Tags.Count > 0)
                {
                    string label = cellTag.Tags.Count == 1 ? $"{cellTag.Tags[0].Key}:{cellTag.Tags[0].Value}" : $"{cellTag.Tags.Count} tags";
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.6f, label, new GUIStyle { normal = { textColor = new Color(1f, 0.6f, 0f) }, fontSize = 10, alignment = TextAnchor.MiddleCenter });
                }
#endif
            }

            // 敵のプレビュー
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.7f);
            foreach (var enemy in _placementData.Enemies)
            {
                Vector3 pos = gridMap != null ? gridMap.GridToWorld(enemy.GridPosition) : new Vector3(enemy.GridPosition.x + 0.5f, enemy.GridPosition.y + 0.5f, 0f);
                Gizmos.DrawWireSphere(pos, 0.4f);

#if UNITY_EDITOR
                if (enemy.EnemyData != null)
                {
                    UnityEditor.Handles.Label(pos + Vector3.up * 0.6f, enemy.EnemyData.EnemyName, new GUIStyle { normal = { textColor = Color.red }, fontSize = 10, alignment = TextAnchor.MiddleCenter });
                }
#endif
            }

            // ルールボードのプレビュー
            Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.8f);
            foreach (var board in _placementData.RuleBoards)
            {
                Vector3 pos = gridMap != null ? gridMap.GridToWorld(board.GridPosition) : new Vector3(board.GridPosition.x + 0.5f, board.GridPosition.y + 0.5f, 0f);
                Gizmos.DrawCube(pos, new Vector3(0.6f, 0.8f, 0.1f));
                
#if UNITY_EDITOR
                UnityEditor.Handles.Label(pos + Vector3.up * 0.6f, "RULE", new GUIStyle { normal = { textColor = new Color(0.6f, 0.4f, 0.2f) }, fontSize = 10, alignment = TextAnchor.MiddleCenter });
#endif
            }
        }
    }
}
