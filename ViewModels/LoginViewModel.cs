using System.ComponentModel.DataAnnotations;

namespace BussinessCupApi.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir")]
        public string Password { get; set; }
    }
} 