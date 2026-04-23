using System;
using System.Collections.Generic;
using RunnerGame.Model;
using RunnerGame.Rendering;

namespace RunnerGame.Presentation
{
    public sealed class GamePresenter
    {
        private readonly IGameView view;
        private readonly GameRenderer renderer;
        private readonly Random random = new();
        private GameState state;

        public GamePresenter(IGameView view, GameRenderer renderer)
        {
            this.view = view;
            this.renderer = renderer;
            state = new GameState();

            view.Attach(state, renderer);
            view.StartGameRequested += HandleStartGameRequested;
            view.FrameAdvanced += HandleFrameAdvanced;
            view.MoveLeftRequested += HandleMoveLeftRequested;
            view.MoveRightRequested += HandleMoveRightRequested;
            view.JumpRequested += HandleJumpRequested;
            view.ShowMainMenu();
            view.RequestSceneRefresh();
        }

        private void HandleStartGameRequested(object? sender, EventArgs e)
        {
            state = new GameState();
            view.Attach(state, renderer);
            view.HideMainMenu();
            view.Start();
            view.RequestSceneRefresh();
        }

        private void HandleFrameAdvanced(object? sender, EventArgs e)
        {
            if (state.IsGameOver)
            {
                return;
            }

            state.GameTick++;

            UpdateLaneAnimation();
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

            if (state.BonusCooldown > 0)
            {
                state.BonusCooldown--;
            }

            view.RequestSceneRefresh();
        }

        private void HandleMoveLeftRequested(object? sender, EventArgs e)
        {
            if (state.PlayerLane > 0)
            {
                state.PlayerLane--;
            }
        }

        private void HandleMoveRightRequested(object? sender, EventArgs e)
        {
            if (state.PlayerLane < GameState.LaneCount - 1)
            {
                state.PlayerLane++;
            }
        }

        private void HandleJumpRequested(object? sender, EventArgs e)
        {
            if (state.PlayerJumpHeight <= 0.001f)
            {
                state.PlayerVerticalVelocity = GameState.JumpStrength;
            }
        }

        private void UpdateLaneAnimation()
        {
            state.PlayerVisualLane += (state.PlayerLane - state.PlayerVisualLane) * 0.22f;
        }

        private void UpdatePlayerJump()
        {
            if (state.PlayerJumpHeight > 0f || state.PlayerVerticalVelocity > 0f)
            {
                state.PlayerJumpHeight += state.PlayerVerticalVelocity;
                state.PlayerVerticalVelocity -= GameState.Gravity;

                if (state.PlayerJumpHeight < 0f)
                {
                    state.PlayerJumpHeight = 0f;
                    state.PlayerVerticalVelocity = 0f;
                }
            }
        }

        private void MoveObstacles()
        {
            for (int i = state.Obstacles.Count - 1; i >= 0; i--)
            {
                state.Obstacles[i].Depth -= state.CurrentSpeed * 0.42f;

                if (state.Obstacles[i].Depth <= 0.8f)
                {
                    state.Obstacles.RemoveAt(i);
                }
            }
        }

        private void MoveBonuses()
        {
            for (int i = state.Bonuses.Count - 1; i >= 0; i--)
            {
                state.Bonuses[i].Depth -= state.CurrentSpeed * 0.42f;

                if (state.Bonuses[i].Depth <= 0.8f)
                {
                    state.Bonuses.RemoveAt(i);
                }
            }
        }

        private void SpawnObstacle()
        {
            if (random.Next(0, 100) >= state.SpawnChance)
            {
                return;
            }

            foreach (var obstacle in state.Obstacles)
            {
                if (obstacle.Depth > GameState.MaxDepth - 18f)
                {
                    return;
                }
            }

            var lanes = new List<int> { 0, 1, 2 };
            int maxObstacles = state.GameTick > 1000 ? 2 : 1;
            int obstacleCount = random.Next(1, maxObstacles + 1);

            for (int i = 0; i < obstacleCount && lanes.Count > 0; i++)
            {
                int laneIndex = random.Next(lanes.Count);
                int lane = lanes[laneIndex];
                lanes.RemoveAt(laneIndex);

                bool isHigh = random.Next(100) < 35;
                state.Obstacles.Add(new ObstacleModel
                {
                    Lane = lane,
                    Depth = GameState.MaxDepth,
                    Height = isHigh ? 2.05f : 0.72f,
                    Width = isHigh ? 0.92f : 0.94f,
                    Type = isHigh ? ObstacleType.High : ObstacleType.Low
                });
            }
        }

