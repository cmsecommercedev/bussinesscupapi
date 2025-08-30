using BussinessCupApi.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class Goal
{
    public int GoalID { get; set; }
    
    public int MatchID { get; set; }
    public int TeamID { get; set; }
    public int PlayerID { get; set; }
    public int? AssistPlayerID { get; set; }
    public int Minute { get; set; }
    public bool IsPenalty { get; set; }
    public bool IsOwnGoal { get; set; }

    // Navigation properties
    [ForeignKey("MatchID")]
    public virtual Match Match { get; set; }
    
    [ForeignKey("TeamID")]
    public virtual Team Team { get; set; }
    
    [ForeignKey("PlayerID")]
    public virtual Player Player { get; set; }
    
    public virtual Player AssistPlayer { get; set; }
} 