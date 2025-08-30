using BussinessCupApi.Models.UserPlayerTypes;
using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.Models
{
    public class Player
    {
        public int PlayerID { get; set; }

        [Required(ErrorMessage = "Ad zorunludur")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Soyad zorunludur")]
        public string LastName { get; set; }

        public string? Position { get; set; }
        public int? Number { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? Icon { get; set; }
        public string? IdentityNumber { get; set; }
        public bool isArchived { get; set; }
        public PlayerType? PlayerType { get; set; } // Season or Guest
        public string? Height { get; set; } // Boy (cm)
        public string? Weight { get; set; } // Kilo (kg)
        public string? PreferredFoot { get; set; } // Sağ/Sol ayak
        // Navigation properties
        [Required(ErrorMessage = "Takım seçimi zorunludur")]
        public int TeamID { get; set; }
        public string? UserId { get; set; } // Nullable UserId
        public User User { get; set; } // Navigation property
        public Team Team { get; set; }
        public string? FrontIdentityImage { get; set; }

        public string? BackIdentityImage { get; set; }

        public int? PlayerValue { get; set; } = 500;

        public DateTime? SubscriptionExpireDate { get; set; }
        public bool? isSubscribed { get; set; }
        public bool? LicensedPlayer { get; set; }
        public ICollection<Goal> Goals { get; set; } = new List<Goal>();
        public ICollection<Goal> Assists { get; set; } = new List<Goal>();
        public ICollection<Card> Cards { get; set; } = new List<Card>();
    }
}
