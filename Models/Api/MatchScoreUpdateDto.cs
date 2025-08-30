namespace BussinessCupApi.Models.Api
{
    public class MatchScoreUpdateDto
    {
        public int MatchID { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public int ScoringTeamID { get; set; }
        public int ScorerPlayerID { get; set; }
        public int? AssistPlayerID { get; set; }
        public int Minute { get; set; }
        public bool IsPenalty { get; set; }
        public bool IsOwnGoal { get; set; }
    }
}
