using UnityEngine;

namespace FlipLogic.Core
{
    /// <summary>
    /// エンティティ用スプライトを動的生成する静的ユーティリティ。
    /// 丸の中に文字を描いたスプライトを生成する。
    /// </summary>
    public static class EntitySpriteFactory
    {
        private const int TexSize = 64;
        private const float Radius = 28f;

        /// <summary>
        /// 丸＋中央文字のスプライトを生成する。
        /// </summary>
        public static Sprite CreateCircleWithLetter(char letter, Color circleColor, Color letterColor)
        {
            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            var pixels = new Color[TexSize * TexSize];

            float center = TexSize / 2f;

            // 背景を透明に
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // 円を描画（アンチエイリアス付き）
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    if (dist <= Radius)
                    {
                        // エッジのアンチエイリアス
                        float alpha = Mathf.Clamp01(Radius - dist + 1f);
                        pixels[y * TexSize + x] = new Color(circleColor.r, circleColor.g, circleColor.b, alpha);
                    }
                }
            }

            // 文字を描画（ビットマップフォント風）
            DrawLetter(pixels, letter, letterColor);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, TexSize, TexSize), new Vector2(0.5f, 0.5f), TexSize);
        }

        private static void DrawLetter(Color[] pixels, char letter, Color color)
        {
            // 簡易ビットマップ文字 (8x10 グリッド, 中央に配置)
            bool[,] bitmap = GetLetterBitmap(letter);
            if (bitmap == null) return;

            int bw = bitmap.GetLength(1);
            int bh = bitmap.GetLength(0);
            int scale = 3; // 拡大率
            int startX = (TexSize - bw * scale) / 2;
            int startY = (TexSize - bh * scale) / 2;

            for (int by = 0; by < bh; by++)
            {
                for (int bx = 0; bx < bw; bx++)
                {
                    if (!bitmap[by, bx]) continue;
                    for (int sy = 0; sy < scale; sy++)
                    {
                        for (int sx = 0; sx < scale; sx++)
                        {
                            int px = startX + bx * scale + sx;
                            int py = startY + (bh - 1 - by) * scale + sy;
                            if (px >= 0 && px < TexSize && py >= 0 && py < TexSize)
                                pixels[py * TexSize + px] = color;
                        }
                    }
                }
            }
        }

        private static bool[,] GetLetterBitmap(char c)
        {
            switch (c)
            {
                case 'P':
                    return new bool[,]
                    {
                        {true, true, true, true, true, false},
                        {true, false, false, false, false, true},
                        {true, false, false, false, false, true},
                        {true, true, true, true, true, false},
                        {true, false, false, false, false, false},
                        {true, false, false, false, false, false},
                        {true, false, false, false, false, false},
                    };
                case 'E':
                    return new bool[,]
                    {
                        {true, true, true, true, true, true},
                        {true, false, false, false, false, false},
                        {true, false, false, false, false, false},
                        {true, true, true, true, true, false},
                        {true, false, false, false, false, false},
                        {true, false, false, false, false, false},
                        {true, true, true, true, true, true},
                    };
                default:
                    return null;
            }
        }
    }
}
