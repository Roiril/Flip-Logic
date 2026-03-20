using UnityEngine;

namespace FlipLogic.Core
{
    /// <summary>
    /// ゲーム内エンティティの統一基底。
    /// キャラクター、敵、オブジェクト、地形セル等すべてがこのコンポーネントを持つ。
    /// タグの集合体としてRule Evaluatorの評価対象となる。
    /// </summary>
    public class GameEntity : MonoBehaviour
    {
        [Header("Entity Info")]
        [SerializeField] private string _entityName;
        [SerializeField] private EntityType _entityType;

        [Header("Grid Position")]
        [SerializeField] private Vector2Int _gridPosition;

        [Header("Tags")]
        [SerializeField] private TagContainer _tags = new TagContainer();

        [Header("Stats")]
        [SerializeField] private int _hp;
        [SerializeField] private int _maxHp;
        [SerializeField] private int _attack;
        [SerializeField] private int _defense;

        /// <summary>エンティティ名。</summary>
        public string EntityName
        {
            get => _entityName;
            set => _entityName = value;
        }

        /// <summary>エンティティ種別。</summary>
        public EntityType Type => _entityType;

        /// <summary>グリッド上の座標。</summary>
        public Vector2Int GridPosition
        {
            get => _gridPosition;
            set => _gridPosition = value;
        }

        /// <summary>タグコンテナ。</summary>
        public TagContainer Tags => _tags;

        /// <summary>現在HP。</summary>
        public int Hp
        {
            get => _hp;
            set => _hp = Mathf.Clamp(value, 0, _maxHp);
        }

        /// <summary>最大HP。</summary>
        public int MaxHp
        {
            get => _maxHp;
            set => _maxHp = Mathf.Max(1, value);
        }

        /// <summary>攻撃力。</summary>
        public int Attack
        {
            get => _attack;
            set => _attack = value;
        }

        /// <summary>防御力。</summary>
        public int Defense
        {
            get => _defense;
            set => _defense = value;
        }

        /// <summary>生存しているか。</summary>
        public bool IsAlive => _hp > 0;

        /// <summary>
        /// エンティティを初期化する。
        /// </summary>
        public void Initialize(string entityName, EntityType type, Vector2Int gridPos, int maxHp = 0, int attack = 0, int defense = 0)
        {
            _entityName = entityName;
            _entityType = type;
            _gridPosition = gridPos;
            _maxHp = maxHp;
            _hp = maxHp;
            _attack = attack;
            _defense = defense;
        }

        /// <summary>
        /// ダメージを受ける。防御力による軽減あり。
        /// </summary>
        /// <returns>実際に受けたダメージ量。</returns>
        public int TakeDamage(int rawDamage)
        {
            int actual = Mathf.Max(1, rawDamage - _defense);
            _hp = Mathf.Max(0, _hp - actual);
            return actual;
        }

        /// <summary>
        /// HPを回復する。
        /// </summary>
        /// <returns>実際に回復した量。</returns>
        public int Heal(int amount)
        {
            int before = _hp;
            _hp = Mathf.Min(_maxHp, _hp + amount);
            return _hp - before;
        }

        /// <summary>
        /// ワールド座標をグリッド位置に同期する。
        /// </summary>
        public void SyncWorldPosition(float cellSize = 1f)
        {
            transform.position = new Vector3(
                _gridPosition.x * cellSize,
                _gridPosition.y * cellSize,
                0f
            );
        }
    }

    /// <summary>
    /// エンティティの種別。
    /// </summary>
    public enum EntityType
    {
        Player,     // プレイヤーキャラクター
        Enemy,      // 敵
        Npc,        // NPC
        Object,     // オブジェクト（扉、宝箱等）
        Terrain     // 地形セル
    }
}
