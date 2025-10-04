using BussinessCupApi.Data;
using BussinessCupApi.Models;
using BussinessCupApi.Models.Dtos; // Eklediğimiz DTO'lar için
using BussinessCupApi.Managers;    // NotificationManager için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory; // IMemoryCache için
using Microsoft.Extensions.Caching.Distributed; // IDistributedCache için (Opsiyonel ama iyi pratik)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using BussinessCupApi.Dtos;
using BussinessCupApi.Attributes; // Kullanıcının kimliğini almak için (opsiyonel)

namespace BussinessCupApi.Controllers.Api // Namespace'i kontrol edin
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class FavouritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavouritesController> _logger;
        private readonly IMemoryCache _cache;
        private readonly NotificationManager _notificationManager;

        public FavouritesController(ApplicationDbContext context, ILogger<FavouritesController> logger, IMemoryCache cache, NotificationManager notificationManager)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _notificationManager = notificationManager;
        }

        [HttpPost("addusertoall")]
        public async Task<IActionResult> AddUserToAll([FromQuery] string userToken, string culture = "tr")
        {
            if (string.IsNullOrWhiteSpace(userToken))
                return BadRequest("Geçersiz macid veya kullanıcı token bilgisi.");


            var result = await _notificationManager.SubscribeToTopicAsync(new[] { userToken }, $"all_users_{culture}");

            if (result.success)
                return Ok(new { success = true, message = $"Kullanıcı başarıyla eklendi" });
            else
                return StatusCode(500, new { success = false, message = $"Hata: {result.message}" });
        }

        [HttpPost("addteamtofav")]
        public async Task<IActionResult> AddTeamToFav([FromQuery] int teamId, [FromQuery] string userToken, string MacID, string culture = "tr")
        {
            // Zaten favori mi kontrol et
            bool alreadyExists = await _context.FavouriteTeams
                .AnyAsync(f => f.TeamID == teamId && f.UserToken == userToken);

            if (alreadyExists)
                return Ok(new { success = true, message = "Bu takım zaten favorilerde." });

            // Tabloya ekle
            var fav = new FavouriteTeams
            {
                TeamID = teamId,
                UserToken = userToken,
                MacID = MacID
            };
            _context.FavouriteTeams.Add(fav);
            await _context.SaveChangesAsync();

            // Firebase topic abone et
            string topic = $"team_{teamId}_{culture}";
            var result = await _notificationManager.SubscribeToTopicAsync(new[] { userToken }, topic);

            if (result.success)
                return Ok(new { success = true, message = $"Takım favorilere eklendi ve topic'e abone olundu: {topic}" });
            else
                return StatusCode(500, new { success = false, message = $"Favorilere eklendi fakat topic abonesi yapılamadı: {result.message}" });
        }

        [HttpPost("removeteamfromfav")]
        public async Task<IActionResult> RemoveTeamFromFav([FromQuery] int teamId, [FromQuery] string userToken, string MacID, string culture = "tr")
        {
            if (teamId <= 0 || string.IsNullOrWhiteSpace(userToken))
                return BadRequest("Geçersiz takım veya kullanıcı token bilgisi.");

            // Favori kaydını bul
            var fav = await _context.FavouriteTeams
                .FirstOrDefaultAsync(f => f.TeamID == teamId && f.MacID == MacID);

            if (fav == null)
                return Ok(new { success = true, message = "Bu takım zaten favorilerde değil." });

            // Tablo kaydını sil
            _context.FavouriteTeams.Remove(fav);
            await _context.SaveChangesAsync();

            // Firebase topic'ten çıkar
            string topic = $"team_{teamId}_{culture}";
            var result = await _notificationManager.UnsubscribeFromTopicAsync(new[] { userToken }, topic);

            if (result.success)
                return Ok(new { success = true, message = $"Takım favorilerden çıkarıldı ve topic'ten çıkıldı: {topic}" });
            else
                return StatusCode(500, new { success = false, message = $"Favorilerden çıkarıldı fakat topic'ten çıkılamadı: {result.message}" });
        }

        [HttpGet("isteamfav")]
        public async Task<IActionResult> IsTeamFav([FromQuery] int teamId, [FromQuery] string macId)
        {
            if (teamId <= 0 || string.IsNullOrWhiteSpace(macId))
                return BadRequest("Geçersiz takım veya cihaz bilgisi.");

            bool isFav = await _context.FavouriteTeams
                .AnyAsync(f => f.TeamID == teamId && f.MacID == macId);

            return Ok(new { isFavourite = isFav });
        }

    }
}