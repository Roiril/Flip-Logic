using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FlipLogic.Grid
{
    /// <summary>
    /// グリッドマップ管理。Tilemapを使用したグリッドベースのマップを管理し、
    /// 各セルのタグ情報と通行判定を提供する。
    /// </summary>
    public class GridMap : MonoBehaviour
    {
        public static GridMap Instance { get; private set; }

        [Header("Tilemap References")]
        [SerializeField] private Tilemap _groundTilemap;
        [SerializeField] private Tilemap _wallTilemap;
        [SerializeField] private Tilemap _overlayTilemap;

        [Header("Map Settings")]
        [SerializeField] private Vector2Int _mapSize = new Vector2Int(20, 15);

        /// <summary>セルごとのタグ情報。地形属性をタグで表現。</summary>
        private readonly Dictionary<Vector2Int, Core.TagContainer> _cellTags = new Dictionary<Vector2Int, Core.TagContainer>();

        /// <summary>セル上のエンティティを追跡。</summary>
        private readonly Dictionary<Vector2Int, List<Core.GameEntity>> _entityMap = new Dictionary<Vector2Int, List<Core.GameEntity>>();

        /// <summary>マップサイズ。</summary>
        public Vector2Int MapSize => _mapSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>指定セルが通行可能か。</summary>
        public bool IsWalkable(Vector2Int pos)
        {
            if (!IsInBounds(pos)) return false;

            // 壁Tilemapにタイルがある場合は通行不可
            if (_wallTilemap != null)
            {
                var tilePos = new Vector3Int(pos.x, pos.y, 0);
                if (_wallTilemap.HasTile(tilePos)) return false;
            }

            // セルタグで [Terrain:Wall] を持つ場合も通行不可
            var tags = GetCellTags(pos);
            if (tags.HasTag("Terrain", "Wall")) return false;

            return true;
        }

        /// <summary>指定セルにエンティティがいるか。</summary>
        public bool HasEntity(Vector2Int pos)
        {
            return _entityMap.ContainsKey(pos) && _entityMap[pos].Count > 0;
        }

        /// <summary>指定セルのエンティティ一覧を取得。</summary>
        public List<Core.GameEntity> GetEntitiesAt(Vector2Int pos)
        {
            if (_entityMap.TryGetValue(pos, out var list))
                return new List<Core.GameEntity>(list);
            return new List<Core.GameEntity>();
        }

        /// <summary>セルのタグコンテナを取得（なければ作成）。</summary>
        public Core.TagContainer GetCellTags(Vector2Int pos)
        {
            if (!_cellTags.ContainsKey(pos))
                _cellTags[pos] = new Core.TagContainer();
            return _cellTags[pos];
        }

        /// <summary>指定セルにタグを付与する。</summary>
        public void AddCellTag(Vector2Int pos, Core.TagDefinition tag)
        {
            GetCellTags(pos).AddTag(tag);
        }

        /// <summary>エンティティの位置を登録/更新する。</summary>
        public void RegisterEntity(Core.GameEntity entity, Vector2Int pos)
        {
            // 旧位置から除去
            UnregisterEntity(entity);

            // 新位置に登録
            if (!_entityMap.ContainsKey(pos))
                _entityMap[pos] = new List<Core.GameEntity>();
            _entityMap[pos].Add(entity);
        }

        /// <summary>エンティティを位置マップから除去する。</summary>
        public void UnregisterEntity(Core.GameEntity entity)
        {
            foreach (var kvp in _entityMap)
            {
                kvp.Value.Remove(entity);
            }
        }

        /// <summary>座標が範囲内か。</summary>
        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < _mapSize.x && pos.y >= 0 && pos.y < _mapSize.y;
        }

        /// <summary>ワールド座標をグリッド座標に変換する。</summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            if (_groundTilemap != null)
            {
                var cellPos = _groundTilemap.WorldToCell(worldPos);
                return new Vector2Int(cellPos.x, cellPos.y);
            }
            return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
        }

        /// <summary>グリッド座標をワールド座標に変換する。</summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            if (_groundTilemap != null)
            {
                return _groundTilemap.CellToWorld(new Vector3Int(gridPos.x, gridPos.y, 0))
                       + _groundTilemap.cellSize * 0.5f;
            }
            return new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f);
        }

        /// <summary>全セルのタグ期限処理を実行する。</summary>
        public void TickAllCellTags()
        {
            foreach (var kvp in _cellTags)
            {
                var expired = kvp.Value.TickDurations();
                foreach (var tag in expired)
                {
                    Debug.Log($"[GridMap] セル{kvp.Key}のタグ {tag} が期限切れ");
                }
            }
        }
    }
}
