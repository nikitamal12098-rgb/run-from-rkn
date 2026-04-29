using System.Collections.Generic;

namespace RunnerGame.Model
{
    public sealed class GameState
    {
        public const int LaneCount = 3;
        public const float MaxDepth = 100f;
        public const float MinSpawnDepth = 30f;
        public const float StartingSpeed = 4f;
        public const float MaxSpeed = 11f;
        public const float PostHitSpeed = 7f;
        public const float JumpStrength = 0.37f;
        public const float Gravity = 0.028f;
        public const float PlayerCollisionDepth = 7.4f;
        public const float LowObstacleJumpClearHeight = 0.74f;

        public List<ObstacleModel> Obstacles { get; } = new();
        public List<BonusItemModel> Bonuses { get; } = new();

        public int GameTick { get; set; }
        public int Score { get; set; }
        public float ScoreProgress { get; set; }
        public float TargetSpeed { get; set; } = StartingSpeed;
        public float CurrentSpeed { get; set; } = StartingSpeed;
        public int SpawnChance { get; set; } = 5;
        public int BonusCooldown { get; set; }
        public int HitFlashTimer { get; set; }
        public int SlowTimer { get; set; }
        public int InvincibleTimer { get; set; }
        public int DangerLevel { get; set; }
        public bool IsInvincible { get; set; }
        public bool IsGameOver { get; set; }
        public int PlayerLane { get; set; } = 1;
        public float PlayerVisualLane { get; set; } = 1f;
        public float PlayerJumpHeight { get; set; }
        public float PlayerVerticalVelocity { get; set; }
        public float RknDepth { get; set; } = MaxDepth - 4f;
        public RunnerType SelectedRunner { get; set; } = RunnerType.Classic;
        public SkinType SelectedSkin { get; set; } = SkinType.Default;
        public int ComboCount { get; set; }
        public int BestCombo { get; set; }
        public int ComboFlashTimer { get; set; }
        public string ComboMessage { get; set; } = string.Empty;
        public int AdrenalineTimer { get; set; }

        public int MaxDanger { get; } = 100;
        public int SmallHit { get; } = 25;
        public int BigHit { get; } = 50;
    }
}
