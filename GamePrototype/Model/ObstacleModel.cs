namespace RunnerGame.Model
{
    public sealed class ObstacleModel
    {
        public int Lane { get; set; }
        public float Depth { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }
        public ObstacleType Type { get; set; }
    }

    public enum ObstacleType
    {
        Low,
        High
    }
}
