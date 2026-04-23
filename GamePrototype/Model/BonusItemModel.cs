namespace RunnerGame.Model
{
    public sealed class BonusItemModel
    {
        public int Lane { get; set; }
        public float Depth { get; set; }
        public BonusType Kind { get; set; }
    }

    public enum BonusType
    {
        Vpn,
        ProxyHeal,
        Slow
    }
}
