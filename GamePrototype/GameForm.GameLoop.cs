using System;
using System.Drawing;
using System.Windows.Forms;

namespace RunnerGame
{
    public partial class GameForm
    {
        private void GameLoop(object? sender, EventArgs e)
        {
            gameTick++;

            MoveObstacles();
            SpawnObstacle();
            CheckCollisions();

            UpdateScore();
            IncreaseDifficulty();
            UpdateDanger();

            MoveBonuses();
            SpawnBonus();
            UpdateEffects();

            UpdatePlayerState();
            UpdateRkn();

            if (bonusCooldown > 0)
            {
                bonusCooldown--;
            }
        }

        private void UpdateRkn()
        {
            var maxDistance = ClientSize.Height;
            var minDistance = player.Top - rkn.Height;

            var t = dangerLevel / 100f;
            var targetY = (int)(maxDistance - (maxDistance - minDistance) * t);

            rknY += (int)((targetY - rknY) * 0.2f);

            UpdateRknPosition();

            if (rkn.Bounds.IntersectsWith(player.Bounds))
            {
                GameOver();
            }
        }

        private void UpdatePlayerState()
        {
            if (hitTimer > 0)
            {
                player.Visible = !player.Visible;
                hitTimer--;

                if (hitTimer == 0)
                {
                    player.Image = playerNormalImage;
                }
            }
        }

        private void UpdateEffects()
        {
            currentSpeed = baseSpeed;

            if (isInvincible)
            {
                invincibleTimer--;

                if (invincibleTimer <= 0)
                {
                    isInvincible = false;
                    player.Image = playerNormalImage;
                }
            }

            if (slowTimer > 0)
            {
                slowTimer--;
                currentSpeed = baseSpeed / 2;

                if (slowTimer == 1)
                {
                    player.Image = playerNormalImage;
                }
            }
        }

        private void UpdateDanger()
        {
            dangerLabel.Text = $"RKN: {dangerLevel}%";

            if (dangerLevel > 70)
            {
                dangerLabel.ForeColor = Color.Red;
                BackColor = Color.DarkRed;
            }
            else if (dangerLevel > 30)
            {
                dangerLabel.ForeColor = Color.Orange;
            }
            else
            {
                dangerLabel.ForeColor = Color.Green;
            }
        }

        private void UpdateScore()
        {
            score++;
            scoreLabel.Text = $"Score: {score}";
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

        private void GameOver()
        {
            gameTimer.Stop();
            MessageBox.Show("Тебя поймали РКН 😈");
        }
    }
}
