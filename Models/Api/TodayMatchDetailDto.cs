namespace BussinessCupApi.Models.Api
{
    public class TodayMatchDetailDto
    {
        public int MatchID { get; set; }
        public int LeagueID { get; set; }
        public string LeagueName { get; set; }
        public int HomeTeamID { get; set; }
        public string HomeTeamName { get; set; }
        public string HomeTeamLogo { get; set; }
        public int AwayTeamID { get; set; }
        public string AwayTeamName { get; set; }
        public string AwayTeamLogo { get; set; }
        public DateTime MatchDate { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public bool IsPlayed { get; set; }
        public string? MatchURL { get; set; }
        // Gerekirse ek alanlar
    }
}
