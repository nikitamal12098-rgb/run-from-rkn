using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace RunnerGame
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new GameForm());
        }
    }

    public class GameForm : Form
    {
        private PictureBox rkn = null!;

        private int rknY;
        private int rknBaseSpeed = 1;

        private List<PictureBox> bonuses = new List<PictureBox>();
        private Image playerNormalImage = null!;
        private Image playerHitImage = null!;
        private Image playerVpnImage = null!;

        private int hitTimer = 0;

        private int baseSpeed = 5;
        private int currentSpeed = 5;
        private int bonusCooldown = 0;

        private bool isInvincible = false;
        private int invincibleTimer = 0;

        private int slowTimer = 0;
        private int spawnChance = 5;

        private int dangerLevel = 0;
        private int maxDanger = 100;

        private int smallHit = 25;
        private int bigHit = 50;

        private Label dangerLabel = null!;

        private int score = 0;
        private Label scoreLabel = null!;

        private int gameTick = 0;
        private List<PictureBox> obstacles = new List<PictureBox>();
        private Random random = new Random();

        private int obstacleSpeed = 5;
        private Timer gameTimer = null!;
        private PictureBox player = null!;

        private int playerLane = 1;
        private int laneWidth = 100;

        public GameForm()
        {
            Init();
            InitGame();
        }

        // Initialization
        private void Init()
        {
            Text = "Run From RKN";
            Width = 400;
            Height = 600;
            BackgroundImage = Image.FromFile("Assets/background.png");
            BackgroundImageLayout = ImageLayout.Stretch;
            DoubleBuffered = true;

            KeyDown += OnKeyDown;
        }

        private void InitGame()
        {
            dangerLabel = new Label();
            dangerLabel.ForeColor = Color.Red;
            dangerLabel.BackColor = Color.Transparent;
            dangerLabel.Font = new Font("Arial", 12, FontStyle.Bold);
            dangerLabel.Location = new Point(10, 40);
            dangerLabel.AutoSize = true;

            Controls.Add(dangerLabel);

            scoreLabel = new Label();
            scoreLabel.ForeColor = Color.White;
            scoreLabel.BackColor = Color.Transparent;
            scoreLabel.Font = new Font("Arial", 14, FontStyle.Bold);
            scoreLabel.Location = new Point(10, 10);
            scoreLabel.AutoSize = true;

            Controls.Add(scoreLabel);

            playerNormalImage = Image.FromFile("Assets/good1.png");
            playerHitImage = Image.FromFile("Assets/bad1.png");
            playerVpnImage = Image.FromFile("Assets/heal1.png");

            player = new PictureBox();
            player.Width = 50;
            player.Height = 50;
            player.SizeMode = PictureBoxSizeMode.StretchImage;
            player.Image = playerNormalImage;

            Controls.Add(player);
            UpdatePlayerPosition();

            gameTimer = new Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            rkn = new PictureBox();
            rkn.Width = 240;
            rkn.Height = 140;
            rkn.SizeMode = PictureBoxSizeMode.StretchImage;
            rkn.Image = Image.FromFile("Assets/rkn.png");

            Controls.Add(rkn);

            rknY = ClientSize.Height;
            UpdateRknPosition();
        }

        private void UpdateRknPosition()
        {
            var x = ClientSize.Width / 2 - rkn.Width / 2;
            rkn.Location = new Point(x, rknY);
        }

        // Game Loop
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
            var minDistance = player.Top + player.Height;

            var t = dangerLevel / 100f;
            var targetY = (int)(maxDistance - (maxDistance - minDistance) * t);

            rknY += (int)((targetY - rknY) * 0.2f);

            UpdateRknPosition();
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

            if (dangerLevel >= 100)
            {
                GameOver();
            }

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

        // Obstacles
        private void MoveObstacles()
        {
            for (int i = obstacles.Count - 1; i >= 0; i--)
            {
                obstacles[i].Top += currentSpeed;

                if (obstacles[i].Top > Height)
                {
                    Controls.Remove(obstacles[i]);
                    obstacles.RemoveAt(i);
                }
            }
        }

        private void SpawnObstacle()
        {
            var dynamicDistance = baseSpeed * 15;

            if (random.Next(0, 100) < spawnChance)
            {
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.Top < dynamicDistance)
                    {
                        return;
                    }
                }

                var maxObstacles = gameTick > 1000 ? 2 : 1;
                var obstacleCount = random.Next(1, maxObstacles + 1);
                var lanes = new List<int> { 0, 1, 2 };

                for (int i = 0; i < obstacleCount; i++)
                {
                    if (lanes.Count <= 1)
                    {
                        break;
                    }

                    int index = random.Next(lanes.Count);
                    int lane = lanes[index];
                    lanes.RemoveAt(index);

                    CreateObstacle(lane);
                }
            }
        }

        private void CreateObstacle(int lane)
        {
            PictureBox obstacle = new PictureBox();
            obstacle.Width = 50;
            obstacle.Height = 50;
            obstacle.SizeMode = PictureBoxSizeMode.StretchImage;

            bool isHard = random.Next(0, 100) < 30;

            if (isHard)
            {
                obstacle.Image = Image.FromFile("Assets/hacker.png");
                obstacle.Tag = "hard";
            }
            else
            {
                obstacle.Image = Image.FromFile("Assets/DDOS.png");
                obstacle.Tag = "normal";
            }

            var x = 50 + lane * laneWidth;
            obstacle.Location = new Point(x, -50);

            obstacles.Add(obstacle);
            Controls.Add(obstacle);
        }

        // Bonuses
        private void MoveBonuses()
        {
            for (int i = bonuses.Count - 1; i >= 0; i--)
            {
                bonuses[i].Top += currentSpeed;

                if (bonuses[i].Top > Height)
                {
                    Controls.Remove(bonuses[i]);
                    bonuses.RemoveAt(i);
                }
            }
        }

        private void SpawnBonus()
        {
            if (bonusCooldown > 0)
            {
                return;
            }

            if (random.Next(0, 100) < 2)
            {
                var lane = random.Next(0, 3);

                var bonus = new PictureBox();
                bonus.Width = 40;
                bonus.Height = 40;
                bonus.SizeMode = PictureBoxSizeMode.StretchImage;

                var type = random.Next(0, 3);

                if (type == 0)
                {
                    bonus.Image = Image.FromFile("Assets/vpn.png");
                    bonus.Tag = "vpn";
                }
                else if (type == 1)
                {
                    bonus.Image = Image.FromFile("Assets/proxy.png");
                    bonus.Tag = "proxy_heal";
                }
                else
                {
                    bonus.Image = Image.FromFile("Assets/block-change.png");
                    bonus.Tag = "slow";
                }

                var x = 50 + lane * laneWidth;
                bonus.Location = new Point(x, -40);

                bonuses.Add(bonus);
                Controls.Add(bonus);

                bonusCooldown = 120;
            }
        }

        // Player
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
            player.Location = new Point(x, y);
        }
    }
}