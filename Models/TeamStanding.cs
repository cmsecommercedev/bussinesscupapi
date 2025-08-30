using BussinessCupApi.Models;

public class TeamStanding
{
    public int TeamID { get; set; }
    public string TeamName { get; set; }
    public string Manager { get; set; }
    public List<Player> Players { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }

    // Hesaplanan özellikler
    public int Points => Won * 3 + Drawn;
    public int GoalDifference => GoalsFor - GoalsAgainst;
} 