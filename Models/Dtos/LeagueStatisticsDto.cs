using BussinessCupApi.Models.UserPlayerTypes;
using Controllers.Api;

public class LeagueStatisticsDto
{
    public int? GroupId { get; set; }
    public List<PlayerStatsDto> TopScorers { get; set; }
    public List<PlayerStatsDto> TopAssists { get; set; }
    public List<TeamStatsDto> TeamStats { get; set; }
    public int TotalGoals { get; set; }
    public int TotalAssists { get; set; }
    public int PenaltyGoals { get; set; }
    public int OwnGoals { get; set; }
    public string PlayerIcon { get; set; }
    public double AverageGoalsPerMatch { get; set; }
}
public class LeagueWithAllWeeksDto
{
    public int? GroupId { get; set; }
    public int LeagueID { get; set; }
    public string Name { get; set; } 
    public List<SeasonDto> Seasons { get; set; }
}

public class PlayerSeasonStatisticsDto
{
    public int? GroupId { get; set; }
    public int PlayerID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int TeamID { get; set; }
    public string TeamName { get; set; }

    public string PlayerIcon { get; set; }
    public PlayerType? PlayerType { get; set; }
    public string Position { get; set; }
    public int? Number { get; set; }
    public int Matches { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int PenaltyGoals { get; set; }
    public int OwnGoals { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int ManOfTheMatch { get; set; } 
}
 