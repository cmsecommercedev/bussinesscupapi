using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using BussinessCupApi.ViewModels;
using BussinessCupApi.Models.UserPlayerTypes;
using BussinessCupApi.Models.Dtos;
using Microsoft.OpenApi.Extensions;
using BussinessCupApi.Managers;
using Microsoft.AspNetCore.Authentication;

namespace BussinessCupApi.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly CustomUserManager _customUserManager;
        private readonly ILogger<AccountController> _logger;
        private readonly EmailServiceManager _emailServiceManager;

        public AccountController(ApplicationDbContext context, IConfiguration configuration, CustomUserManager customUserManager, ILogger<AccountController> logger, EmailServiceManager emailServiceManager)
        {
            _context = context;
            _configuration = configuration;
            _customUserManager = customUserManager;
            _logger = logger;
            _emailServiceManager = emailServiceManager;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Geçersiz email veya şifre");
                    return View(model);
                }

                var result = await _customUserManager.SignInUserAsync(model.Email, model.Password, false);

                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Geçersiz email veya şifre");
                    return View(model);
                }

                var roles = await _customUserManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();


                return role switch
                {
                    "Admin" => RedirectToAction("Dashboard", "Admin"),
                    "Captain" => RedirectToAction("Dashboard", "Captain"),
                    "CityAdmin" => RedirectToAction("Dashboard", "CityAdmin"),
                    _ => throw new UnauthorizedAccessException("Tanımsız rol ile giriş yapıldı.")
                };
            }
            catch (Exception ex)
            {
                // Hata logla
                _logger.LogError(ex, "Login işlemi sırasında hata oluştu");
                ModelState.AddModelError("", "Giriş işlemi sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(); // Oturumu sonlandır

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    // Güvenlik için kullanıcıya email'in gönderildiğini söyle
                    TempData["SuccessMessage"] = "Şifre sıfırlama bağlantısı email adresinize gönderildi.";
                    return RedirectToAction("Login");
                }

                // Şifre sıfırlama token'ı oluştur
                var token = await _customUserManager.GeneratePasswordResetTokenAsync(user);
                
                // Email gönder
                await SendPasswordResetEmailAsync(user.Email, token);

                TempData["SuccessMessage"] = "Şifre sıfırlama bağlantısı email adresinize gönderildi.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ForgotPassword işlemi sırasında hata oluştu");
                ModelState.AddModelError("", "İşlem sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Geçersiz şifre sıfırlama bağlantısı.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction("Login");
                }

                var result = await _customUserManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                TempData["SuccessMessage"] = "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPassword işlemi sırasında hata oluştu");
                ModelState.AddModelError("", "İşlem sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }
        }

        private async Task SendPasswordResetEmailAsync(string email, string token)
        {
            try
            {
                var resetLink = Url.Action("ResetPassword", "Account", 
                    new { email = email, token = token }, 
                    Request.Scheme, Request.Host.Value);

                // Kullanıcı adını al
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                var userName = user?.UserName ?? "Kullanıcı";

                // EmailServiceManager kullanarak email gönder
                await _emailServiceManager.SendPasswordResetEmailAsync(email, userName, resetLink);

                _logger.LogInformation($"Password reset email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderme sırasında hata oluştu");
                throw;
            }
        }
    }
}