using Microsoft.AspNetCore.Identity;
using BussinessCupApi.Models.UserPlayerTypes;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class User : IdentityUser
    {
        public string? MacID { get; set; }
        public string? OS { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? ExternalID { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool isSubscribed { get; set; }

        public UserType UserType { get; set; } // Enum olarak değiştirildi

        // Yeni alanlar 
        public string? UserRole { get; set; }
        public int? CityID { get; set; }
        [Required]
        [StringLength(50)]
        public string? UserKey { get; set; }
    }

    // DTO'lar için yeni sınıflar
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [MinLength(6)]
        public string? Password { get; set; }

        [Required]
        public string? Firstname { get; set; }

        [Required]
        public string? Lastname { get; set; }
        public string? MacID { get; set; }
        public string? OS { get; set; }
        public IFormFile? ProfilePicture { get; set; }


        public string? ExternalID { get; set; }
         public int? CityID { get; set; } // CityID eklendi
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }
        public string? MacID { get; set; }
        public string? OS { get; set; }
              public string? ExternalID { get; set; }
    }

    public class UpdateUserDto
    {
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public int? CityID { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}