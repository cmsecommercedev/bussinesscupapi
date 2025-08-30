using BussinessCupApi.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class MatchSquad
{
    public int MatchSquadID { get; set; }
    public int MatchID { get; set; }
    public int PlayerID { get; set; }
    public int TeamID { get; set; }
    public bool IsStarting11 { get; set; }
    public bool IsSubstitute { get; set; }
    public int? ShirtNumber { get; set; }
    public float? TopPosition { get; set; } // Decimal veya Float kullanmak daha doğru
    public float? LeftPosition { get; set; }
    [ForeignKey("MatchID")]

    public virtual Match Match { get; set; }
    [ForeignKey("PlayerID")]

    public virtual Player Player { get; set; }
    [ForeignKey("TeamID")]

    public virtual Team Team { get; set; }
} 