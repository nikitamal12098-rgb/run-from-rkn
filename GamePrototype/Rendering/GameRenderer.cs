using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using RunnerGame.Model;

namespace RunnerGame.Rendering
{
    public sealed class GameRenderer : IDisposable
    {
        private const float HorizonRatio = 0.34f;
        private const float RoadTopWidthRatio = 0.18f;
        private const float RoadBottomWidthRatio = 0.92f;
        private const float PlayerGroundOffset = 86f;
        private const float PlayerJumpPixels = 118f;

        private readonly Font hudFont = new("Arial", 12, FontStyle.Bold);
        private readonly Font titleFont = new("Arial", 18, FontStyle.Bold);
        private readonly Brush roadBrush = new SolidBrush(Color.FromArgb(32, 42, 52));
        private readonly Brush roadSideBrush = new SolidBrush(Color.FromArgb(12, 24, 28));
        private readonly Brush playerShadowBrush = new SolidBrush(Color.FromArgb(90, 20, 20, 20));
        private readonly Pen highObstaclePen = new(Color.FromArgb(40, 15, 15), 2f);
        private readonly Pen lowObstaclePen = new(Color.FromArgb(35, 35, 10), 2f);
        private readonly Pen bonusPen = new(Color.FromArgb(200, 225, 255, 255), 2f);
        private readonly SpriteAssets assets = new();
        private readonly ImageAttributes translucentImageAttributes = CreateImageAttributes(0.34f);
        private readonly ImageAttributes rknImageAttributes = CreateImageAttributes(0.68f);

        public void Render(Graphics graphics, Size clientSize, GameState state)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            DrawWorld(graphics, clientSize, state);
            DrawDecisionLine(graphics, clientSize, state);
            DrawObstacles(graphics, clientSize, state);
            DrawBonuses(graphics, clientSize, state);
            DrawPlayer(graphics, clientSize, state);
            DrawRkn(graphics, clientSize, state);
            DrawHud(graphics, clientSize, state);
            DrawLaneThreats(graphics, clientSize, state);
        }

        public void Dispose()
        {
            hudFont.Dispose();
            titleFont.Dispose();
            roadBrush.Dispose();
            roadSideBrush.Dispose();
            playerShadowBrush.Dispose();
            highObstaclePen.Dispose();
            lowObstaclePen.Dispose();
            bonusPen.Dispose();
            assets.Dispose();
            translucentImageAttributes.Dispose();
            rknImageAttributes.Dispose();
        }

        private void DrawWorld(Graphics g, Size clientSize, GameState state)
        {
            float width = clientSize.Width;
            float height = clientSize.Height;
            float horizonY = height * HorizonRatio;
            float cameraLaneOffset = GetLaneOffset(state.PlayerVisualLane);

            g.DrawImage(assets.Background, new RectangleF(0, 0, width, height));
            using var skyShade = new LinearGradientBrush(
                new PointF(0, 0),
                new PointF(0, horizonY),
                Color.FromArgb(70, 6, 16, 22),
                Color.FromArgb(10, 6, 16, 22));
            g.FillRectangle(skyShade, 0, 0, width, horizonY);

            PointF leftTop = GetRoadPoint(clientSize, -1.38f - cameraLaneOffset, GameState.MinSpawnDepth);
            PointF rightTop = GetRoadPoint(clientSize, 1.38f - cameraLaneOffset, GameState.MinSpawnDepth);
            PointF leftBottom = GetRoadPoint(clientSize, -1.6f - cameraLaneOffset, 0.5f);
            PointF rightBottom = GetRoadPoint(clientSize, 1.6f - cameraLaneOffset, 0.5f);

            DrawTunnelWall(g, clientSize, new[] { new PointF(0, horizonY), leftTop, leftBottom, new PointF(0, height) }, true);
            DrawTunnelWall(g, clientSize, new[] { rightTop, new PointF(width, horizonY), new PointF(width, height), rightBottom }, false);
            g.FillPolygon(roadSideBrush, new[] { leftTop, leftBottom, new PointF(0, height), new PointF(0, horizonY) });
            g.FillPolygon(roadSideBrush, new[] { rightTop, rightBottom, new PointF(width, height), new PointF(width, horizonY) });
            g.FillPolygon(roadBrush, new[] { leftTop, rightTop, rightBottom, leftBottom });

            using var roadOverlay = new LinearGradientBrush(
                new PointF(0, horizonY),
                new PointF(0, height),
                Color.FromArgb(70, 20, 180, 180),
                Color.FromArgb(135, 0, 0, 0));
            g.FillPolygon(roadOverlay, new[] { leftTop, rightTop, rightBottom, leftBottom });

            DrawLaneMarkers(g, clientSize, state, cameraLaneOffset);
            DrawWallLines(g, clientSize, state, cameraLaneOffset);

            using var fogGradient = new LinearGradientBrush(
                new PointF(0, horizonY),
                new PointF(0, height),
                Color.FromArgb(18, 120, 255, 255),
                Color.FromArgb(90, 0, 10, 14));
            g.FillRectangle(fogGradient, 0, horizonY, width, height - horizonY);
        }

