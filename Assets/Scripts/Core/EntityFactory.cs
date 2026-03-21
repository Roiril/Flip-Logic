using UnityEngine;
using FlipLogic.Data;
using FlipLogic.Explore;

namespace FlipLogic.Core
{
    /// <summary>
    /// EnemyData (ScriptableObject) を元に GameEntity を動的生成するファクトリ。
    /// </summary>
    public static class EntityFactory
    {
        /// <summary>
        /// EnemyData に基づいて敵エンティティを生成する。
        /// </summary>
        /// <param name="data">敵定義データ</param>
        /// <param name="gridPos">配置するグリッド座標</param>
        /// <param name="parent">親Transform（任意）</param>
        /// <returns>生成された GameEntity</returns>
        public static GameEntity CreateEnemy(EnemyData data, Vector2Int gridPos, Transform parent = null)
        {
            if (data == null)
            {
                Debug.LogError("[EntityFactory] EnemyData が null です。");
                return null;
            }

            // GameObject生成
            var go = new GameObject($"Enemy_{data.EnemyName}");
            if (parent != null)
                go.transform.SetParent(parent);

            // GameEntity初期化
            var entity = go.AddComponent<GameEntity>();
            entity.Initialize(
                entityName: data.EnemyName,
                type: EntityType.Enemy,
                gridPos: gridPos,
                maxHp: data.MaxHp,
                attack: data.Attack,
                defense: data.Defense
            );

            // SpriteRenderer + ビジュアル
            var sr = go.AddComponent<SpriteRenderer>();
            if (data.VisualDef != null)
            {
                if (data.VisualDef.UseDynamicSprite)
                {
                    sr.sprite = EntitySpriteFactory.CreateCircleWithLetter(
                        data.VisualDef.Letter,
                        data.VisualDef.CircleColor,
                        data.VisualDef.LetterColor
                    );
                }
                else
                {
                    sr.sprite = data.VisualDef.Sprite;
                }
                sr.color = data.VisualDef.TintColor;
                sr.sortingLayerName = data.VisualDef.SortingLayerName;
                sr.sortingOrder = data.VisualDef.OrderInLayer;
                go.transform.localScale = data.VisualDef.Scale;
            }
            else
            {
                // Fallback if no VisualDef assigned
                sr.sprite = EntitySpriteFactory.CreateCircleWithLetter(
                    'E',
                    new Color(0.5f, 0.9f, 1.0f),
                    Color.white
                );
                sr.sortingOrder = 5;
                go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            }

            // EnemySymbol (AI制御)
            var symbol = go.AddComponent<EnemySymbol>();
            symbol.AIType = data.AIType;

            // 初期タグの付与
            foreach (var tag in data.InitialTags)
            {
                entity.Tags.AddTag(tag);
            }

            // グリッド位置にワールド座標を同期
            if (Grid.GridMap.Instance != null)
            {
                go.transform.position = Grid.GridMap.Instance.GridToWorld(gridPos);
            }
            else
            {
                entity.SyncWorldPosition();
            }

            return entity;
        }
    }
}
