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
        private readonly Label selectedRunnerLabel;
        private readonly Label selectedSkinLabel;
        private readonly Label menuDescriptionLabel;
        private readonly Button runnerButton;
        private readonly Button skinButton;
        private readonly Button startButton;
        private GameRenderer? renderer;
        private GameState? state;
        private RunnerType selectedRunner = RunnerType.Classic;
        private SkinType selectedSkin = SkinType.Default;

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

            menuOverlay = CreateMenuOverlay(
                out selectedRunnerLabel,
                out selectedSkinLabel,
                out menuDescriptionLabel,
                out runnerButton,
                out skinButton,
                out startButton);

            Controls.Add(menuOverlay);
            menuOverlay.BringToFront();

            runnerButton.Click += HandleRunnerButtonClick;
            skinButton.Click += HandleSkinButtonClick;
            startButton.Click += HandleStartButtonClick;

            UpdateMenuSelectionUi();
        }

        public event EventHandler? StartGameRequested;
        public event EventHandler? FrameAdvanced;
        public event EventHandler? MoveLeftRequested;
        public event EventHandler? MoveRightRequested;
        public event EventHandler? JumpRequested;

        public RunnerType SelectedRunner => selectedRunner;
        public SkinType SelectedSkin => selectedSkin;

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

        private void HandleRunnerButtonClick(object? sender, EventArgs e)
        {
            SelectNextRunner();
        }

        private void HandleSkinButtonClick(object? sender, EventArgs e)
        {
            using var dialog = new SkinSelectionForm(selectedSkin);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                selectedSkin = dialog.SelectedSkin;
                UpdateMenuSelectionUi();
            }
        }

        private void HandleKeyDown(object? sender, KeyEventArgs e)
        {
            if (menuOverlay.Visible)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    StartGameRequested?.Invoke(this, EventArgs.Empty);
                }
                else if (e.KeyCode == Keys.Left)
                {
                    SelectPreviousRunner();
                }
                else if (e.KeyCode == Keys.Right)
                {
                    SelectNextRunner();
                }
                else if (e.KeyCode == Keys.S)
                {
                    HandleSkinButtonClick(this, EventArgs.Empty);
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

        private static Panel CreateMenuOverlay(
            out Label selectedRunnerLabel,
            out Label selectedSkinLabel,
            out Label menuDescriptionLabel,
            out Button runnerButton,
            out Button skinButton,
            out Button startButton)
        {
            var overlay = new MenuPanel
            {
                Dock = DockStyle.Fill,
                Name = "MenuOverlay",
                TabStop = true
            };

            var shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(0, 38, 0, 34)
            };
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 28f));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 26f));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 16f));

            var titleWrap = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var titleLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 88,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 33f, FontStyle.Bold),
                Text = "RUN FROM RKN",
                TextAlign = ContentAlignment.BottomCenter
            };

            var subtitleLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(205, 214, 228, 236),
                Font = new Font("Segoe UI", 14f, FontStyle.Regular),
                Text = "Псевдо-3D раннер про увороты, прыжки и выживание",
                TextAlign = ContentAlignment.TopCenter
            };

            titleWrap.Controls.Add(subtitleLabel);
            titleWrap.Controls.Add(titleLabel);

            var centerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            startButton = CreateMenuButton("Начать игру");
            startButton.Width = 320;
            startButton.Height = 82;
            startButton.BackColor = Color.FromArgb(42, 191, 186);
            startButton.Anchor = AnchorStyles.None;
            var centeredStartButton = startButton;

            selectedRunnerLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 19f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            selectedSkinLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(125, 235, 220),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            menuDescriptionLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 66,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(205, 214, 228, 236),
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                TextAlign = ContentAlignment.TopCenter
            };

            var bottomCard = new Panel
            {
                Anchor = AnchorStyles.None,
                Width = 520,
                Height = 156,
                BackColor = Color.Transparent
            };

            runnerButton = CreateMenuButton("Персонаж");
            runnerButton.Width = 220;
            runnerButton.Height = 56;
            runnerButton.Location = new Point(28, 86);

            skinButton = CreateMenuButton("Скин");
            skinButton.Width = 220;
            skinButton.Height = 56;
            skinButton.Location = new Point(bottomCard.Width - skinButton.Width - 28, 86);

            bottomCard.Controls.Add(skinButton);
            bottomCard.Controls.Add(runnerButton);
            bottomCard.Controls.Add(menuDescriptionLabel);
            bottomCard.Controls.Add(selectedSkinLabel);
            bottomCard.Controls.Add(selectedRunnerLabel);
            bottomCard.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var cardRect = new RectangleF(8, 8, bottomCard.Width - 16, bottomCard.Height - 16);
                using var brush = new LinearGradientBrush(cardRect, Color.FromArgb(96, 18, 30, 40), Color.FromArgb(146, 10, 16, 24), 90f);
                using var borderPen = new Pen(Color.FromArgb(90, 140, 255, 235), 1.6f);
                e.Graphics.FillRoundedRectangle(brush, cardRect, 26f);
                e.Graphics.DrawRoundedRectangle(borderPen, cardRect, 26f);
            };

            centerPanel.Resize += (_, _) =>
            {
                centeredStartButton.Left = centerPanel.Width / 2 - centeredStartButton.Width / 2;
                centeredStartButton.Top = centerPanel.Height / 2 - centeredStartButton.Height / 2 - 18;
            };
            centerPanel.Controls.Add(startButton);

            var optionsHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            optionsHost.Controls.Add(bottomCard);
            optionsHost.Resize += (_, _) =>
            {
                bottomCard.Left = optionsHost.Width / 2 - bottomCard.Width / 2;
                bottomCard.Top = Math.Max(0, optionsHost.Height / 2 - bottomCard.Height / 2 - 6);
            };

            var hintLabel = new Label
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(190, 214, 228, 236),
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                Text = "← → персонаж    S выбор скина    Enter старт    В игре: ← → и Space",
                TextAlign = ContentAlignment.MiddleCenter
            };

            shell.Controls.Add(titleWrap, 0, 0);
            shell.Controls.Add(centerPanel, 0, 1);
            shell.Controls.Add(optionsHost, 0, 2);
            shell.Controls.Add(hintLabel, 0, 3);

            overlay.Controls.Add(shell);
            return overlay;
        }

        private static Button CreateMenuButton(string text)
        {
            var button = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 176, 170),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold),
                Text = text,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private void SelectPreviousRunner()
        {
            selectedRunner = selectedRunner switch
            {
                RunnerType.Classic => RunnerType.Acrobat,
                RunnerType.Sprinter => RunnerType.Classic,
                _ => RunnerType.Sprinter
            };
            UpdateMenuSelectionUi();
        }

        private void SelectNextRunner()
        {
            selectedRunner = selectedRunner switch
            {
                RunnerType.Classic => RunnerType.Sprinter,
                RunnerType.Sprinter => RunnerType.Acrobat,
                _ => RunnerType.Classic
            };
            UpdateMenuSelectionUi();
        }

        private void UpdateMenuSelectionUi()
        {
            selectedRunnerLabel.Text = $"Персонаж: {GetRunnerName(selectedRunner)}";
            selectedSkinLabel.Text = $"Скин: {GetSkinName(selectedSkin)}";
            menuDescriptionLabel.Text = selectedRunner switch
            {
                RunnerType.Sprinter => "Быстрее разгоняется, резче меняет полосу и сильнее раскрывается в adrenaline-режиме.",
                RunnerType.Acrobat => "Прыгает выше, легче перелетает низкие преграды и сильнее награждается за чистые трюки.",
                _ => "Сбалансированный персонаж с ровным ритмом и чуть более щадящими защитными бонусами."
            };

            runnerButton.Text = $"Персонаж: {GetRunnerName(selectedRunner)}";
            skinButton.Text = $"Скин: {GetSkinName(selectedSkin)}";
        }

        private static string GetRunnerName(RunnerType runnerType)
        {
            return runnerType switch
            {
                RunnerType.Sprinter => "Sprinter",
                RunnerType.Acrobat => "Acrobat",
                _ => "Classic"
            };
        }

        private static string GetSkinName(SkinType skinType)
        {
            return skinType switch
            {
                SkinType.Neon => "Neon",
                SkinType.Crimson => "Crimson",
                SkinType.Ghost => "Ghost",
                _ => "Default"
            };
        }

        private sealed class MenuPanel : Panel
        {
            private Bitmap? cachedBackground;
            private Size cachedSize;

            public MenuPanel()
            {
                DoubleBuffered = true;
                SetStyle(ControlStyles.Selectable, true);
                Resize += (_, _) => RebuildBackground();
            }

            protected override void OnCreateControl()
            {
                base.OnCreateControl();
                RebuildBackground();
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                if (cachedBackground is not null)
                {
                    e.Graphics.DrawImageUnscaled(cachedBackground, Point.Empty);
                    return;
                }

                base.OnPaintBackground(e);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    cachedBackground?.Dispose();
                }

                base.Dispose(disposing);
            }

            private void RebuildBackground()
            {
                if (Width <= 0 || Height <= 0)
                {
                    return;
                }

                if (cachedSize == ClientSize && cachedBackground is not null)
                {
                    return;
                }

                cachedBackground?.Dispose();
                cachedBackground = new Bitmap(Width, Height);
                cachedSize = ClientSize;

                using var g = Graphics.FromImage(cachedBackground);
                g.SmoothingMode = SmoothingMode.None;

                using var gradient = new LinearGradientBrush(
                    ClientRectangle,
                    Color.FromArgb(18, 26, 34),
                    Color.FromArgb(9, 13, 18),
                    90f);
                g.FillRectangle(gradient, ClientRectangle);

                using var accentBrush = new SolidBrush(Color.FromArgb(30, 82, 205, 190));
                g.FillEllipse(accentBrush, Width / 2 - 170, Height / 2 - 90, 340, 180);

                using var linePen = new Pen(Color.FromArgb(18, 230, 238, 242), 1f);
                for (int y = 0; y < Height; y += 40)
                {
                    g.DrawLine(linePen, 0, y, Width, y);
                }

                for (int x = 0; x < Width; x += 40)
                {
                    g.DrawLine(linePen, x, 0, x, Height);
                }

                Invalidate();
            }
        }

        private sealed class SkinSelectionForm : Form
        {
            private readonly ListBox listBox;

            public SkinSelectionForm(SkinType currentSkin)
            {
                Text = "Выбор скина";
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                ClientSize = new Size(320, 260);
                MaximizeBox = false;
                MinimizeBox = false;
                BackColor = Color.FromArgb(16, 22, 28);
                ForeColor = Color.White;

                listBox = new ListBox
                {
                    Dock = DockStyle.Top,
                    Height = 170,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.FromArgb(22, 30, 38),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11f, FontStyle.Regular)
                };

                listBox.Items.Add(new SkinListItem(SkinType.Default, "Default", "Базовый светлый стиль"));
                listBox.Items.Add(new SkinListItem(SkinType.Neon, "Neon", "Яркий бирюзовый акцент"));
                listBox.Items.Add(new SkinListItem(SkinType.Crimson, "Crimson", "Красно-оранжевый боевой стиль"));
                listBox.Items.Add(new SkinListItem(SkinType.Ghost, "Ghost", "Холодный и полупрозрачный вайб"));

                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    if (((SkinListItem)listBox.Items[i]!).Skin == currentSkin)
                    {
                        listBox.SelectedIndex = i;
                        break;
                    }
                }

                var buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 62,
                    BackColor = Color.Transparent
                };

                var okButton = new Button
                {
                    Text = "Выбрать",
                    Width = 120,
                    Height = 40,
                    Left = 32,
                    Top = 10,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(42, 191, 186),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold)
                };
                okButton.FlatAppearance.BorderSize = 0;
                okButton.Click += (_, _) =>
                {
                    if (listBox.SelectedItem is SkinListItem item)
                    {
                        SelectedSkin = item.Skin;
                    }

                    DialogResult = DialogResult.OK;
                    Close();
                };

                var cancelButton = new Button
                {
                    Text = "Отмена",
                    Width = 120,
                    Height = 40,
                    Left = 168,
                    Top = 10,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(52, 62, 74),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold)
                };
                cancelButton.FlatAppearance.BorderSize = 0;
                cancelButton.Click += (_, _) =>
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                };

                buttonPanel.Controls.Add(okButton);
                buttonPanel.Controls.Add(cancelButton);

                Controls.Add(buttonPanel);
                Controls.Add(listBox);
            }

            public SkinType SelectedSkin { get; private set; }

            private sealed class SkinListItem
            {
                public SkinListItem(SkinType skin, string name, string description)
                {
                    Skin = skin;
                    Name = name;
                    Description = description;
                }

                public SkinType Skin { get; }
                public string Name { get; }
                public string Description { get; }

                public override string ToString()
                {
                    return $"{Name} - {Description}";
                }
            }
        }
    }
}
