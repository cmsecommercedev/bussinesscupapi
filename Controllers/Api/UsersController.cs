using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using BussinessCupApi.Models;
using BussinessCupApi.Data;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using BussinessCupApi.Attributes;
using BussinessCupApi.Models.UserPlayerTypes;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using BussinessCupApi.DTOs;
using BussinessCupApi.Managers;
using BussinessCupApi.Models.Api;

namespace BussinessCupApi.Controllers.Api
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _passwordKey;
        private readonly CloudflareR2Manager _r2Manager;
        private readonly CustomUserManager _customUserManager;

        public UsersController(ApplicationDbContext context, IConfiguration configuration, CloudflareR2Manager r2Manager, CustomUserManager customUserManager)
        {
            _context = context;
            _configuration = configuration;
            _passwordKey = _configuration["PasswordKey"] ?? throw new InvalidOperationException("PasswordKey is not configured");
            _r2Manager = r2Manager;
            _customUserManager = customUserManager;

        }

        [HttpPost("register-public")]
        [AllowAnonymous]
        public async Task<ActionResult<User>> RegisterPublicUser(RegisterDto registerDto)
        {
            // Benzersiz UserKey oluştur
            string userKey = GenerateUniqueUserKey();
            string? playerIconPath = null;

            if (registerDto.ProfilePicture != null)
            {
                var key = $"playerimages/{Guid.NewGuid()}{Path.GetExtension(registerDto.ProfilePicture.FileName)}";

                using var stream = registerDto.ProfilePicture.OpenReadStream();

                await _r2Manager.UploadFileAsync(key, stream, registerDto.ProfilePicture.ContentType);

                playerIconPath = _r2Manager.GetFileUrl(key);
            }
            var user = new User
            {
                Email = registerDto.Email,
                UserName = registerDto.Email,
                Firstname = registerDto.Firstname,
                Lastname = registerDto.Lastname,
                UserRole = "Public",
                ExternalID = registerDto.ExternalID,
                MacID = registerDto.MacID,
                OS = registerDto.OS,
                ProfilePictureUrl = playerIconPath, // Yeni eklenen alan
                CityID = registerDto.CityID,
                UserType = UserType.Public, // UserType'ı Public olarak ayarlıyoruz
                UserKey = userKey // Yeni eklenen alan
            };


            var createResult = await _customUserManager.CreateUserAsync(user, registerDto.Password ?? string.Empty, "Public");
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return BadRequest($"Kullanıcı oluşturulamadı: {errors}");
            }

            return Ok(new
            {
                message = "Kayıt başarılı",
                userkey = user.UserKey // Response'a UserKey'i de ekleyelim
            });
        }

        // Benzersiz UserKey oluşturmak için yardımcı metod
        private string GenerateUniqueUserKey()
        {
            // Benzersiz bir key oluştur (örn: USER_20240315_XXXXX)
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            string randomPart = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
            return $"USER_{timestamp}_{randomPart}";
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> Login(LoginDto loginDto)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null)
                return Unauthorized("Geçersiz email veya şifre");

            var result = await _customUserManager.SignInUserAsync(loginDto.Email ?? string.Empty, loginDto.Password ?? string.Empty, false);
            if (!result.Succeeded)
                return Unauthorized("Geçersiz email veya şifre");

            var roles = await _customUserManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? user.UserRole ?? "";

            user.MacID = loginDto.MacID;
            user.OS = loginDto.OS;
            user.ExternalID = loginDto.ExternalID;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Login başarılı",
                userkey = user.UserKey,
                userrole = userRole
            });
        }

        [HttpDelete("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount(string userKey)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Kullanıcı ID'si geçersiz.");

            var user = await _context.Users.FindAsync(userId); // Kullanıcıyı bul
            if (user == null)
                return NotFound("Kullanıcı bulunamadı");


            user.PasswordHash = $"INVALIDATED_{Guid.NewGuid()}";
            user.Email = $"DELETED_{Guid.NewGuid()}";

            _context.Users.Update(user);


            await _context.SaveChangesAsync(); // Değişiklikleri kaydet

            // Mesajı güncelle
            return Ok("Hesabınız devre dışı bırakılmıştır.");
            // veya "Hesabınız devre dışı bırakılmıştır."
        }

        [HttpPost("register-player")]
        [AllowAnonymous]
        public async Task<ActionResult<User>> RegisterPlayer(RegisterPlayerDto registerPlayerDto)
        {
            // Email kontrolü
            if (await _context.Users.AnyAsync(u => u.Email == registerPlayerDto.Email))
                return BadRequest("Bu email adresi zaten kayıtlı.");
             

            // TC Kimlik Numarası benzersiz olmalı
            if (await _context.Players.AnyAsync(p => p.IdentityNumber == registerPlayerDto.IdentityNumber))
                return BadRequest("Bu TC Kimlik Numarası ile kayıtlı bir oyuncu zaten var.");

            // Takım kontrolü
            if (registerPlayerDto.TeamID == 0)
                return BadRequest("Takım ID'si zorunludur.");

            var team = await _context.Teams.FindAsync(registerPlayerDto.TeamID);

            if (team == null)
                return BadRequest("Belirtilen takım bulunamadı.");

            // Takım şifre kontrolü
            if (team.TeamIsFree != true && team.TeamPassword != registerPlayerDto.TeamPassword)
                return BadRequest("Takım şifresi yanlış."); 

            // Şehir restriction kontrolü
            var cityRestriction = await _context.CityRestrictions
                .FirstOrDefaultAsync(cr => cr.CityID == team.CityID);

            if (cityRestriction != null && cityRestriction.IsRegistrationStopped && !team.TeamIsFree)
            {
                return BadRequest(new { message = "Bu şehirde yeni oyuncu kaydı durdurulmuştur." });
            }

            // Benzersiz UserKey oluştur
            string userKey = GenerateUniqueUserKey();

            string? playerIconPath = null;

            try
            {
                if (registerPlayerDto.PlayerIcon != null)
                {
                    var key = $"playerimages/{Guid.NewGuid()}{Path.GetExtension(registerPlayerDto.PlayerIcon.FileName)}";
                    using var stream = registerPlayerDto.PlayerIcon.OpenReadStream();
                    await _r2Manager.UploadFileAsync(key, stream, registerPlayerDto.PlayerIcon.ContentType);
                    playerIconPath = _r2Manager.GetFileUrl(key);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Resim kaydetme sırasında hata oluştu: {ex.Message}");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Email = registerPlayerDto.Email,
                    UserName = registerPlayerDto.Email,
                    Firstname = registerPlayerDto.Firstname,
                    Lastname = registerPlayerDto.Lastname,
                    MacID = registerPlayerDto.MacID,
                    OS = registerPlayerDto.OS,
                    CityID = team.CityID,
                    UserRole = "Player",
                    UserType = UserType.Player,
                    ExternalID = registerPlayerDto.ExternalID,
                    UserKey = userKey,
                    isSubscribed = false,
                    ProfilePictureUrl = playerIconPath
                };

                var createResult = await _customUserManager.CreateUserAsync(user, registerPlayerDto.Password, "Player");
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return BadRequest($"Kullanıcı oluşturulamadı: {errors}");
                }

                var player = new Player
                {
                    FirstName = registerPlayerDto.Firstname,
                    LastName = registerPlayerDto.Lastname,
                    Icon = playerIconPath,
                    Nationality = registerPlayerDto.PlayerNationality,
                    Number = registerPlayerDto.PlayerNumber,
                    Position = registerPlayerDto.PlayerPosition,
                    DateOfBirth = registerPlayerDto.PlayerDateOfBirth,
                    TeamID = registerPlayerDto.TeamID,
                    UserId = user.Id,
                    IdentityNumber = registerPlayerDto.IdentityNumber,
                    isArchived = false,
                    Height = registerPlayerDto.Height,
                    Weight = registerPlayerDto.Weight,
                    PreferredFoot = registerPlayerDto.PreferredFoot,
                    PlayerValue=500
                    
                };

                _context.Players.Add(player);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Oyuncu kaydı başarılı",
                    userkey = user.UserKey
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest($"Kayıt sırasında hata oluştu: {ex.Message}");
            }
        } 
        [HttpGet("refresh-user/{userKey}")]
        [AllowAnonymous] // Ya da uygun bir yetkilendirme mekanizması
        public async Task<ActionResult> RefreshUser(string userKey)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserKey == userKey);

            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            var playerInfo = user.UserType == UserType.Player || user.UserType == UserType.Captain
    ? await _context.Players
        .Where(p => p.UserId == user.Id)
        .Select(p => new
        {
            p.PlayerID,
            p.FirstName,
            p.LastName,
            p.Icon,
            p.Nationality,
            p.Number,
            p.Position,
            p.DateOfBirth,
            p.TeamID,
            p.PlayerType,
            p.IdentityNumber,
            p.isArchived,
            p.Height,
            p.Weight,
            p.PreferredFoot,
            p.PlayerValue,
            TeamName = p.Team.Name,
            TeamCityId = p.Team.CityID
        })
        .FirstOrDefaultAsync()
    : null;

            var response = new
            {
                user = new
                {
                    user.Id,
                    user.Email,
                    user.Firstname,
                    user.Lastname,
                    user.UserType,
                    user.MacID,
                    user.OS,
                    user.UserKey,
                    user.ExternalID,
                    user.isSubscribed,
                    user.CityID,
                    user.UserRole,
                    user.ProfilePictureUrl
                },
                Player = playerInfo
            };

            return Ok(response);
        }

        [HttpPost("update-user/{userKey}")]
        public async Task<IActionResult> UpdateUser(string userKey, [FromForm] UpdateUserDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserKey == userKey);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // Profil resmi güncelleme
            string? newProfilePicUrl = user.ProfilePictureUrl;
            if (dto.ProfilePicture != null)
            {
                var key = $"playerimages/{Guid.NewGuid()}{Path.GetExtension(dto.ProfilePicture.FileName)}";
                using var stream = dto.ProfilePicture.OpenReadStream();
                await _r2Manager.UploadFileAsync(key, stream, dto.ProfilePicture.ContentType);
                newProfilePicUrl = _r2Manager.GetFileUrl(key);
                user.ProfilePictureUrl = newProfilePicUrl;
            }

            // Diğer alanlar
            if (!string.IsNullOrWhiteSpace(dto.Firstname))
                user.Firstname = dto.Firstname;
            if (!string.IsNullOrWhiteSpace(dto.Lastname))
                user.Lastname = dto.Lastname;
            if (dto.CityID.HasValue)
                user.CityID = dto.CityID.Value;

            // Eğer oyuncu ise Player tablosunu da güncelle
            if (user.UserType == UserType.Player)
            {
                var player = await _context.Players.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (player != null)
                {
                    if (!string.IsNullOrWhiteSpace(dto.Firstname))
                        player.FirstName = dto.Firstname;
                    if (!string.IsNullOrWhiteSpace(dto.Lastname))
                        player.LastName = dto.Lastname;
                    if (newProfilePicUrl != null)
                        player.Icon = newProfilePicUrl;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Kullanıcı bilgileri güncellendi." });
        }

        [HttpPost("update-player/{userKey}")]
        public async Task<IActionResult> UpdatePlayer(string userKey, [FromForm] UpdatePlayerDto dto)
        {
            // User'ı bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserKey == userKey);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // Player'ı bul
            var player = await _context.Players.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (player == null)
                return NotFound("Oyuncu bulunamadı.");

            // Profil resmi güncelleme
            string? newProfilePicUrl = user.ProfilePictureUrl;
            if (dto.ProfilePicture != null)
            {
                var key = $"playerimages/{Guid.NewGuid()}{Path.GetExtension(dto.ProfilePicture.FileName)}";
                using var stream = dto.ProfilePicture.OpenReadStream();
                await _r2Manager.UploadFileAsync(key, stream, dto.ProfilePicture.ContentType);
                newProfilePicUrl = _r2Manager.GetFileUrl(key);
                user.ProfilePictureUrl = newProfilePicUrl;
                player.Icon = newProfilePicUrl;
            }

            // Diğer alanlar
            if (!string.IsNullOrWhiteSpace(dto.Firstname))
            {
                user.Firstname = dto.Firstname;
                player.FirstName = dto.Firstname;
            }
            if (!string.IsNullOrWhiteSpace(dto.Lastname))
            {
                user.Lastname = dto.Lastname;
                player.LastName = dto.Lastname;
            }
            if (dto.CityID.HasValue)
                user.CityID = dto.CityID.Value;

            // Player'a özel alanlar
            if (!string.IsNullOrWhiteSpace(dto.Nationality))
                player.Nationality = dto.Nationality;
            if (!string.IsNullOrWhiteSpace(dto.Position))
                player.Position = dto.Position;
            if (dto.Number.HasValue)
                player.Number = dto.Number.Value;
            if (dto.DateOfBirth.HasValue)
                player.DateOfBirth = dto.DateOfBirth.Value;
            if (!string.IsNullOrWhiteSpace(dto.Height))
                player.Height = dto.Height;
            if (!string.IsNullOrWhiteSpace(dto.Weight))
                player.Weight = dto.Weight;
            if (!string.IsNullOrWhiteSpace(dto.PreferredFoot))
                player.PreferredFoot = dto.PreferredFoot;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Oyuncu bilgileri güncellendi." });
        }

        [HttpPost("update-player-subscription/{userKey}")]
        public async Task<IActionResult> UpdatePlayerSubscription(string userKey, [FromBody] UpdatePlayerSubscriptionDto dto)
        {
            // User'ı bul
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserKey == userKey);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            // Player'ı bul
            var player = await _context.Players.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (player == null)
                return NotFound("Oyuncu bulunamadı.");

            // Abonelik bilgilerini güncelle
            player.isSubscribed = dto.isSubscribed;
            player.SubscriptionExpireDate = dto.SubscriptionExpireDate;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Oyuncu abonelik bilgileri güncellendi." });
        }

    }
}