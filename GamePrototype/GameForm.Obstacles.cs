using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RunnerGame
{
    public partial class GameForm
    {
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
    }
}
