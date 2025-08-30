using BussinessCupApi.Models;
using System.ComponentModel.DataAnnotations;

public class UpdateScoreViewModel
{
    public int MatchID { get; set; }
    public int HomeScore { get; set; }
    public string? HomeTeamName { get; set; }
    public string? AwayTeamName { get; set; }
    public int AwayScore { get; set; }
    public bool IsPlayed { get; set; }
    public List<Goal> Goals { get; set; } = new List<Goal>();
    public List<Player> AllPlayers { get; set; } = new List<Player>();
}

public class GoalViewModel
{
    public int MatchID { get; set; }

    [Required(ErrorMessage = "Gol atan oyuncu seçilmelidir")]
    public int ScoredByID { get; set; }

    public int? AssistedByID { get; set; }

    [Required(ErrorMessage = "Dakika girilmelidir")]
    [Range(1, 120, ErrorMessage = "Dakika 1-120 arasında olmalıdır")]
    public int Minute { get; set; }

    public bool IsOwnGoal { get; set; }
    public bool IsPenalty { get; set; }
}