        private void SpawnBonus()
        {
            if (state.BonusCooldown > 0 || random.Next(0, 100) >= 2)
            {
                return;
            }

            foreach (var bonus in state.Bonuses)
            {
                if (bonus.Depth > GameState.MaxDepth - 20f)
                {
                    return;
                }
            }

            state.Bonuses.Add(new BonusItemModel
            {
                Lane = random.Next(0, GameState.LaneCount),
                Depth = GameState.MaxDepth,
                Kind = (BonusType)random.Next(0, 3)
            });

            state.BonusCooldown = 120;
        }

        private void CheckCollisions()
        {
            for (int i = state.Obstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = state.Obstacles[i];

                if (obstacle.Lane != state.PlayerLane)
                {
                    continue;
                }

                if (obstacle.Depth > GameState.PlayerCollisionDepth || obstacle.Depth < 2.4f)
                {
                    continue;
                }

                bool clearsLowObstacle = obstacle.Type == ObstacleType.Low &&
                    state.PlayerJumpHeight >= GameState.LowObstacleJumpClearHeight;

                if (clearsLowObstacle)
                {
                    continue;
                }

                if (!state.IsInvincible)
                {
                    state.HitFlashTimer = 20;
                    state.DangerLevel += obstacle.Type == ObstacleType.High ? state.BigHit : state.SmallHit;
                    if (state.DangerLevel > state.MaxDanger)
                    {
                        state.DangerLevel = state.MaxDanger;
                    }

                    state.CurrentSpeed = Math.Min(GameState.PostHitSpeed, state.TargetSpeed);
                }

                state.Obstacles.RemoveAt(i);
            }

            for (int i = state.Bonuses.Count - 1; i >= 0; i--)
            {
                var bonus = state.Bonuses[i];

                if (bonus.Lane != state.PlayerLane ||
                    bonus.Depth > GameState.PlayerCollisionDepth ||
                    bonus.Depth < 2.4f)
                {
                    continue;
                }

                switch (bonus.Kind)
                {
                    case BonusType.Vpn:
                        state.IsInvincible = true;
                        state.InvincibleTimer = 120;
                        break;
                    case BonusType.ProxyHeal:
                        state.DangerLevel -= 25;
                        if (state.DangerLevel < 0)
                        {
                            state.DangerLevel = 0;
                        }
                        break;
                    case BonusType.Slow:
                        state.SlowTimer = 120;
                        break;
                }

                state.Bonuses.RemoveAt(i);
            }
        }

        private void UpdateScore()
        {
            state.ScoreProgress += state.CurrentSpeed * 0.12f;

            while (state.ScoreProgress >= 1f)
            {
                state.Score++;
                state.ScoreProgress -= 1f;
            }
        }

        private void IncreaseDifficulty()
        {
            if (state.GameTick % 240 == 0)
            {
                state.TargetSpeed = Math.Min(GameState.MaxSpeed, state.TargetSpeed + 0.35f);
            }

            if (state.GameTick % 360 == 0 && state.SpawnChance < 14)
            {
                state.SpawnChance++;
            }
        }

        private void UpdateEffects()
        {
            float desiredSpeed = state.TargetSpeed;

            if (state.HitFlashTimer > 0)
            {
                state.HitFlashTimer--;
            }

            if (state.IsInvincible)
            {
                state.InvincibleTimer--;
                if (state.InvincibleTimer <= 0)
                {
                    state.IsInvincible = false;
                }
            }

            if (state.SlowTimer > 0)
            {
                state.SlowTimer--;
                desiredSpeed = Math.Max(2.5f, state.TargetSpeed * 0.65f);
            }

            state.CurrentSpeed += (desiredSpeed - state.CurrentSpeed) * 0.035f;
        }

        private void UpdateDanger()
        {
            if (state.DangerLevel >= state.MaxDanger)
            {
                state.IsGameOver = true;
                view.Stop();
                view.ShowGameOver(state);
                view.ShowMainMenu();
            }
        }

        private void UpdateRkn()
        {
            float targetDepth = Math.Max(8f, GameState.MaxDepth - state.DangerLevel * 0.72f);
            state.RknDepth += (targetDepth - state.RknDepth) * 0.08f;
        }
    }
}
