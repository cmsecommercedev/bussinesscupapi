namespace BussinessCupApi.Models.Api
{
    public class MatchSquadPlayerDto
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public bool IsStarting11 { get; set; }
        public bool IsSubstitute { get; set; }
        public int? ShirtNumber { get; set; }
    }

    public class MatchSquadTeamDto
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public string TeamLogo { get; set; }
        public List<MatchSquadPlayerDto> Players { get; set; }
    }
    public class MatchSquadsResponseDto
    {
        public MatchSquadTeamDto HomeTeam { get; set; }
        public MatchSquadTeamDto AwayTeam { get; set; }
    }
}
