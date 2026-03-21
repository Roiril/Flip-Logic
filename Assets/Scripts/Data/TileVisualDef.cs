using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlipLogic.Data
{
    [Serializable]
    public class TileVisualMapping
    {
        public string TagKey; // e.g. "Element"
        public string TagValue; // e.g. "Fire"
        
        [Header("Visual Settings")]
        public Color OverlayColor;
        [Tooltip("If true, uses CustomSprite instead of generating a generic procedural pattern.")]
        public bool UseCustomSprite = false;
        public Sprite CustomSprite;

        public bool Matches(Core.TagContainer tags)
        {
            return tags.HasTag(TagKey, TagValue);
        }
    }

    /// <summary>
    /// タイルのタグ属性に応じたオーバーレイ表示データを管理する設定ファイル。
    /// </summary>
    [CreateAssetMenu(fileName = "TileVisualDef", menuName = "FlipLogic/Visual/Tile Visual Def")]
    public class TileVisualDef : ScriptableObject
    {
        public List<TileVisualMapping> Mappings = new List<TileVisualMapping>();

        public TileVisualMapping GetMappingForTags(Core.TagContainer tags)
        {
            foreach (var mapping in Mappings)
            {
                if (mapping.Matches(tags)) return mapping;
            }
            return null;
        }
    }
}
