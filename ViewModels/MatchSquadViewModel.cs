using BussinessCupApi.Models;

public class MatchSquadViewModel
{
    public int MatchId { get; set; }
    public Team HomeTeam { get; set; }
    public Team AwayTeam { get; set; }
    public List<PlayerSquadViewModel> HomeSquad { get; set; }
    public List<PlayerSquadViewModel> AwaySquad { get; set; }
}

public class SaveMatchSquadViewModel
{
    public int MatchId { get; set; }
    public List<PlayerSquadViewModel> HomeSquad { get; set; }
    public List<PlayerSquadViewModel> AwaySquad { get; set; }
}

public class PlayerSquadViewModel
{
    public int PlayerId { get; set; }
    public int TeamId { get; set; }
    public int ShirtNumber { get; set; }
    public bool IsStarting11 { get; set; }
    public bool IsSubstitute { get; set; }
} 