using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace RunnerGame
{
    public partial class GameForm : Form
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
    }
}
