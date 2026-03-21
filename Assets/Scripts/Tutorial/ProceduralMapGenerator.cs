using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Grid;

namespace FlipLogic.Tutorial
{
    /// <summary>
    /// チュートリアルデモ用のプロシージャルマップ生成。
    /// SpriteRendererで色付きタイルを敷き詰めてグリッドを視覚化する。
    /// </summary>
    public class ProceduralMapGenerator : MonoBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private int _width = 10;
        [SerializeField] private int _height = 8;

        [Header("Colors")]
        [SerializeField] private Color _floorColor = new Color(0.22f, 0.35f, 0.22f);
        [SerializeField] private Color _wallColor = new Color(0.15f, 0.12f, 0.1f);

        private Transform _tilesParent;
        private static Sprite _whiteSprite;

        private void Awake()
        {
            _tilesParent = new GameObject("GeneratedTiles").transform;
            _tilesParent.SetParent(transform);
        }

        private void Start()
        {
            GenerateMap();
        }

        public void GenerateMap()
        {
            foreach (Transform child in _tilesParent)
                Destroy(child.gameObject);

            EnsureWhiteSprite();

            int[,] map = CreateLayout();

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    bool isWall = map[x, y] == 1;

                    var go = new GameObject($"Tile_{x}_{y}");
                    go.transform.SetParent(_tilesParent);
                    go.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = _whiteSprite;
                    sr.color = isWall ? _wallColor : _floorColor;
                    sr.sortingOrder = -10;

                    if (isWall && GridMap.Instance != null)
                    {
                        GridMap.Instance.AddCellTag(new Vector2Int(x, y),
                            new TagDefinition("Terrain", "Wall", -1, "Map"));
                    }
                }
            }

            // GridMapの境界を実際のマップサイズに同期
            if (GridMap.Instance != null)
            {
                GridMap.Instance.SetMapSize(new Vector2Int(_width, _height));
            }

        }

        private int[,] CreateLayout()
        {
            int[,] map = new int[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (x == 0 || x == _width - 1 || y == 0 || y == _height - 1)
                        map[x, y] = 1;
                    else
                        map[x, y] = 0;
                }
            }
            map[4, 3] = 1;
            map[4, 4] = 1;
            map[5, 3] = 1;
            return map;
        }

        private static void EnsureWhiteSprite()
        {
            if (_whiteSprite != null) return;
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
