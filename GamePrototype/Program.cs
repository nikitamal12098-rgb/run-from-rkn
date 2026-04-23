using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace RunnerGame
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new GameForm());
        }
    }

    public class GameForm : Form
    {
        private readonly Timer gameTimer;
        private readonly Random random = new();
        private readonly List<Obstacle> obstacles = new();
        private readonly List<BonusItem> bonuses = new();

        private readonly Font hudFont = new("Arial", 12, FontStyle.Bold);
        private readonly Font titleFont = new("Arial", 18, FontStyle.Bold);
        private readonly Brush skyBrush = new SolidBrush(Color.FromArgb(95, 165, 255));
        private readonly Brush roadBrush = new SolidBrush(Color.FromArgb(50, 50, 58));
        private readonly Brush roadSideBrush = new SolidBrush(Color.FromArgb(38, 30, 25));
        private readonly Brush laneMarkerBrush = new SolidBrush(Color.FromArgb(235, 224, 150));
        private readonly Brush fogBrush = new SolidBrush(Color.FromArgb(100, 220, 235, 255));
        private readonly Pen highObstaclePen = new(Color.FromArgb(40, 15, 15), 2f);
        private readonly Pen lowObstaclePen = new(Color.FromArgb(35, 35, 10), 2f);
        private readonly Pen bonusPen = new(Color.FromArgb(20, 70, 20), 2f);
        private readonly Brush playerShadowBrush = new SolidBrush(Color.FromArgb(90, 20, 20, 20));
        private readonly Brush rknBrush = new SolidBrush(Color.FromArgb(180, 155, 35, 35));
        private readonly Pen rknPen = new(Color.FromArgb(215, 255, 210, 210), 2f);

        private const int LaneCount = 3;
        private const float MaxDepth = 100f;
        private const float MinSpawnDepth = 30f;
        private const float JumpStrength = 1.08f;
        private const float Gravity = 0.085f;
        private const float PlayerGroundOffset = 88f;
        private const float HorizonRatio = 0.34f;
        private const float RoadTopWidthRatio = 0.18f;
        private const float RoadBottomWidthRatio = 0.92f;

        private int gameTick;
        private int score;
        private int baseSpeed = 5;
        private int currentSpeed = 5;
        private int spawnChance = 5;
        private int bonusCooldown;
        private int hitFlashTimer;
        private int slowTimer;
        private int invincibleTimer;
        private int dangerLevel;

        private readonly int maxDanger = 100;
        private readonly int smallHit = 25;
        private readonly int bigHit = 50;

        private bool isInvincible;
        private int playerLane = 1;
        private float playerJumpHeight;
        private float playerVerticalVelocity;
        private bool jumpPressed;

        private float rknDepth = MaxDepth - 4f;

        public GameForm()
        {
            Text = "Run From RKN";
            ClientSize = new Size(1100, 720);
            DoubleBuffered = true;
            KeyPreview = true;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 620);

            BackColor = Color.Black;

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            gameTimer = new Timer
            {
                Interval = 16
            };
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawWorld(g);
            DrawObstacles(g);
            DrawBonuses(g);
            DrawPlayer(g);
            DrawRkn(g);
            DrawHud(g);
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            gameTick++;

            UpdatePlayerJump();
            MoveObstacles();
            MoveBonuses();
            SpawnObstacle();
            SpawnBonus();
            CheckCollisions();

            UpdateScore();
            IncreaseDifficulty();
            UpdateEffects();
            UpdateDanger();
            UpdateRkn();

            if (bonusCooldown > 0)
            {
                bonusCooldown--;
            }

            Invalidate();
        }

        private void UpdatePlayerJump()
        {
            if (jumpPressed && playerJumpHeight <= 0.001f)
            {
                playerVerticalVelocity = JumpStrength;
                jumpPressed = false;
            }

            if (playerJumpHeight > 0f || playerVerticalVelocity > 0f)
            {
                playerJumpHeight += playerVerticalVelocity;
                playerVerticalVelocity -= Gravity;

                if (playerJumpHeight < 0f)
                {
                    playerJumpHeight = 0f;
                    playerVerticalVelocity = 0f;
                }
            }
        }

        private void MoveObstacles()
        {
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                obstacles[i].Depth -= currentSpeed * 0.42f;

                if (obstacles[i].Depth <= 0.8f)
                {
                    obstacles.RemoveAt(i);
                }
            }
        }

        private void MoveBonuses()
        {
            for (int i = bonuses.Count - 1; i >= 0; i--)
            {
                bonuses[i].Depth -= currentSpeed * 0.42f;

                if (bonuses[i].Depth <= 0.8f)
                {
                    bonuses.RemoveAt(i);
                }
            }
        }

        private void SpawnObstacle()
        {
            if (random.Next(0, 100) >= spawnChance)
            {
                return;
            }

            foreach (var obstacle in obstacles)
            {
                if (obstacle.Depth > MaxDepth - 18f)
                {
                    return;
                }
            }

            var lanes = new List<int> { 0, 1, 2 };
            var maxObstacles = gameTick > 1000 ? 2 : 1;
            var obstacleCount = random.Next(1, maxObstacles + 1);

            for (int i = 0; i < obstacleCount && lanes.Count > 0; i++)
            {
                int index = random.Next(lanes.Count);
                int lane = lanes[index];
                lanes.RemoveAt(index);

                bool isHigh = random.Next(100) < 35;
                obstacles.Add(new Obstacle
                {
                    Lane = lane,
                    Depth = MaxDepth,
                    Height = isHigh ? 2.25f : 0.95f,
                    Width = isHigh ? 0.78f : 0.92f,
                    Type = isHigh ? ObstacleType.High : ObstacleType.Low
                });
            }
        }

        private void SpawnBonus()
        {
            if (bonusCooldown > 0 || random.Next(0, 100) >= 2)
            {
                return;
            }

            foreach (var bonus in bonuses)
            {
                if (bonus.Depth > MaxDepth - 20f)
                {
                    return;
                }
            }

            bonuses.Add(new BonusItem
            {
                Lane = random.Next(0, LaneCount),
                Depth = MaxDepth,
                Kind = (BonusType)random.Next(0, 3)
            });

            bonusCooldown = 120;
        }

        private void CheckCollisions()
        {
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = obstacles[i];

                if (obstacle.Lane != playerLane)
                {
                    continue;
                }

                if (obstacle.Depth > 9.5f || obstacle.Depth < 2.5f)
                {
                    continue;
                }

                bool clearsLowObstacle = obstacle.Type == ObstacleType.Low && playerJumpHeight > 0.72f;
                if (clearsLowObstacle)
                {
                    continue;
                }

                if (!isInvincible)
                {
                    hitFlashTimer = 20;

                    dangerLevel += obstacle.Type == ObstacleType.High ? bigHit : smallHit;
                    if (dangerLevel > maxDanger)
                    {
                        dangerLevel = maxDanger;
                    }
                }

                obstacles.RemoveAt(i);
            }

            for (int i = bonuses.Count - 1; i >= 0; i--)
            {
                var bonus = bonuses[i];

                if (bonus.Lane != playerLane || bonus.Depth > 9.5f || bonus.Depth < 2.5f)
                {
                    continue;
                }

                switch (bonus.Kind)
                {
                    case BonusType.Vpn:
                        isInvincible = true;
                        invincibleTimer = 120;
                        break;
                    case BonusType.ProxyHeal:
                        dangerLevel -= 25;
                        if (dangerLevel < 0)
                        {
                            dangerLevel = 0;
                        }
                        break;
                    case BonusType.Slow:
                        slowTimer = 120;
                        break;
                }

                bonuses.RemoveAt(i);
            }
        }

        private void UpdateEffects()
        {
            currentSpeed = baseSpeed;

            if (hitFlashTimer > 0)
            {
                hitFlashTimer--;
            }

            if (isInvincible)
            {
                invincibleTimer--;
                if (invincibleTimer <= 0)
                {
                    isInvincible = false;
                }
            }

            if (slowTimer > 0)
            {
                slowTimer--;
                currentSpeed = Math.Max(2, baseSpeed / 2);
            }
        }

        private void UpdateScore()
        {
            score++;
        }

        private void IncreaseDifficulty()
        {
            if (gameTick % 120 == 0)
            {
                if (baseSpeed < 15)
                {
                    baseSpeed++;
                }

                if (spawnChance < 20)
                {
                    spawnChance++;
                }
            }
        }

        private void UpdateDanger()
        {
            if (dangerLevel >= maxDanger)
            {
                GameOver();
            }
        }

        private void UpdateRkn()
        {
            float targetDepth = Math.Max(8f, MaxDepth - dangerLevel * 0.72f);
            rknDepth += (targetDepth - rknDepth) * 0.08f;
        }

        private void GameOver()
        {
            gameTimer.Stop();
            MessageBox.Show(
                $"Тебя поймали РКН 😈{Environment.NewLine}Счёт: {score}",
                "Game Over",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void DrawWorld(Graphics g)
        {
            float width = ClientSize.Width;
            float height = ClientSize.Height;
            float horizonY = height * HorizonRatio;

            g.FillRectangle(skyBrush, 0, 0, width, horizonY);

            using var cityBrush = new SolidBrush(Color.FromArgb(70, 80, 105));
            var skyline = new[]
            {
                new RectangleF(width * 0.04f, horizonY - 70f, 60f, 70f),
                new RectangleF(width * 0.16f, horizonY - 110f, 80f, 110f),
                new RectangleF(width * 0.29f, horizonY - 85f, 50f, 85f),
                new RectangleF(width * 0.63f, horizonY - 95f, 75f, 95f),
                new RectangleF(width * 0.78f, horizonY - 130f, 90f, 130f),
                new RectangleF(width * 0.9f, horizonY - 72f, 40f, 72f)
            };

            foreach (var rect in skyline)
            {
                g.FillRectangle(cityBrush, rect);
            }

            PointF leftTop = GetRoadPoint(-1.38f, MinSpawnDepth);
            PointF rightTop = GetRoadPoint(1.38f, MinSpawnDepth);
            PointF leftBottom = GetRoadPoint(-1.6f, 0.5f);
            PointF rightBottom = GetRoadPoint(1.6f, 0.5f);

            g.FillPolygon(roadSideBrush, new[] { leftTop, leftBottom, new PointF(0, height), new PointF(0, horizonY) });
            g.FillPolygon(roadSideBrush, new[] { rightTop, rightBottom, new PointF(width, height), new PointF(width, horizonY) });
            g.FillPolygon(roadBrush, new[] { leftTop, rightTop, rightBottom, leftBottom });

            DrawLaneMarkers(g);

            using var fogGradient = new LinearGradientBrush(
                new PointF(0, horizonY),
                new PointF(0, height),
                Color.FromArgb(15, 255, 255, 255),
                Color.FromArgb(120, 255, 255, 255));
            g.FillRectangle(fogGradient, 0, horizonY, width, height - horizonY);
        }

        private void DrawLaneMarkers(Graphics g)
        {
            for (int laneSeparator = 1; laneSeparator < LaneCount; laneSeparator++)
            {
                float laneRatio = (laneSeparator / (float)LaneCount) * 2f - 1f;

                for (float depth = 6f; depth < MaxDepth; depth += 10f)
                {
                    PointF p1 = GetRoadPoint(laneRatio, depth);
                    PointF p2 = GetRoadPoint(laneRatio, depth + 4.5f);

                    float thickness = Math.Max(1f, GetScale(depth) * 12f);
                    using var markerPen = new Pen(Color.FromArgb(180, 245, 235, 160), thickness)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };
                    g.DrawLine(markerPen, p1, p2);
                }
            }
        }

        private void DrawObstacles(Graphics g)
        {
            obstacles.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            foreach (var obstacle in obstacles)
            {
                DrawObstacle(g, obstacle);
            }
        }

        private void DrawObstacle(Graphics g, Obstacle obstacle)
        {
            float scale = GetScale(obstacle.Depth);
            float width = 80f * obstacle.Width * scale;
            float height = 135f * obstacle.Height * scale;

            PointF basePoint = GetRoadPoint(GetLaneOffset(obstacle.Lane), obstacle.Depth);
            RectangleF rect = new(basePoint.X - width / 2f, basePoint.Y - height, width, height);

            if (obstacle.Type == ObstacleType.High)
            {
                using var bodyBrush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(85, 20, 20),
                    Color.FromArgb(225, 75, 75),
                    LinearGradientMode.Vertical);
                g.FillRectangle(bodyBrush, rect);
                g.DrawRectangle(highObstaclePen, rect.X, rect.Y, rect.Width, rect.Height);

                var cap = new RectangleF(rect.X + rect.Width * 0.18f, rect.Y + rect.Height * 0.12f, rect.Width * 0.64f, rect.Height * 0.22f);
                using var capBrush = new SolidBrush(Color.FromArgb(255, 215, 120));
                g.FillRectangle(capBrush, cap);
            }
            else
            {
                using var bodyBrush = new LinearGradientBrush(
                    rect,
                    Color.FromArgb(130, 105, 25),
                    Color.FromArgb(230, 195, 70),
                    LinearGradientMode.Vertical);
                g.FillRectangle(bodyBrush, rect);
                g.DrawRectangle(lowObstaclePen, rect.X, rect.Y, rect.Width, rect.Height);

                using var stripeBrush = new SolidBrush(Color.FromArgb(225, 100, 35));
                float stripeHeight = Math.Max(4f, rect.Height * 0.18f);
                g.FillRectangle(stripeBrush, rect.X, rect.Y + rect.Height - stripeHeight, rect.Width, stripeHeight);
            }

            float shadowWidth = width * 1.15f;
            float shadowHeight = Math.Max(6f, 18f * scale);
            g.FillEllipse(
                playerShadowBrush,
                basePoint.X - shadowWidth / 2f,
                basePoint.Y - shadowHeight / 2f,
                shadowWidth,
                shadowHeight);
        }

        private void DrawBonuses(Graphics g)
        {
            bonuses.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            foreach (var bonus in bonuses)
            {
                float scale = GetScale(bonus.Depth);
                float size = 58f * scale;
                PointF center = GetRoadPoint(GetLaneOffset(bonus.Lane), bonus.Depth);
                RectangleF rect = new(center.X - size / 2f, center.Y - size - 12f * scale, size, size);

                Color fillColor = bonus.Kind switch
                {
                    BonusType.Vpn => Color.FromArgb(65, 220, 180),
                    BonusType.ProxyHeal => Color.FromArgb(60, 170, 255),
                    _ => Color.FromArgb(170, 120, 255)
                };

                using var bonusBrush = new SolidBrush(fillColor);
                g.FillEllipse(bonusBrush, rect);
                g.DrawEllipse(bonusPen, rect);

                string label = bonus.Kind switch
                {
                    BonusType.Vpn => "VPN",
                    BonusType.ProxyHeal => "+25",
                    _ => "SLOW"
                };

                using var bonusLabelFont = new Font("Arial", Math.Max(8f, 11f * scale), FontStyle.Bold);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(label, bonusLabelFont, Brushes.White, rect, sf);
            }
        }

        private void DrawPlayer(Graphics g)
        {
            float screenCenterX = ClientSize.Width / 2f;
            float baseY = ClientSize.Height - PlayerGroundOffset;
            float laneShift = (playerLane - 1) * 92f;
            float jumpOffset = playerJumpHeight * 120f;
            float bodyBottom = baseY - jumpOffset;

            float bodyWidth = 72f;
            float bodyHeight = 118f;
            RectangleF bodyRect = new(screenCenterX - bodyWidth / 2f + laneShift, bodyBottom - bodyHeight, bodyWidth, bodyHeight);

            float shadowWidth = 85f;
            float shadowHeight = 18f;
            g.FillEllipse(
                playerShadowBrush,
                bodyRect.X + bodyWidth / 2f - shadowWidth / 2f,
                baseY - shadowHeight / 2f,
                shadowWidth,
                shadowHeight);

            Color suitColor = isInvincible
                ? Color.FromArgb(95, 240, 195)
                : hitFlashTimer > 0 && hitFlashTimer % 4 < 2
                    ? Color.FromArgb(255, 105, 105)
                    : Color.FromArgb(80, 225, 120);

            using var suitBrush = new SolidBrush(suitColor);
            using var darkBrush = new SolidBrush(Color.FromArgb(30, 30, 40));
            using var faceBrush = new SolidBrush(Color.FromArgb(255, 225, 190));
            using var linePen = new Pen(Color.FromArgb(35, 35, 35), 3f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            g.FillRoundedRectangle(suitBrush, bodyRect, 14f);
            RectangleF visorRect = new(bodyRect.X + 10f, bodyRect.Y + 12f, bodyRect.Width - 20f, 26f);
            g.FillRoundedRectangle(darkBrush, visorRect, 10f);

            float headSize = 42f;
            RectangleF headRect = new(bodyRect.X + bodyRect.Width / 2f - headSize / 2f, bodyRect.Y - 24f, headSize, headSize);
            g.FillEllipse(faceBrush, headRect);
            g.DrawEllipse(Pens.Black, headRect);

            float legY = bodyRect.Bottom;
            float footOffset = playerJumpHeight > 0.12f ? 10f : 0f;
            g.DrawLine(linePen, bodyRect.X + 20f, legY - 2f, bodyRect.X + 16f, legY + 26f - footOffset);
            g.DrawLine(linePen, bodyRect.Right - 20f, legY - 2f, bodyRect.Right - 16f, legY + 26f + footOffset);
            g.DrawLine(linePen, bodyRect.X + 14f, bodyRect.Y + 50f, bodyRect.X - 10f, bodyRect.Y + 70f - footOffset);
            g.DrawLine(linePen, bodyRect.Right - 14f, bodyRect.Y + 50f, bodyRect.Right + 10f, bodyRect.Y + 70f + footOffset);
        }

        private void DrawRkn(Graphics g)
        {
            PointF center = GetRoadPoint(0f, rknDepth);
            float scale = Math.Max(0.2f, GetScale(rknDepth));
            float width = 170f * scale;
            float height = 88f * scale;

            RectangleF rect = new(center.X - width / 2f, center.Y - height - 6f, width, height);
            g.FillRoundedRectangle(rknBrush, rect, 18f * scale);
            g.DrawRoundedRectangle(rknPen, rect, 18f * scale);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var font = new Font("Arial", Math.Max(8f, 18f * scale), FontStyle.Bold);
            g.DrawString("RKN", font, Brushes.White, rect, sf);
        }

        private void DrawHud(Graphics g)
        {
            RectangleF hudRect = new(18f, 16f, 280f, 122f);
            using var hudBrush = new SolidBrush(Color.FromArgb(140, 10, 16, 28));
            using var hudBorder = new Pen(Color.FromArgb(130, 255, 255, 255), 1.5f);

            g.FillRoundedRectangle(hudBrush, hudRect, 18f);
            g.DrawRoundedRectangle(hudBorder, hudRect, 18f);

            Color dangerColor = dangerLevel switch
            {
                > 70 => Color.FromArgb(255, 85, 85),
                > 30 => Color.FromArgb(255, 190, 70),
                _ => Color.FromArgb(80, 225, 140)
            };

            g.DrawString($"Score: {score}", titleFont, Brushes.White, 32f, 28f);
            using var dangerBrush = new SolidBrush(dangerColor);
            g.DrawString($"RKN: {dangerLevel}%", hudFont, dangerBrush, 32f, 65f);
            g.DrawString($"Скорость: {currentSpeed}", hudFont, Brushes.WhiteSmoke, 32f, 91f);

            string state = isInvincible
                ? $"VPN {invincibleTimer / 60f:0.0}s"
                : slowTimer > 0
                    ? $"Замедление {slowTimer / 60f:0.0}s"
                    : playerJumpHeight > 0.05f
                        ? "Прыжок"
                        : "На земле";

            g.DrawString(state, hudFont, Brushes.WhiteSmoke, 32f, 117f);

            RectangleF tipRect = new(ClientSize.Width - 330f, 18f, 300f, 86f);
            g.FillRoundedRectangle(hudBrush, tipRect, 18f);
            g.DrawRoundedRectangle(hudBorder, tipRect, 18f);
            g.DrawString("← → полосы   Space прыжок", hudFont, Brushes.White, tipRect.X + 18f, tipRect.Y + 18f);
            g.DrawString("Жёлтое можно перепрыгнуть", hudFont, Brushes.White, tipRect.X + 18f, tipRect.Y + 43f);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left && playerLane > 0)
            {
                playerLane--;
            }
            else if (e.KeyCode == Keys.Right && playerLane < LaneCount - 1)
            {
                playerLane++;
            }
            else if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Up) && playerJumpHeight <= 0.001f)
            {
                jumpPressed = true;
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Up)
            {
                jumpPressed = false;
            }
        }

        private float GetLaneOffset(int lane)
        {
            return lane switch
            {
                0 => -0.68f,
                1 => 0f,
                _ => 0.68f
            };
        }

        private PointF GetRoadPoint(float roadX, float depth)
        {
            float normalizedDepth = Math.Clamp(depth / MaxDepth, 0f, 1f);
            float t = 1f - normalizedDepth;
            float width = Lerp(ClientSize.Width * RoadTopWidthRatio, ClientSize.Width * RoadBottomWidthRatio, t);
            float x = ClientSize.Width / 2f + roadX * width / 2f;
            float y = Lerp(ClientSize.Height * HorizonRatio, ClientSize.Height - 50f, t);

            return new PointF(x, y);
        }

        private float GetScale(float depth)
        {
            float normalizedDepth = Math.Clamp(depth / MaxDepth, 0f, 1f);
            return Lerp(2.1f, 0.22f, normalizedDepth);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                gameTimer.Dispose();
                hudFont.Dispose();
                titleFont.Dispose();
                skyBrush.Dispose();
                roadBrush.Dispose();
                roadSideBrush.Dispose();
                laneMarkerBrush.Dispose();
                fogBrush.Dispose();
                highObstaclePen.Dispose();
                lowObstaclePen.Dispose();
                bonusPen.Dispose();
                playerShadowBrush.Dispose();
                rknBrush.Dispose();
                rknPen.Dispose();
            }

            base.Dispose(disposing);
        }

        private sealed class Obstacle
        {
            public int Lane { get; set; }
            public float Depth { get; set; }
            public float Height { get; set; }
            public float Width { get; set; }
            public ObstacleType Type { get; set; }
        }

        private sealed class BonusItem
        {
            public int Lane { get; set; }
            public float Depth { get; set; }
            public BonusType Kind { get; set; }
        }

        private enum ObstacleType
        {
            Low,
            High
        }

        private enum BonusType
        {
            Vpn,
            ProxyHeal,
            Slow
        }
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF bounds, float radius)
        {
            using var path = CreateRoundedRectanglePath(bounds, radius);
            graphics.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, RectangleF bounds, float radius)
        {
            using var path = CreateRoundedRectanglePath(bounds, radius);
            graphics.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF bounds, float radius)
        {
            float clampedRadius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2f);
            float diameter = clampedRadius * 2f;
            var path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
