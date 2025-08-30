public class UpdateScoreModel
{
    public int MatchId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public List<GoalDetailModel> Goals { get; set; } = new List<GoalDetailModel>();
}

public class GoalDetailModel
{
    public int? GoalID { get; set; }
    public int PlayerID { get; set; }
    public int TeamID { get; set; }
    public int Minute { get; set; }
    public bool IsOwnGoal { get; set; }
    public bool IsPenalty { get; set; }
} 