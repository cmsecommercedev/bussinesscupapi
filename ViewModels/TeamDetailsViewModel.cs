public class TeamDetailsViewModel
{
    public int TeamID { get; set; }
    public string Name { get; set; }
    public string City { get; set; }
    public string Stadium { get; set; }
    public string Manager { get; set; }
    public bool TeamIsFree{ get; set; }
    public List<PlayerListViewModel> Players { get; set; }
}

public class PlayerListViewModel
{
    public int PlayerID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Position { get; set; }
    public string FrontIDImageUrl { get; set; }
    public string BackIDImageUrl { get; set; }
    public int Number { get; set; }
    public string PlayerType{ get; set; }
    public string? UserRole { get; set; }
    public bool IsArchived { get; set; } 
    public bool? isSubscribed { get; set; } 
    public bool? LicensedPlayer { get; set; } 
    public DateTime? SubscriptionExpireDate { get; set; } 
    public string? Icon { get; set; }  // Base64 string olarak resim

    public string FullName => $"{FirstName} {LastName}";
    public string DefaultIcon => "/images/default-player.png";  // Varsayılan oyuncu resmi
} 