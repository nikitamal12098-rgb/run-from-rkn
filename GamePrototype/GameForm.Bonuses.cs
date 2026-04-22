using System.Drawing;
using System.Windows.Forms;

namespace RunnerGame
{
    public partial class GameForm
    {
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
    }
}