        private void DrawTunnelWall(Graphics g, Size clientSize, PointF[] polygon, bool isLeftWall)
        {
            var state = g.Save();
            using var clipPath = new GraphicsPath();
            clipPath.AddPolygon(polygon);
            g.SetClip(clipPath);

            float width = clientSize.Width;
            float height = clientSize.Height;
            var sourceRect = isLeftWall
                ? new RectangleF(0, 0, assets.Background.Width * 0.48f, assets.Background.Height)
                : new RectangleF(assets.Background.Width * 0.52f, 0, assets.Background.Width * 0.48f, assets.Background.Height);

            float drawX = isLeftWall ? -width * 0.12f : width * 0.18f;
            g.DrawImage(
                assets.Background,
                new Rectangle((int)drawX, 0, (int)(width * 0.95f), (int)height),
                sourceRect.X,
                sourceRect.Y,
                sourceRect.Width,
                sourceRect.Height,
                GraphicsUnit.Pixel,
                translucentImageAttributes);

            using var tintBrush = new LinearGradientBrush(
                new PointF(0, 0),
                new PointF(0, height),
                Color.FromArgb(105, 0, 32, 38),
                Color.FromArgb(165, 0, 12, 18));
            g.FillPolygon(tintBrush, polygon);
            g.Restore(state);
        }

        private void DrawLaneMarkers(Graphics g, Size clientSize, GameState state, float cameraLaneOffset)
        {
            for (int laneSeparator = 1; laneSeparator < GameState.LaneCount; laneSeparator++)
            {
                float laneRatio = (laneSeparator / (float)GameState.LaneCount) * 2f - 1f;

                for (float depth = 6f; depth < GameState.MaxDepth; depth += 8f)
                {
                    PointF p1 = GetRoadPoint(clientSize, laneRatio - cameraLaneOffset, depth);
                    PointF p2 = GetRoadPoint(clientSize, laneRatio - cameraLaneOffset, depth + 3.8f);
                    float thickness = Math.Max(1f, GetScale(depth) * 11f);
                    using var markerPen = new Pen(Color.FromArgb(215, 120, 255, 245), thickness)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };
                    g.DrawLine(markerPen, p1, p2);
                }
            }

