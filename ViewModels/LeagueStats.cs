using BussinessCupApi.Models;

public class LeagueStats
{
    public int LeagueID { get; set; }
    public string LeagueName { get; set; }
    public string Season { get; set; }
    public List<LeagueStandingStats> TeamStandings { get; set; } = new List<LeagueStandingStats>();
}

public class LeagueStandingStats
{
    public int TeamID { get; set; }
    public string TeamName { get; set; }
    public string Manager { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    
    public int Points 
    { 
        get { return Won * 3 + Drawn; }
    }
    
    public int GoalDifference 
    { 
        get { return GoalsFor - GoalsAgainst; }
    }
    
    public List<Player> Players { get; set; } = new List<Player>();
} 