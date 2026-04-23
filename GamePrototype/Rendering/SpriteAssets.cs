using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RunnerGame.Rendering
{
    internal sealed class SpriteAssets : IDisposable
    {
        public SpriteAssets()
        {
            string assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");

            Background = LoadBitmap(assetsPath, "background.png");
            PlayerNormal = RemoveConnectedBackground(LoadBitmap(assetsPath, "good1.png"), 92);
            PlayerHit = RemoveConnectedBackground(LoadBitmap(assetsPath, "bad1.png"), 112);
            PlayerBuff = RemoveConnectedBackground(LoadBitmap(assetsPath, "heal1.png"), 96);

            ObstacleLow = LoadBitmap(assetsPath, "DDOS.png");
            ObstacleHigh = LoadBitmap(assetsPath, "hacker.png");
            BonusSlow = LoadBitmap(assetsPath, "block-change.png");
            BonusProxy = LoadBitmap(assetsPath, "proxy.png");
            BonusVpn = LoadBitmap(assetsPath, "vpn.png");
            Rkn = LoadBitmap(assetsPath, "rkn.png");
        }

        public Bitmap Background { get; }
        public Bitmap PlayerNormal { get; }
        public Bitmap PlayerHit { get; }
        public Bitmap PlayerBuff { get; }
        public Bitmap ObstacleLow { get; }
        public Bitmap ObstacleHigh { get; }
        public Bitmap BonusSlow { get; }
        public Bitmap BonusProxy { get; }
        public Bitmap BonusVpn { get; }
        public Bitmap Rkn { get; }

        public void Dispose()
        {
            Background.Dispose();
            PlayerNormal.Dispose();
            PlayerHit.Dispose();
            PlayerBuff.Dispose();
            ObstacleLow.Dispose();
            ObstacleHigh.Dispose();
            BonusSlow.Dispose();
            BonusProxy.Dispose();
            BonusVpn.Dispose();
            Rkn.Dispose();
        }

        private static Bitmap LoadBitmap(string assetsPath, string fileName)
        {
            string fullPath = Path.Combine(assetsPath, fileName);
            using var source = new Bitmap(fullPath);
            return new Bitmap(source);
        }

        private static Bitmap RemoveConnectedBackground(Bitmap source, int threshold)
        {
            var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(result))
            {
                graphics.DrawImage(source, 0, 0, source.Width, source.Height);
            }

            var samples = new List<Color>
            {
                result.GetPixel(0, 0),
                result.GetPixel(result.Width - 1, 0),
                result.GetPixel(0, result.Height - 1),
                result.GetPixel(result.Width - 1, result.Height - 1)
            };

            var queue = new Queue<Point>();
            var visited = new bool[result.Width * result.Height];

            void Enqueue(int x, int y)
            {
                if (x < 0 || y < 0 || x >= result.Width || y >= result.Height)
                {
                    return;
                }

                int index = y * result.Width + x;
                if (visited[index])
                {
                    return;
                }

                visited[index] = true;
                queue.Enqueue(new Point(x, y));
            }

            for (int x = 0; x < result.Width; x++)
            {
                Enqueue(x, 0);
                Enqueue(x, result.Height - 1);
            }

            for (int y = 0; y < result.Height; y++)
            {
                Enqueue(0, y);
                Enqueue(result.Width - 1, y);
            }

            while (queue.Count > 0)
            {
                Point point = queue.Dequeue();
                Color color = result.GetPixel(point.X, point.Y);

                if (!MatchesBackground(color, samples, threshold))
                {
                    continue;
                }

                result.SetPixel(point.X, point.Y, Color.Transparent);

                Enqueue(point.X - 1, point.Y);
                Enqueue(point.X + 1, point.Y);
                Enqueue(point.X, point.Y - 1);
                Enqueue(point.X, point.Y + 1);
            }

            SoftFadeEdges(result);
            source.Dispose();
            return result;
        }

        private static bool MatchesBackground(Color color, List<Color> samples, int threshold)
        {
            if (color.A < 20)
            {
                return true;
            }

            foreach (var sample in samples)
            {
                int dr = color.R - sample.R;
                int dg = color.G - sample.G;
                int db = color.B - sample.B;
                int distance = dr * dr + dg * dg + db * db;

                if (distance <= threshold * threshold)
                {
                    return true;
                }
            }

            bool veryLight = color.R > 228 && color.G > 228 && color.B > 228;
            bool veryDark = color.R < 28 && color.G < 28 && color.B < 28;
            return veryLight || veryDark;
        }

        private static void SoftFadeEdges(Bitmap bitmap)
        {
            for (int y = 1; y < bitmap.Height - 1; y++)
            {
                for (int x = 1; x < bitmap.Width - 1; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    if (color.A == 0)
                    {
                        continue;
                    }

                    int transparentNeighbors = 0;
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0)
                            {
                                continue;
                            }

                            if (bitmap.GetPixel(x + offsetX, y + offsetY).A == 0)
                            {
                                transparentNeighbors++;
                            }
                        }
                    }

                    if (transparentNeighbors >= 3)
                    {
                        int alpha = Math.Min(color.A, 220 - transparentNeighbors * 18);
                        bitmap.SetPixel(x, y, Color.FromArgb(Math.Max(70, alpha), color));
                    }
                }
            }
        }
    }
}
