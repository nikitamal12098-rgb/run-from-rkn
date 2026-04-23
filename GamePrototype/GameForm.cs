using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RunnerGame.Model;
using RunnerGame.Presentation;
using RunnerGame.Rendering;
using Timer = System.Windows.Forms.Timer;

namespace RunnerGame
{
    public sealed class GameForm : Form, IGameView
    {
        private readonly Timer gameTimer;
        private readonly Panel menuOverlay;
        private Button? startButton;
        private GameRenderer? renderer;
        private GameState? state;

        public GameForm()
        {
            Text = "Run From RKN";
            ClientSize = new Size(1100, 720);
            DoubleBuffered = true;
            KeyPreview = true;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 620);
            BackColor = Color.Black;

            KeyDown += HandleKeyDown;

            gameTimer = new Timer
            {
                Interval = 16
            };
            gameTimer.Tick += HandleTimerTick;

            menuOverlay = CreateMenuOverlay();
            Controls.Add(menuOverlay);
            menuOverlay.BringToFront();

            startButton!.Click += HandleStartButtonClick;
        }

        public event EventHandler? StartGameRequested;
        public event EventHandler? FrameAdvanced;
        public event EventHandler? MoveLeftRequested;
        public event EventHandler? MoveRightRequested;
        public event EventHandler? JumpRequested;

        public void Attach(GameState gameState, GameRenderer gameRenderer)
        {
            state = gameState;
            renderer = gameRenderer;
        }

        public void ShowMainMenu()
        {
            menuOverlay.Visible = true;
            menuOverlay.BringToFront();
            menuOverlay.Focus();
        }

        public void HideMainMenu()
        {
            menuOverlay.Visible = false;
        }

        public void Start()
        {
            gameTimer.Start();
        }

        public void Stop()
        {
            gameTimer.Stop();
        }

        public void RequestSceneRefresh()
        {
            Invalidate();
        }

        public void ShowGameOver(GameState gameState)
        {
            MessageBox.Show(
                $"Тебя поймали РКН 😈{Environment.NewLine}Счёт: {gameState.Score}",
                "Game Over",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (state is null || renderer is null)
            {
                return;
            }

            renderer.Render(e.Graphics, ClientSize, state);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                gameTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void HandleTimerTick(object? sender, EventArgs e)
        {
            FrameAdvanced?.Invoke(this, EventArgs.Empty);
        }

        private void HandleStartButtonClick(object? sender, EventArgs e)
        {
            StartGameRequested?.Invoke(this, EventArgs.Empty);
        }

        private void HandleKeyDown(object? sender, KeyEventArgs e)
        {
            if (menuOverlay.Visible)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    StartGameRequested?.Invoke(this, EventArgs.Empty);
                }

                return;
            }

            if (e.KeyCode == Keys.Left)
            {
                MoveLeftRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Right)
            {
                MoveRightRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Up)
            {
                JumpRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private Panel CreateMenuOverlay()
        {
            var overlay = new MenuPanel
            {
                Dock = DockStyle.Fill,
                Name = "MenuOverlay",
                TabStop = true
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 5
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 15f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 18f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 22f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 18f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 27f));

            var titleLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 30f, FontStyle.Bold),
                Text = "RUN FROM RKN",
                TextAlign = ContentAlignment.BottomCenter
            };

            var subtitleLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(210, 220, 232, 240),
                Font = new Font("Segoe UI", 13f, FontStyle.Regular),
                Text = "Псевдо-3D раннер про увороты, прыжки и выживание",
                TextAlign = ContentAlignment.TopCenter
            };

            startButton = new Button
            {
                Name = "StartButton",
                Anchor = AnchorStyles.None,
                Width = 270,
                Height = 72,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(28, 200, 185),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 16f, FontStyle.Bold),
                Text = "Начать игру",
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 12, 0, 0),
                TabStop = false
            };
            startButton.FlatAppearance.BorderSize = 0;

            var hintLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(195, 220, 232, 240),
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                Text = "← → смена полосы   Space прыжок   Enter старт / кнопка",
                TextAlign = ContentAlignment.TopCenter
            };

            layout.Controls.Add(titleLabel, 0, 1);
            layout.Controls.Add(subtitleLabel, 0, 2);
            layout.Controls.Add(startButton, 0, 3);
            layout.Controls.Add(hintLabel, 0, 4);

            overlay.Controls.Add(layout);
            return overlay;
        }

        private sealed class MenuPanel : Panel
        {
            public MenuPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.Selectable, true);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var gradient = new LinearGradientBrush(
                    ClientRectangle,
                    Color.FromArgb(18, 28, 38),
                    Color.FromArgb(10, 14, 18),
                    90f);
                g.FillRectangle(gradient, ClientRectangle);

                using var glowBrush = new PathGradientBrush(new[]
                {
                    new Point(ClientRectangle.Width / 2, ClientRectangle.Height / 3),
                    new Point((int)(ClientRectangle.Width * 0.82f), (int)(ClientRectangle.Height * 0.52f)),
                    new Point((int)(ClientRectangle.Width * 0.22f), (int)(ClientRectangle.Height * 0.74f))
                })
                {
                    CenterColor = Color.FromArgb(70, 80, 180, 170),
                    SurroundColors = new[]
                    {
                        Color.FromArgb(0, 80, 180, 170),
                        Color.FromArgb(0, 80, 180, 170),
                        Color.FromArgb(0, 80, 180, 170)
                    }
                };
                g.FillRectangle(glowBrush, ClientRectangle);

                using var linePen = new Pen(Color.FromArgb(26, 210, 230, 235), 1f);
                for (int y = 0; y < Height; y += 34)
                {
                    g.DrawLine(linePen, 0, y, Width, y);
                }

                for (int x = 0; x < Width; x += 42)
                {
                    g.DrawLine(linePen, x, 0, x, Height);
                }
            }
        }
    }
}