            for (float depth = 10f; depth < GameState.MaxDepth; depth += 12f)
            {
                PointF left = GetRoadPoint(clientSize, -1.05f - cameraLaneOffset, depth);
                PointF right = GetRoadPoint(clientSize, 1.05f - cameraLaneOffset, depth);
                float thickness = Math.Max(1f, GetScale(depth) * 3f);
                using var crossPen = new Pen(Color.FromArgb(45, 140, 255, 245), thickness);
                g.DrawLine(crossPen, left, right);
            }
        }

        private void DrawWallLines(Graphics g, Size clientSize, GameState state, float cameraLaneOffset)
        {
            for (float depth = 8f; depth < GameState.MaxDepth; depth += 8f)
            {
                PointF leftNear = GetRoadPoint(clientSize, -1.45f - cameraLaneOffset, depth);
                PointF leftFar = GetRoadPoint(clientSize, -1.8f - cameraLaneOffset, depth + 14f);
                PointF rightNear = GetRoadPoint(clientSize, 1.45f - cameraLaneOffset, depth);
                PointF rightFar = GetRoadPoint(clientSize, 1.8f - cameraLaneOffset, depth + 14f);

                float thickness = Math.Max(1f, GetScale(depth) * 3f);
                using var wallPen = new Pen(Color.FromArgb(60, 110, 255, 230), thickness);
                g.DrawLine(wallPen, leftNear, leftFar);
                g.DrawLine(wallPen, rightNear, rightFar);
            }
        }

        private void DrawDecisionLine(Graphics g, Size clientSize, GameState state)
        {
            float cameraLaneOffset = GetLaneOffset(state.PlayerVisualLane);
            PointF left = GetRoadPoint(clientSize, -1.08f - cameraLaneOffset, GameState.PlayerCollisionDepth);
            PointF right = GetRoadPoint(clientSize, 1.08f - cameraLaneOffset, GameState.PlayerCollisionDepth);
            float thickness = Math.Max(3f, GetScale(GameState.PlayerCollisionDepth) * 5f);

            using var glowPen = new Pen(Color.FromArgb(170, 255, 255, 255), thickness)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawLine(glowPen, left, right);
        }

        private void DrawObstacles(Graphics g, Size clientSize, GameState state)
        {
            float cameraLaneOffset = GetLaneOffset(state.PlayerVisualLane);
            state.Obstacles.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            foreach (var obstacle in state.Obstacles)
            {
                DrawObstacle(g, clientSize, obstacle, cameraLaneOffset);
            }
        }

        private void DrawObstacle(Graphics g, Size clientSize, ObstacleModel obstacle, float cameraLaneOffset)
        {
            float scale = GetScale(obstacle.Depth);
            PointF basePoint = GetRoadPoint(clientSize, GetLaneOffset(obstacle.Lane) - cameraLaneOffset, obstacle.Depth);
            bool isNear = obstacle.Depth <= 24f;

            if (obstacle.Type == ObstacleType.High)
            {
                DrawObstaclePrism(
                    g,
                    basePoint,
                    scale,
                    width: 112f * obstacle.Width,
                    height: 174f * obstacle.Height,
                    topInset: 18f * scale,
                    frontTexture: assets.ObstacleHigh,
                    frontTintTop: Color.FromArgb(215, 92, 32, 22),
                    frontTintBottom: Color.FromArgb(245, 255, 130, 80),
                    sideColor: Color.FromArgb(165, 56, 20, 20),
                    topColor: Color.FromArgb(205, 255, 150, 95),
                    outlinePen: highObstaclePen);
            }
            else
            {
                DrawObstaclePrism(
                    g,
                    basePoint,
                    scale,
                    width: 138f * obstacle.Width,
                    height: 78f * obstacle.Height,
                    topInset: 10f * scale,
                    frontTexture: assets.ObstacleLow,
                    frontTintTop: Color.FromArgb(160, 32, 80, 120),
                    frontTintBottom: Color.FromArgb(235, 255, 210, 90),
                    sideColor: Color.FromArgb(170, 80, 65, 18),
                    topColor: Color.FromArgb(210, 255, 230, 120),
                    outlinePen: lowObstaclePen);
            }

            float width = obstacle.Type == ObstacleType.High
                ? 112f * obstacle.Width * scale
                : 138f * obstacle.Width * scale;
            float shadowWidth = width * 1.18f;
            float shadowHeight = Math.Max(6f, 18f * scale);
            g.FillEllipse(
                playerShadowBrush,
                basePoint.X - shadowWidth / 2f,
                basePoint.Y - shadowHeight / 2f,
                shadowWidth,
                shadowHeight);

            if (isNear)
            {
                float warningSize = Math.Max(18f, 32f * scale);
                float topY = basePoint.Y - (obstacle.Type == ObstacleType.High ? 174f * obstacle.Height * scale : 78f * obstacle.Height * scale);
                PointF tip = new(basePoint.X, topY - warningSize * 0.8f);
                PointF left = new(basePoint.X - warningSize / 2f, topY - 4f);
                PointF right = new(basePoint.X + warningSize / 2f, topY - 4f);
                using var warningBrush = new SolidBrush(Color.FromArgb(215, 255, 245, 110));
                g.FillPolygon(warningBrush, new[] { tip, left, right });
            }
        }

        private void DrawObstaclePrism(
            Graphics g,
            PointF basePoint,
            float scale,
            float width,
            float height,
            float topInset,
            Image frontTexture,
            Color frontTintTop,
            Color frontTintBottom,
            Color sideColor,
            Color topColor,
            Pen outlinePen)
        {
            float scaledWidth = width * scale;
            float scaledHeight = height * scale;
            RectangleF frontRect = new(basePoint.X - scaledWidth / 2f, basePoint.Y - scaledHeight, scaledWidth, scaledHeight);

            PointF topFrontLeft = new(frontRect.Left, frontRect.Top);
            PointF topFrontRight = new(frontRect.Right, frontRect.Top);
            PointF topBackLeft = new(frontRect.Left + topInset, frontRect.Top - topInset);
            PointF topBackRight = new(frontRect.Right + topInset, frontRect.Top - topInset);
            PointF bottomRight = new(frontRect.Right, frontRect.Bottom);
            PointF bottomBackRight = new(frontRect.Right + topInset, frontRect.Bottom - topInset);

            using var sideBrush = new SolidBrush(sideColor);
            using var topBrush = new SolidBrush(topColor);
            g.FillPolygon(sideBrush, new[] { topFrontRight, topBackRight, bottomBackRight, bottomRight });
            g.FillPolygon(topBrush, new[] { topFrontLeft, topFrontRight, topBackRight, topBackLeft });

            using (new GraphicsClipScope(g, frontRect))
            {
                g.DrawImage(frontTexture, frontRect);
            }

            using var frontTintBrush = new LinearGradientBrush(frontRect, frontTintTop, frontTintBottom, LinearGradientMode.Vertical);
            g.FillRectangle(frontTintBrush, frontRect);

            g.DrawRectangle(outlinePen, frontRect.X, frontRect.Y, frontRect.Width, frontRect.Height);
            g.DrawPolygon(outlinePen, new[] { topFrontLeft, topFrontRight, topBackRight, topBackLeft });
            g.DrawPolygon(outlinePen, new[] { topFrontRight, topBackRight, bottomBackRight, bottomRight });
        }

        private void DrawBonuses(Graphics g, Size clientSize, GameState state)
        {
            float cameraLaneOffset = GetLaneOffset(state.PlayerVisualLane);
            state.Bonuses.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            foreach (var bonus in state.Bonuses)
            {
                float scale = GetScale(bonus.Depth);
                float size = 70f * scale;
                PointF center = GetRoadPoint(clientSize, GetLaneOffset(bonus.Lane) - cameraLaneOffset, bonus.Depth);
                RectangleF rect = new(center.X - size / 2f, center.Y - size - 12f * scale, size, size);
                using var glowBrush = new SolidBrush(Color.FromArgb(70, 120, 255, 240));
                g.FillEllipse(glowBrush, rect.X - 6f * scale, rect.Y - 6f * scale, rect.Width + 12f * scale, rect.Height + 12f * scale);
                g.DrawImage(GetBonusImage(bonus.Kind), rect);
                g.DrawRoundedRectangle(bonusPen, rect, 12f * scale);
            }
        }

        private void DrawPlayer(Graphics g, Size clientSize, GameState state)
        {
            float screenCenterX = clientSize.Width / 2f;
            float baseY = clientSize.Height - PlayerGroundOffset;
            float sway = (state.PlayerVisualLane - 1f) * 18f;
            float jumpOffset = Math.Min(138f, state.PlayerJumpHeight * PlayerJumpPixels);
            float headSize = state.PlayerJumpHeight > 0.05f ? 132f : 122f;
            RectangleF headRect = new(screenCenterX - headSize / 2f + sway, baseY - jumpOffset - headSize - 14f, headSize, headSize);

            float shadowWidth = 84f - Math.Min(26f, state.PlayerJumpHeight * 22f);
            float shadowHeight = 16f;
            g.FillEllipse(
                playerShadowBrush,
                headRect.X + headRect.Width / 2f - shadowWidth / 2f,
                baseY - shadowHeight / 2f,
                shadowWidth,
                shadowHeight);

            Color runnerColor = GetRunnerAccentColor(state);
            Color glowColor = state.IsInvincible
                ? Color.FromArgb(80, 85, 255, 210)
                : state.SlowTimer > 0
                    ? Color.FromArgb(65, 120, 255, 245)
                    : state.HitFlashTimer > 0 && state.HitFlashTimer % 4 < 2
                        ? Color.FromArgb(85, 255, 80, 80)
                        : Color.FromArgb(48, runnerColor);

            using var glowBrush = new SolidBrush(glowColor);
            g.FillEllipse(
                glowBrush,
                headRect.X - 12f,
                headRect.Y - 10f,
                headRect.Width + 24f,
                headRect.Height + 24f);

            g.DrawImage(GetPlayerImage(state), headRect);

            using var laneRingPen = new Pen(Color.FromArgb(200, runnerColor), 3f);
            g.DrawArc(
                laneRingPen,
                headRect.X - 18f,
                baseY - 26f,
                headRect.Width + 36f,
                28f,
                0f,
                180f);

            DrawJumpAssist(g, clientSize, state, headRect, baseY);
        }

        private void DrawRkn(Graphics g, Size clientSize, GameState state)
        {
            PointF center = GetRoadPoint(clientSize, -GetLaneOffset(state.PlayerVisualLane) * 0.35f, state.RknDepth);
            float scale = Math.Max(0.2f, GetScale(state.RknDepth));
            float width = 220f * scale;
            float height = 140f * scale;
            RectangleF rect = new(center.X - width / 2f, center.Y - height - 6f, width, height);
            using var glowBrush = new SolidBrush(Color.FromArgb(55, 255, 70, 70));
            g.FillEllipse(glowBrush, rect.X - 12f * scale, rect.Bottom - 28f * scale, rect.Width + 24f * scale, 36f * scale);
            g.DrawImage(
                assets.Rkn,
                Rectangle.Round(rect),
                0,
                0,
                assets.Rkn.Width,
                assets.Rkn.Height,
                GraphicsUnit.Pixel,
                rknImageAttributes);
        }

        private void DrawHud(Graphics g, Size clientSize, GameState state)
        {
            RectangleF hudRect = new(18f, 16f, 310f, 152f);
            using var hudBrush = new SolidBrush(Color.FromArgb(145, 10, 16, 28));
            using var hudBorder = new Pen(Color.FromArgb(130, 255, 255, 255), 1.5f);
            g.FillRoundedRectangle(hudBrush, hudRect, 18f);
            g.DrawRoundedRectangle(hudBorder, hudRect, 18f);

            Color dangerColor = state.DangerLevel switch
            {
                > 70 => Color.FromArgb(255, 85, 85),
                > 30 => Color.FromArgb(255, 190, 70),
                _ => Color.FromArgb(80, 225, 140)
            };

            Color runnerColor = GetRunnerAccentColor(state);
            using var dangerBrush = new SolidBrush(dangerColor);
            using var runnerBrush = new SolidBrush(runnerColor);
            g.DrawString($"Score: {state.Score}", titleFont, Brushes.White, 32f, 28f);
            g.DrawString($"Runner: {GetRunnerName(state.SelectedRunner)}", hudFont, runnerBrush, 32f, 54f);
            g.DrawString($"Skin: {GetSkinName(state.SelectedSkin)}", hudFont, Brushes.WhiteSmoke, 170f, 54f);
            g.DrawString($"RKN: {state.DangerLevel}%", hudFont, dangerBrush, 32f, 65f);
            g.DrawString($"Скорость: {state.CurrentSpeed:0.0}", hudFont, Brushes.WhiteSmoke, 32f, 89f);
            g.DrawString(GetStateLabel(state), hudFont, Brushes.WhiteSmoke, 32f, 113f);
            g.DrawString($"Combo: x{state.ComboCount}", hudFont, Brushes.WhiteSmoke, 32f, 137f);

            RectangleF tipRect = new(clientSize.Width - 350f, 18f, 320f, 92f);
            g.FillRoundedRectangle(hudBrush, tipRect, 18f);
            g.DrawRoundedRectangle(hudBorder, tipRect, 18f);
            g.DrawString("← → меняют полосу мира", hudFont, Brushes.White, tipRect.X + 18f, tipRect.Y + 16f);
            g.DrawString("Space / Up: прыжок над DDOS", hudFont, Brushes.White, tipRect.X + 18f, tipRect.Y + 40f);
            g.DrawString("DDOS прыгай, hacker объезжай", hudFont, Brushes.White, tipRect.X + 18f, tipRect.Y + 64f);

            DrawStyleHud(g, clientSize, state, hudBrush, hudBorder);
        }

        private void DrawStyleHud(Graphics g, Size clientSize, GameState state, Brush hudBrush, Pen hudBorder)
        {
            RectangleF styleRect = new(clientSize.Width - 350f, 122f, 320f, 92f);
            g.FillRoundedRectangle(hudBrush, styleRect, 18f);
            g.DrawRoundedRectangle(hudBorder, styleRect, 18f);

            string adrenalineText = state.AdrenalineTimer > 0
                ? $"Adrenaline: {state.AdrenalineTimer / 60f:0.0}s"
                : "Adrenaline: charging";

            Color adrenalineColor = state.AdrenalineTimer > 0
                ? Color.FromArgb(255, 100, 225, 170)
                : Color.FromArgb(210, 185, 195, 205);
            using var adrenalineBrush = new SolidBrush(adrenalineColor);

            g.DrawString(adrenalineText, hudFont, adrenalineBrush, styleRect.X + 18f, styleRect.Y + 14f);
            g.DrawString($"Best combo: x{Math.Max(state.BestCombo, state.ComboCount)}", hudFont, Brushes.White, styleRect.X + 18f, styleRect.Y + 39f);

            string message = state.ComboMessage.Length > 0 ? state.ComboMessage : "Стиль играет роль";
            Brush messageBrush = state.ComboFlashTimer > 0 ? adrenalineBrush : Brushes.WhiteSmoke;
            g.DrawString(message, hudFont, messageBrush, styleRect.X + 18f, styleRect.Y + 63f);
        }

        private void DrawLaneThreats(Graphics g, Size clientSize, GameState state)
        {
            RectangleF panel = new(clientSize.Width / 2f - 180f, clientSize.Height - 78f, 360f, 50f);
            using var panelBrush = new SolidBrush(Color.FromArgb(150, 7, 10, 16));
            using var borderPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1.4f);
            g.FillRoundedRectangle(panelBrush, panel, 18f);
            g.DrawRoundedRectangle(borderPen, panel, 18f);

            for (int lane = 0; lane < GameState.LaneCount; lane++)
            {
                float cellWidth = panel.Width / GameState.LaneCount;
                RectangleF laneRect = new(panel.X + lane * cellWidth + 6f, panel.Y + 6f, cellWidth - 12f, panel.Height - 12f);
                bool isPlayerLane = lane == state.PlayerLane;
                float nearestObstacleDepth = GetNearestObstacleDepth(state, lane);

                Color laneColor = Color.FromArgb(70, 90, 110);
                if (nearestObstacleDepth <= 15f)
                {
                    laneColor = Color.FromArgb(210, 210, 65, 65);
                }
                else if (nearestObstacleDepth <= 28f)
                {
                    laneColor = Color.FromArgb(190, 235, 165, 65);
                }
                else if (isPlayerLane)
                {
                    laneColor = Color.FromArgb(180, 70, 200, 120);
                }

                using var laneBrush = new SolidBrush(laneColor);
                g.FillRoundedRectangle(laneBrush, laneRect, 12f);

                string laneLabel = lane switch
                {
                    0 => "L",
                    1 => "C",
                    _ => "R"
                };

                using var labelFont = new Font("Arial", 12, FontStyle.Bold);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(laneLabel, labelFont, Brushes.White, laneRect, sf);
            }
        }

        private float GetNearestObstacleDepth(GameState state, int lane)
        {
            float nearest = float.MaxValue;

            foreach (var obstacle in state.Obstacles)
            {
                if (obstacle.Lane == lane && obstacle.Depth < nearest)
                {
                    nearest = obstacle.Depth;
                }
            }

            return nearest;
        }

        private string GetStateLabel(GameState state)
        {
            if (state.IsInvincible)
            {
                return $"VPN {state.InvincibleTimer / 60f:0.0}s";
            }

            if (state.SlowTimer > 0)
            {
                return $"Замедление {state.SlowTimer / 60f:0.0}s";
            }

            if (state.PlayerJumpHeight > 0.05f)
            {
                return "Прыжок";
            }

            return "На земле";
        }

        private Image GetBonusImage(BonusType bonusType)
        {
            return bonusType switch
            {
                BonusType.Vpn => assets.BonusVpn,
                BonusType.ProxyHeal => assets.BonusProxy,
                _ => assets.BonusSlow
            };
        }

        private Image GetPlayerImage(GameState state)
        {
            if (state.HitFlashTimer > 0 && state.HitFlashTimer % 4 < 2)
            {
                return assets.PlayerHit;
            }

            if (state.IsInvincible || state.SlowTimer > 0)
            {
                return assets.PlayerBuff;
            }

            return assets.PlayerNormal;
        }

        private string GetRunnerName(RunnerType runnerType)
        {
            return runnerType switch
            {
                RunnerType.Sprinter => "Sprinter",
                RunnerType.Acrobat => "Acrobat",
                _ => "Classic"
            };
        }

        private Color GetRunnerAccentColor(GameState state)
        {
            if (state.SelectedSkin != SkinType.Default)
            {
                return GetSkinAccentColor(state.SelectedSkin);
            }

            return state.SelectedRunner switch
            {
                RunnerType.Sprinter => Color.FromArgb(255, 180, 95),
                RunnerType.Acrobat => Color.FromArgb(120, 255, 210),
                _ => Color.FromArgb(145, 255, 240)
            };
        }

        private string GetSkinName(SkinType skinType)
        {
            return skinType switch
            {
                SkinType.Neon => "Neon",
                SkinType.Crimson => "Crimson",
                SkinType.Ghost => "Ghost",
                _ => "Default"
            };
        }

        private Color GetSkinAccentColor(SkinType skinType)
        {
            return skinType switch
            {
                SkinType.Neon => Color.FromArgb(95, 255, 225),
                SkinType.Crimson => Color.FromArgb(255, 110, 90),
                SkinType.Ghost => Color.FromArgb(180, 220, 255),
                _ => Color.FromArgb(145, 255, 240)
            };
        }

        private void DrawJumpAssist(Graphics g, Size clientSize, GameState state, RectangleF headRect, float baseY)
        {
            if (state.PlayerJumpHeight <= 0.05f)
            {
                return;
            }

            ObstacleModel? nearestLowObstacle = null;
            foreach (var obstacle in state.Obstacles)
            {
                if (obstacle.Lane != state.PlayerLane || obstacle.Type != ObstacleType.Low)
                {
                    continue;
                }

                if (obstacle.Depth > 12.5f || obstacle.Depth < 3f)
                {
                    continue;
                }

                if (nearestLowObstacle is null || obstacle.Depth < nearestLowObstacle.Depth)
                {
                    nearestLowObstacle = obstacle;
                }
            }

            if (nearestLowObstacle is null)
            {
                return;
            }

            bool willClear = state.PlayerJumpHeight >= GameState.LowObstacleJumpClearHeight;
            Color assistColor = willClear ? Color.FromArgb(210, 80, 255, 180) : Color.FromArgb(210, 255, 95, 95);
            using var assistPen = new Pen(assistColor, 4f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            PointF arcStart = new(headRect.Left + 12f, baseY - 24f);
            PointF arcEnd = new(headRect.Right - 12f, baseY - 24f);
            PointF arcPeak = new(headRect.X + headRect.Width / 2f, headRect.Y - 18f);
            g.DrawBezier(assistPen, arcStart, arcPeak, arcPeak, arcEnd);
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

        private float GetLaneOffset(float lane)
        {
            return (lane - 1f) * 0.68f;
        }

        private PointF GetRoadPoint(Size clientSize, float roadX, float depth)
        {
            float normalizedDepth = Math.Clamp(depth / GameState.MaxDepth, 0f, 1f);
            float t = 1f - normalizedDepth;
            float width = Lerp(clientSize.Width * RoadTopWidthRatio, clientSize.Width * RoadBottomWidthRatio, t);
            float x = clientSize.Width / 2f + roadX * width / 2f;
            float y = Lerp(clientSize.Height * HorizonRatio, clientSize.Height - 50f, t);
            return new PointF(x, y);
        }

        private float GetScale(float depth)
        {
            float normalizedDepth = Math.Clamp(depth / GameState.MaxDepth, 0f, 1f);
            return Lerp(2.1f, 0.22f, normalizedDepth);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private static ImageAttributes CreateImageAttributes(float alpha)
        {
            var imageAttributes = new ImageAttributes();
            var colorMatrix = new ColorMatrix
            {
                Matrix33 = alpha
            };
            imageAttributes.SetColorMatrix(colorMatrix);
            return imageAttributes;
        }

        private sealed class GraphicsClipScope : IDisposable
        {
            private readonly Graphics graphics;
            private readonly GraphicsState state;

            public GraphicsClipScope(Graphics graphics, RectangleF clipBounds)
            {
                this.graphics = graphics;
                state = graphics.Save();
                graphics.SetClip(clipBounds);
            }

            public void Dispose()
            {
                graphics.Restore(state);
            }
        }
    }
}
