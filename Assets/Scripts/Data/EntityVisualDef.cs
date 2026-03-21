using UnityEngine;

namespace FlipLogic.Data
{
    /// <summary>
    /// エンティティの標準的な視覚表現を定義する ScriptableObject。
    /// ハードコードされた Sprite 生成を代替する。
    /// </summary>
    [CreateAssetMenu(fileName = "NewEntityVisual", menuName = "FlipLogic/Visual/Entity Visual Def")]
    public class EntityVisualDef : ScriptableObject
    {
        [Header("Sprite Settings")]
        public Sprite Sprite;
        
        [Header("Dynamic Prototype Settings")]
        [Tooltip("If true, ignores the Sprite field and dynamically generates a circle sprite with a letter.")]
        public bool UseDynamicSprite = false;
        public char Letter = '?';
        public Color CircleColor = Color.white;
        public Color LetterColor = Color.black;

        [Header("Transform & Sorting")]
        public Color TintColor = Color.white;
        public string SortingLayerName = "Default";
        public int OrderInLayer = 0;
        public Vector3 Scale = Vector3.one;
    }
}
