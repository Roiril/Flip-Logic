using System.Collections.Generic;
using UnityEngine;

namespace FlipLogic.Core
{
    /// <summary>
    /// エンティティの全体管理システム。
    /// FindObjectsByType のような重い検索を排除し、
    /// O(1)でのカテゴリ別・ID別のエンティティアクセスを提供する。
    /// </summary>
    public class EntityRegistry
    {
        // Singleton （必要に応じて ServiceLocator 等に差し替え可能）
        private static EntityRegistry _instance;
        public static EntityRegistry Instance => _instance ??= new EntityRegistry();

        // 種類ごとのエンティティリスト
        private readonly Dictionary<EntityType, List<GameEntity>> _entitiesByType = new Dictionary<EntityType, List<GameEntity>>();
        
        // 全エンティティの高速アクセスマップ（InstanceIDキー）
        private readonly Dictionary<int, GameEntity> _entityById = new Dictionary<int, GameEntity>();

        private EntityRegistry()
        {
            // 全ての EntityType のリストをあらかじめ作成
            foreach (EntityType type in System.Enum.GetValues(typeof(EntityType)))
            {
                _entitiesByType[type] = new List<GameEntity>();
            }
        }

        /// <summary>
        /// エンティティを登録する。通常は GameEntity の OnEnable から呼ばれる。
        /// </summary>
        public void Register(GameEntity entity)
        {
            if (entity == null) return;

            int id = entity.gameObject.GetInstanceID();
            if (!_entityById.ContainsKey(id))
            {
                _entityById[id] = entity;
                _entitiesByType[entity.Type].Add(entity);
            }
        }

        /// <summary>
        /// エンティティを登録解除する。通常は GameEntity の OnDisable から呼ばれる。
        /// </summary>
        public void Unregister(GameEntity entity)
        {
            if (entity == null) return;

            int id = entity.gameObject.GetInstanceID();
            if (_entityById.ContainsKey(id))
            {
                _entityById.Remove(id);
                _entitiesByType[entity.Type].Remove(entity);
            }
        }

        /// <summary>
        /// 指定した種別の全エンティティリストを取得する (Allocationなし)。
        /// ※戻り値のリストは直接操作しないこと。
        /// </summary>
        public IReadOnlyList<GameEntity> GetEntities(EntityType type)
        {
            return _entitiesByType[type];
        }

        /// <summary>
        /// 指定した InstanceID のエンティティを取得する。存在しない場合は null を返す。
        /// </summary>
        public GameEntity GetEntityById(int instanceId)
        {
            if (_entityById.TryGetValue(instanceId, out var entity))
            {
                return entity;
            }
            return null;
        }

        /// <summary>
        /// 全エンティティのコレクションを取得する。
        /// </summary>
        public IEnumerable<GameEntity> GetAllEntities()
        {
            return _entityById.Values;
        }

        /// <summary>
        /// ゲーム終了時やシーン遷移時のクリーンアップ。
        /// </summary>
        public void Clear()
        {
            _entityById.Clear();
            foreach (var list in _entitiesByType.Values)
            {
                list.Clear();
            }
        }
    }
}
