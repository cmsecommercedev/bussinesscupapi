using BussinessCupApi.Data;
using BussinessCupApi.Models;
using BussinessCupApi.Models.Dtos; // Eklediğimiz DTO'lar için
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory; // IMemoryCache için
using Microsoft.Extensions.Caching.Distributed; // IDistributedCache için (Opsiyonel ama iyi pratik)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BussinessCupApi.Attributes;

namespace BussinessCupApi.Controllers.Api // Namespace'i kontrol edin
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class WeekDetailController : ControllerBase
    {
        // Cache key'lerini sabit olarak tanımlayalım
        private static class CacheKeys
        {
            private const string Prefix = "week_details_";
            public static string Suspensions(int weekId) => $"{Prefix}{weekId}_suspensions";
            public static string BestTeam(int weekId) => $"{Prefix}{weekId}_best_team";
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<WeekDetailController> _logger;
        private readonly IMemoryCache _cache;
        // private readonly IDistributedCache _distributedCache; // Eğer kullanıyorsanız
        private const int CACHE_DURATION_MINUTES = 1; // Haftalık veriler daha sık değişebilir

        public WeekDetailController(
            ApplicationDbContext context,
            ILogger<WeekDetailController> logger,
            IMemoryCache cache
            /* IDistributedCache distributedCache */) // Distributed cache kullanıyorsanız parametreyi ekleyin
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            // _distributedCache = distributedCache;
        }

        // 1. Haftanın Cezalı Oyuncuları
        [HttpGet("{weekId}/suspensions")]
        [ProducesResponseType(typeof(List<SuspensionDto>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<SuspensionDto>>> GetSuspendedPlayers(int weekId)
        {
            string cacheKey = CacheKeys.Suspensions(weekId);

            try
            {
                // Önbellekten almayı dene
                if (_cache.TryGetValue(cacheKey, out ActionResult<List<SuspensionDto>> cachedResult))
                {
                    _logger.LogInformation("Cache hit - Returning suspended players for weekId: {WeekId}", weekId);
                    return cachedResult;
                }

                _logger.LogInformation("Cache miss - Getting suspended players for weekId: {WeekId}", weekId);

                var suspensions = await _context.PlayerSuspension
                    .Where(ps => ps.WeekID == weekId)
                    .Include(ps => ps.Player) // Oyuncu adını almak için
                        .ThenInclude(p => p.Team) // Takım adını almak için
                    .OrderBy(ps => ps.Player.LastName) // Soyada göre sırala
                    .Select(ps => new SuspensionDto
                    {
                        PlayerSuspensionID = ps.PlayerSuspensionID,
                        PlayerID = ps.PlayerID,
                        PlayerFullName = ps.Player.FirstName + " " + ps.Player.LastName,
                        TeamID = ps.Player.TeamID, // Takım ID'si
                        TeamName = ps.Player.Team.Name, // Takım adı
                        SuspensionType = ps.SuspensionType,
                        GamesSuspended = ps.GamesSuspended,
                        PlayerIcon = ps.Player.Icon,
                        Notes = ps.Notes
                    })
                    .ToListAsync();

                if (!suspensions.Any())
                {
                    _logger.LogWarning("No suspensions found for weekId: {WeekId}", weekId);
                    // Boş liste döndürmek NotFound yerine daha uygun olabilir
                    // return NotFound(new { message = "Bu hafta için ceza kaydı bulunamadı." });
                }

                var result = Ok(suspensions);

                // Önbelleğe ekle
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                _cache.Set(cacheKey, result, cacheEntryOptions);

                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Haftanın cezalı oyuncuları yüklenirken hata oluştu. WeekID: {WeekID}", weekId);
                return StatusCode(500, new { error = "Veriler yüklenirken bir sunucu hatası oluştu." });
            }
        }

        // 2. Haftanın Takımı
        [HttpGet("{weekId}/best-team")]
        [ProducesResponseType(typeof(WeekBestTeamDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<WeekBestTeamDto>> GetWeekBestTeam(int weekId)
        {
            string cacheKey = CacheKeys.BestTeam(weekId);

            try
            {
                // Önbellekten almayı dene
                if (_cache.TryGetValue(cacheKey, out ActionResult<WeekBestTeamDto> cachedResult))
                {
                    _logger.LogInformation("Cache hit - Returning best team for weekId: {WeekId}", weekId);
                    return cachedResult;
                }

                _logger.LogInformation("Cache miss - Getting best team for weekId: {WeekId}", weekId);

                var weekBestTeamData = await _context.WeekBestTeams
                    .Where(wbt => wbt.WeekID == weekId)
                    .Include(wbt => wbt.BestPlayer) // Haftanın Oyuncusu
                        .ThenInclude(p => p.Team) // Oyuncunun takımı
                    .Include(wbt => wbt.Players) // Seçilen oyuncu listesi (WeekBestTeamPlayers)
                        .ThenInclude(wbtp => wbtp.Player) // Her seçilen oyuncunun detayı
                            .ThenInclude(p => p.Team) // Seçilen oyuncunun takımı
                    .Select(wbt => new // Doğrudan DTO'ya Select yapalım
                    {
                        wbt.WeekBestTeamID,
                        wbt.WeekID,
                        wbt.LeagueID,
                        wbt.SeasonID,
                        BestPlayer = wbt.BestPlayer == null ? null : new BestPlayerInfoDto // Null kontrolü
                        {
                            PlayerID = wbt.BestPlayer.PlayerID,
                            FullName = wbt.BestPlayer.FirstName + " " + wbt.BestPlayer.LastName,
                            TeamID = wbt.BestPlayer.TeamID,
                            TeamName = wbt.BestPlayer.Team.Name,
                            PlayerIcon = wbt.BestPlayer.Icon,
                            Position = wbt.BestPlayer.Position

                        },
                        wbt.BestTeamID, // Haftanın Takımı ID'si
                        SelectedPlayers = wbt.Players
                                .OrderBy(p => p.OrderNumber) // Sıralamaya göre getir
                                .Select(p => new SelectedPlayerInfoDto
                                {
                                    PlayerID = p.PlayerID,
                                    FullName = p.Player.FirstName + " " + p.Player.LastName,
                                    TeamID = p.Player.TeamID,
                                    TeamName = p.Player.Team.Name,
                                    PlayerIcon = p.Player.Icon,
                                    OrderNumber = p.OrderNumber,
                                    Position=p.Player.Position
                                }).ToList()
                    })
                    .FirstOrDefaultAsync();


                if (weekBestTeamData == null)
                {
                    _logger.LogWarning("Best team data not found for weekId: {WeekId}", weekId);
                    return NotFound(new { message = "Bu hafta için haftanın takımı verisi bulunamadı." });
                }

                // Haftanın Takımı'nın adını ve logosunu alalım
                var bestTeamInfo = await _context.Teams
                    .Where(t => t.TeamID == weekBestTeamData.BestTeamID)
                    .Select(t => new BestTeamInfoDto
                    {
                        TeamID = t.TeamID,
                        TeamName = t.Name,
                        TeamLogo = t.LogoUrl,
                        TeamManager=t.Manager // Takım yöneticisi bilgisi eklendi

                    })
                    .FirstOrDefaultAsync();


                // Son DTO'yu oluşturalım
                var finalDto = new WeekBestTeamDto
                {
                    WeekBestTeamID = weekBestTeamData.WeekBestTeamID,
                    WeekID = weekBestTeamData.WeekID,
                    LeagueID = weekBestTeamData.LeagueID,
                    SeasonID = weekBestTeamData.SeasonID,
                    BestPlayer = weekBestTeamData.BestPlayer,
                    BestTeam = bestTeamInfo, // Ayrı sorgudan gelen takım bilgisi
                    SelectedPlayers = weekBestTeamData.SelectedPlayers
                };


                var result = Ok(finalDto);

                // Önbelleğe ekle
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                _cache.Set(cacheKey, result, cacheEntryOptions);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Haftanın takımı verisi yüklenirken hata oluştu. WeekID: {WeekID}", weekId);
                return StatusCode(500, new { error = "Veriler yüklenirken bir sunucu hatası oluştu." });
            }
        }
    }
}