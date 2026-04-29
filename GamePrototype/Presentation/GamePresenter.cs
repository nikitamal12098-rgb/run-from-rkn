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
            state = new GameState
            {
                SelectedRunner = view.SelectedRunner,
                SelectedSkin = view.SelectedSkin
            };

            ApplyRunnerStartProfile();
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
            UpdateObstacleStyleWindows();
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
                state.PlayerVerticalVelocity = GetJumpStrength();
            }
        }

        private void ApplyRunnerStartProfile()
        {
            state.TargetSpeed = GetRunnerStartingSpeed();
            state.CurrentSpeed = state.TargetSpeed;
            state.SpawnChance = state.SelectedRunner == RunnerType.Sprinter ? 6 : 5;
        }

        private void UpdateLaneAnimation()
        {
            state.PlayerVisualLane += (state.PlayerLane - state.PlayerVisualLane) * GetLaneAnimationSpeed();
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

        private void UpdateObstacleStyleWindows()
        {
            foreach (var obstacle in state.Obstacles)
            {
                if (!obstacle.WasThreatening &&
                    obstacle.Lane == state.PlayerLane &&
                    obstacle.Depth <= 16f &&
                    obstacle.Depth >= GameState.PlayerCollisionDepth)
                {
                    obstacle.WasThreatening = true;
                }

                if (!obstacle.WasThreatening || obstacle.StyleRewardGranted || obstacle.Depth > 2.1f)
                {
                    continue;
                }

                string? message = null;
                int comboGain = 1;

                if (obstacle.Type == ObstacleType.Low)
                {
                    if (obstacle.Lane == state.PlayerLane && state.PlayerJumpHeight >= GetLowObstacleClearHeight())
                    {
                        message = state.SelectedRunner == RunnerType.Acrobat ? "Sky Step" : "Clean Jump";
                        comboGain = state.SelectedRunner == RunnerType.Acrobat ? 2 : 1;
                    }
                    else if (obstacle.Lane != state.PlayerLane)
                    {
                        message = "Lane Escape";
                    }
                }
                else if (obstacle.Lane != state.PlayerLane)
                {
                    message = state.SelectedRunner == RunnerType.Sprinter ? "Slipstream" : "Near Miss";
                }

                if (message is not null)
                {
                    obstacle.StyleRewardGranted = true;
                    RegisterStylePlay(message, comboGain);
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
                    state.PlayerJumpHeight >= GetLowObstacleClearHeight();

                if (clearsLowObstacle)
                {
                    continue;
                }

                if (!state.IsInvincible)
                {
                    BreakCombo("Combo Lost");
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
                        state.InvincibleTimer = state.SelectedRunner == RunnerType.Classic ? 145 : 120;
                        RegisterSoftMessage("Shield Up");
                        break;
                    case BonusType.ProxyHeal:
                        state.DangerLevel -= state.SelectedRunner == RunnerType.Classic ? 30 : 25;
                        if (state.DangerLevel < 0)
                        {
                            state.DangerLevel = 0;
                        }
                        RegisterSoftMessage("Recovered");
                        break;
                    case BonusType.Slow:
                        state.SlowTimer = state.SelectedRunner == RunnerType.Acrobat ? 150 : 120;
                        RegisterSoftMessage("Time Stretch");
                        break;
                }

                state.Bonuses.RemoveAt(i);
            }
        }

        private void UpdateScore()
        {
            float scoreRate = state.CurrentSpeed * 0.12f * GetScoreMultiplier();
            if (state.AdrenalineTimer > 0)
            {
                scoreRate *= 1.28f;
            }

            state.ScoreProgress += scoreRate;

            while (state.ScoreProgress >= 1f)
            {
                state.Score++;
                state.ScoreProgress -= 1f;
            }
        }

        private void IncreaseDifficulty()
        {
            if (state.GameTick % GetSpeedGrowthInterval() == 0)
            {
                state.TargetSpeed = Math.Min(GameState.MaxSpeed, state.TargetSpeed + GetSpeedGrowthAmount());
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

            if (state.ComboFlashTimer > 0)
            {
                state.ComboFlashTimer--;
            }
            else if (state.ComboMessage.Length > 0)
            {
                state.ComboMessage = string.Empty;
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

            if (state.AdrenalineTimer > 0)
            {
                state.AdrenalineTimer--;
                desiredSpeed = Math.Min(GameState.MaxSpeed, desiredSpeed + GetAdrenalineSpeedBonus());

                if (state.AdrenalineTimer % 45 == 0 && state.DangerLevel > 0)
                {
                    state.DangerLevel--;
                }
            }

            state.CurrentSpeed += (desiredSpeed - state.CurrentSpeed) * GetSpeedSmoothing();
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
            float adrenalineRelief = state.AdrenalineTimer > 0 ? 8f : 0f;
            float targetDepth = Math.Max(8f, GameState.MaxDepth - state.DangerLevel * 0.72f + adrenalineRelief);
            state.RknDepth += (targetDepth - state.RknDepth) * 0.08f;
        }

        private void RegisterStylePlay(string message, int comboGain)
        {
            state.ComboCount += comboGain;
            if (state.ComboCount > state.BestCombo)
            {
                state.BestCombo = state.ComboCount;
            }

            state.Score += 4 + comboGain * 2;
            state.ComboMessage = $"{message} x{state.ComboCount}";
            state.ComboFlashTimer = 80;

            if (state.SelectedRunner == RunnerType.Acrobat)
            {
                state.DangerLevel = Math.Max(0, state.DangerLevel - 1);
            }

            int threshold = GetAdrenalineThreshold();
            if (state.ComboCount >= threshold)
            {
                state.AdrenalineTimer = Math.Min(240, state.AdrenalineTimer + GetAdrenalineDurationGain());
                state.ComboMessage = "ADRENALINE";
                state.ComboFlashTimer = 95;
            }
        }

        private void RegisterSoftMessage(string message)
        {
            state.ComboMessage = message;
            state.ComboFlashTimer = 55;
        }

        private void BreakCombo(string message)
        {
            if (state.ComboCount <= 0)
            {
                state.ComboMessage = message;
                state.ComboFlashTimer = 36;
                state.AdrenalineTimer = 0;
                return;
            }

            state.ComboCount = 0;
            state.AdrenalineTimer = 0;
            state.ComboMessage = message;
            state.ComboFlashTimer = 55;
        }

        private float GetRunnerStartingSpeed()
        {
            return state.SelectedRunner switch
            {
                RunnerType.Sprinter => 4.4f,
                RunnerType.Acrobat => 3.9f,
                _ => GameState.StartingSpeed
            };
        }

        private float GetJumpStrength()
        {
            return state.SelectedRunner switch
            {
                RunnerType.Acrobat => 0.42f,
                RunnerType.Sprinter => 0.35f,
                _ => GameState.JumpStrength
            };
        }

        private float GetLowObstacleClearHeight()
        {
            return state.SelectedRunner switch
            {
                RunnerType.Acrobat => 0.67f,
                RunnerType.Sprinter => 0.77f,
                _ => GameState.LowObstacleJumpClearHeight
            };
        }

        private float GetLaneAnimationSpeed()
        {
            return state.SelectedRunner switch
            {
                RunnerType.Sprinter => 0.29f,
                RunnerType.Acrobat => 0.24f,
                _ => 0.22f
            };
        }

        private float GetScoreMultiplier()
        {
            return state.SelectedRunner switch
            {
                RunnerType.Sprinter => 1.12f,
                RunnerType.Acrobat => 1.05f,
                _ => 1f
            };
        }

        private int GetSpeedGrowthInterval()
        {
            return state.SelectedRunner == RunnerType.Sprinter ? 210 : 240;
        }

        private float GetSpeedGrowthAmount()
        {
            return state.SelectedRunner == RunnerType.Sprinter ? 0.4f : 0.35f;
        }

        private float GetSpeedSmoothing()
        {
            return state.SelectedRunner == RunnerType.Sprinter ? 0.05f : 0.035f;
        }

        private float GetAdrenalineSpeedBonus()
        {
            return state.SelectedRunner switch
            {
                RunnerType.Sprinter => 0.65f,
                RunnerType.Acrobat => 0.28f,
                _ => 0.38f
            };
        }

        private int GetAdrenalineThreshold()
        {
            return state.SelectedRunner switch
            {
                RunnerType.Acrobat => 4,
                RunnerType.Sprinter => 3,
                _ => 3
            };
        }

        private int GetAdrenalineDurationGain()
        {
            return state.SelectedRunner switch
            {
                RunnerType.Sprinter => 105,
                RunnerType.Acrobat => 90,
                _ => 95
            };
        }
    }
}
