using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace RunnerGame
{
    public partial class GameForm
    {
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
            rkn.Width = 60;
            rkn.Height = 60;
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
    }
}
