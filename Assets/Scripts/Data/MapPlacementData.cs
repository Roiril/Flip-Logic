using System.Collections.Generic;
using UnityEngine;
using FlipLogic.Core;

namespace FlipLogic.Data
{
    [System.Serializable]
    public class EnemyPlacement
    {
        public EnemyData EnemyData;
        public Vector2Int GridPosition;
    }

    [System.Serializable]
    public class CellTagPlacement
    {
        public List<TagDefinition> Tags = new List<TagDefinition>();
        public Vector2Int GridPosition;
    }

    [System.Serializable]
    public class RuleBoardPlacement
    {
        public Vector2Int GridPosition;
    }

    /// <summary>
    /// ステージの初期配置データ（敵、セルギミック、ルールボード）を保持するScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "NewMapPlacementData", menuName = "FlipLogic/Map Placement Data")]
    public class MapPlacementData : ScriptableObject
    {
        public List<EnemyPlacement> Enemies = new List<EnemyPlacement>();
        public List<CellTagPlacement> CellTags = new List<CellTagPlacement>();
        public List<RuleBoardPlacement> RuleBoards = new List<RuleBoardPlacement>();
    }
}
