using System.Collections.Generic;
using UnityEngine;
using FlipLogic.Core;

namespace FlipLogic.Grid
{
    /// <summary>
    /// マスの属性タグを視覚的に表示するレンダラー。
    /// 属性に応じた斜め交差格子パターンのオーバーレイスプライトを表示する。
    /// </summary>
    public class TileOverlayRenderer : MonoBehaviour
    {
        public static TileOverlayRenderer Instance { get; private set; }

        private const int TexSize = 32;
        private readonly Dictionary<Vector2Int, GameObject> _overlays = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<string, Sprite> _patternCache = new Dictionary<string, Sprite>();

        [SerializeField] private Data.TileVisualDef _visualDef;
        private Transform _overlayParent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _overlayParent = new GameObject("TileOverlays").transform;
            _overlayParent.SetParent(transform);

#if UNITY_EDITOR
            if (_visualDef == null)
            {
                _visualDef = UnityEditor.AssetDatabase.LoadAssetAtPath<Data.TileVisualDef>("Assets/Data/Visuals/DefaultTileVisual.asset");
            }
#endif
        }

        /// <summary>指定セルのオーバーレイを更新する。</summary>
        public void UpdateOverlay(Vector2Int pos)
        {
            if (GridMap.Instance == null) return;

            var tags = GridMap.Instance.GetCellTags(pos);
            Data.TileVisualMapping mapping = null;
            
            if (_visualDef != null)
            {
                mapping = _visualDef.GetMappingForTags(tags);
            }
            else
            {
                // Fallback for when VisualDef is not assigned yet
                mapping = GetFallbackMapping(tags);
            }

            if (mapping != null)
            {
                SetOverlay(pos, mapping);
            }
            else
            {
                RemoveOverlay(pos);
            }
        }

        /// <summary>全セルのオーバーレイを再構築する。</summary>
        public void RebuildAll()
        {
            ClearAll();
            if (GridMap.Instance == null) return;

            var mapSize = GridMap.Instance.MapSize;
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    UpdateOverlay(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>指定セルにオーバーレイを設置/更新する。</summary>
        private void SetOverlay(Vector2Int pos, Data.TileVisualMapping mapping)
        {
            if (_overlays.TryGetValue(pos, out var existing))
            {
                var existingSr = existing.GetComponent<SpriteRenderer>();
                ApplyMappingToRenderer(existingSr, mapping);
                return;
            }

            var go = new GameObject($"Overlay_{pos.x}_{pos.y}");
            go.transform.SetParent(_overlayParent);
            go.transform.position = GridMap.Instance.GridToWorld(pos);
            go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            ApplyMappingToRenderer(renderer, mapping);
            renderer.sortingOrder = -5;

            _overlays[pos] = go;
        }

        private void ApplyMappingToRenderer(SpriteRenderer renderer, Data.TileVisualMapping mapping)
        {
            if (mapping.UseCustomSprite && mapping.CustomSprite != null)
            {
                renderer.sprite = mapping.CustomSprite;
                renderer.color = mapping.OverlayColor;
            }
            else
            {
                renderer.sprite = GetOrCreatePattern(mapping.OverlayColor);
                renderer.color = Color.white; // パターン自体に色が入っている
            }
        }

        private void RemoveOverlay(Vector2Int pos)
        {
            if (_overlays.TryGetValue(pos, out var go))
            {
                Destroy(go);
                _overlays.Remove(pos);
            }
        }

        private void ClearAll()
        {
            foreach (var kvp in _overlays)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            _overlays.Clear();
        }

        /// <summary>未設定時のフォールバック。</summary>
        private Data.TileVisualMapping GetFallbackMapping(TagContainer tags)
        {
            if (tags.HasTag("Element", "Fire"))
                return new Data.TileVisualMapping { OverlayColor = new Color(1f, 0.55f, 0.1f, 0.6f) };
            if (tags.HasTag("Element", "Poison"))
                return new Data.TileVisualMapping { OverlayColor = new Color(0.6f, 0.15f, 0.8f, 0.6f) };
            if (tags.HasTag("Element", "Ice"))
                return new Data.TileVisualMapping { OverlayColor = new Color(0.4f, 0.8f, 1.0f, 0.6f) };
            return null;
        }

        /// <summary>斜め交差格子パターンのスプライトを生成する。</summary>
        private Sprite GetOrCreatePattern(Color color)
        {
            string key = $"{color.r:F2}_{color.g:F2}_{color.b:F2}";
            if (_patternCache.TryGetValue(key, out var cached))
                return cached;

            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            var pixels = new Color[TexSize * TexSize];

            // 背景を半透明の暗い色に
            Color bg = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.25f);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = bg;

            // 斜め交差格子を描画
            int spacing = 5;
            int lineWidth = 1;
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    // 右上がり斜線
                    bool diag1 = ((x + y) % spacing) < lineWidth;
                    // 右下がり斜線
                    bool diag2 = ((x - y + TexSize * spacing) % spacing) < lineWidth;

                    if (diag1 || diag2)
                    {
                        pixels[y * TexSize + x] = color;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            var sprite = Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), TexSize);
            _patternCache[key] = sprite;
            return sprite;
        }
    }
}
