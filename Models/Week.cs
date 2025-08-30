using BussinessCupApi.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class Week
{
    public int WeekID { get; set; }
    public int LeagueID { get; set; }
    public int WeekNumber { get; set; }
    public int SeasonID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCompleted { get; set; }
    public string WeekName { get; set; }
    public string WeekStatus { get; set; } // "League" veya "Playoff" değerlerini alacak

    [ForeignKey("LeagueID")]
    public virtual League League { get; set; }
    [ForeignKey("SeasonID")]
    public virtual Season Season{ get; set; }
    public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
} 