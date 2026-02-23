namespace psecsapi.api.models.Research
{
    public class ActiveProjectModel
    {
        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public int CurrentPoints { get; set; }
        public int RequiredPoints { get; set; }
        public int AllocationPercent { get; set; }
        public double ProgressPercent => RequiredPoints > 0 ? (CurrentPoints * 100.0) / RequiredPoints : 0;
        public DateTime NextTickAt { get; set; }
        public DateTime StartedAt { get; set; }
    }
}
