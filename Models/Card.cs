using BussinessCupApi.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class Card
{
    public int CardID { get; set; }
    public int PlayerID { get; set; }
    public int MatchID { get; set; }
    public CardType CardType { get; set; }
    public int Minute { get; set; }
    public int MatchBan { get; set; }

    [ForeignKey("PlayerID")]
    public virtual Player Player { get; set; }
    [ForeignKey("MatchID")]
    public virtual Match Match { get; set; }
}

public enum CardType
{
    Yellow = 1,
    Red = 2
} 