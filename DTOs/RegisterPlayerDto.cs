using BussinessCupApi.Models.UserPlayerTypes;
using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.DTOs
{

    // Yeni DTO sınıfı
    public class RegisterPlayerDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Firstname { get; set; }

        [Required]
        public string Lastname { get; set; }

        public string? MacID { get; set; }
        public string? OS { get; set; }
        public string? ExternalID { get; set; }

        [Required]
        public int TeamID { get; set; }

        public string? TeamPassword { get; set; }

        public IFormFile? PlayerIcon { get; set; }
        public string? PlayerNationality { get; set; }
        public int? PlayerNumber { get; set; }
        public string? PlayerPosition { get; set; }
        public DateTime? PlayerDateOfBirth { get; set; }
        public string? Height { get; set; } = "";
        public string? Weight { get; set; } = "";
        public string? PreferredFoot { get; set; } = "";
        public string IdentityNumber { get; set; }
    }
}
