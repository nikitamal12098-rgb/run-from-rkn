using System.Windows.Forms;

namespace RunnerGame
{
    public partial class GameForm
    {
        private void CheckCollisions()
        {
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = obstacles[i];

                if (player.Bounds.IntersectsWith(obstacle.Bounds))
                {
                    if (!isInvincible)
                    {
                        hitTimer = 20;
                        player.Image = playerHitImage;

                        if (obstacle.Tag?.ToString() == "hard")
                        {
                            dangerLevel += bigHit;
                        }
                        else
                        {
                            dangerLevel += smallHit;
                        }

                        if (dangerLevel > maxDanger)
                        {
                            dangerLevel = maxDanger;
                        }
                    }

                    Controls.Remove(obstacle);
                    obstacles.RemoveAt(i);
                }
            }

            for (int i = bonuses.Count - 1; i >= 0; i--)
            {
                var bonus = bonuses[i];

                if (player.Bounds.IntersectsWith(bonus.Bounds))
                {
                    var type = bonus.Tag?.ToString();

                    if (type == "vpn")
                    {
                        isInvincible = true;
                        invincibleTimer = 120;
                        player.Image = playerVpnImage;
                    }
                    else if (type == "proxy_heal")
                    {
                        dangerLevel -= 25;

                        if (dangerLevel < 0)
                        {
                            dangerLevel = 0;
                        }
                    }
                    else if (type == "slow")
                    {
                        slowTimer = 120;
                        player.Image = playerVpnImage;
                    }

                    Controls.Remove(bonus);
                    bonuses.RemoveAt(i);
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left && playerLane > 0)
            {
                playerLane--;
                UpdatePlayerPosition();
            }
            else if (e.KeyCode == Keys.Right && playerLane < 2)
            {
                playerLane++;
                UpdatePlayerPosition();
            }
        }

        private void UpdatePlayerPosition()
        {
            var x = 50 + playerLane * laneWidth;
            var y = ClientSize.Height - 220;
            player.Location = new System.Drawing.Point(x, y);
        }
    }
}
