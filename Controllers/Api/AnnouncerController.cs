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
using BussinessCupApi.Attributes;
using BussinessCupApi.Models.Api; // Kullanıcının kimliğini almak için (opsiyonel)

namespace BussinessCupApi.Controllers.Api // Namespace'i kontrol edin
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnnouncerController> _logger;
        private readonly IMemoryCache _cache;
        private readonly NotificationManager _notificationManager;

        public AnnouncerController(ApplicationDbContext context, ILogger<AnnouncerController> logger, IMemoryCache cache, NotificationManager notificationManager)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _notificationManager = notificationManager;
        }
        [HttpGet("today-matches")]
        public async Task<IActionResult> GetTodayMatches([FromQuery] string userkey)
        {
            if (string.IsNullOrWhiteSpace(userkey))
                return BadRequest("userkey zorunludur.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserKey == userkey);
            if (user == null)
                return NotFound("Kullanıcı bulunamadı.");

            if (user.CityID == null)
                return BadRequest("Kullanıcının CityID bilgisi yok.");

            var today = DateTime.UtcNow.Date;

            var matches = await _context.Matches
                .Include(m => m.League)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m =>
                    m.MatchDate.Date == today &&
                    m.League.CityID == user.CityID)
                .Select(m => new TodayMatchDetailDto
                {
                    MatchID = m.MatchID,
                    LeagueID = m.LeagueID,
                    LeagueName = m.League.Name,
                    HomeTeamID = m.HomeTeamID,
                    HomeTeamName = m.HomeTeam.Name,
                    HomeTeamLogo = m.HomeTeam.LogoUrl,
                    AwayTeamID = m.AwayTeamID,
                    AwayTeamName = m.AwayTeam.Name,
                    AwayTeamLogo = m.AwayTeam.LogoUrl,
                    MatchDate = m.MatchDate,
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                    IsPlayed = m.IsPlayed,
                    MatchURL = m.MatchURL
                })
                .ToListAsync();

            return Ok(matches);
        }

        [HttpGet("match-squads/{matchId}")]
        public async Task<IActionResult> GetMatchSquads(int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null)
                return NotFound("Maç bulunamadı.");

            var squads = await _context.MatchSquads
                .Include(ms => ms.Player)
                .Where(ms => ms.MatchID == matchId)
                .ToListAsync();

            var homePlayers = squads
                .Where(ms => ms.TeamID == match.HomeTeamID)
                .Select(ms => new MatchSquadPlayerDto
                {
                    PlayerID = ms.PlayerID,
                    PlayerName = ms.Player != null ? $"{ms.Player.FirstName} {ms.Player.LastName}" : "",
                    IsStarting11 = ms.IsStarting11,
                    IsSubstitute = ms.IsSubstitute,
                    ShirtNumber = ms.ShirtNumber
                })
                .ToList();

            var awayPlayers = squads
                .Where(ms => ms.TeamID == match.AwayTeamID)
                .Select(ms => new MatchSquadPlayerDto
                {
                    PlayerID = ms.PlayerID,
                    PlayerName = ms.Player != null ? $"{ms.Player.FirstName} {ms.Player.LastName}" : "",
                    IsStarting11 = ms.IsStarting11,
                    IsSubstitute = ms.IsSubstitute,
                    ShirtNumber = ms.ShirtNumber
                })
                .ToList();

            var response = new MatchSquadsResponseDto
            {
                HomeTeam = new MatchSquadTeamDto
                {
                    TeamID = match.HomeTeamID,
                    TeamName = match.HomeTeam?.Name,
                    TeamLogo = match.HomeTeam?.LogoUrl,
                    Players = homePlayers
                },
                AwayTeam = new MatchSquadTeamDto
                {
                    TeamID = match.AwayTeamID,
                    TeamName = match.AwayTeam?.Name,
                    TeamLogo = match.AwayTeam?.LogoUrl,
                    Players = awayPlayers
                }
            };

            return Ok(response);
        }
        [HttpPost("add-substitution")]
        public async Task<IActionResult> AddSubstitution([FromBody] MatchSquadSubstitutionCreateDto dto)
        {
            if (dto == null ||
                dto.MatchID <= 0 ||
                dto.PlayerInID <= 0 ||
                dto.PlayerOutID <= 0 ||
                dto.Minute < 0)
            {
                return BadRequest("Geçersiz veri.");
            }

            // İsteğe bağlı: Maç ve oyuncuların varlığını kontrol edebilirsiniz
            var matchExists = await _context.Matches.AnyAsync(m => m.MatchID == dto.MatchID);
            var playerInExists = await _context.Players.AnyAsync(p => p.PlayerID == dto.PlayerInID);
            var playerOutExists = await _context.Players.AnyAsync(p => p.PlayerID == dto.PlayerOutID);

            if (!matchExists || !playerInExists || !playerOutExists)
                return NotFound("Maç veya oyuncu(lar) bulunamadı.");

            var substitution = new MatchSquadSubstitution
            {
                MatchID = dto.MatchID,
                PlayerInID = dto.PlayerInID,
                PlayerOutID = dto.PlayerOutID,
                Minute = dto.Minute
            };

            _context.Set<MatchSquadSubstitution>().Add(substitution);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, substitutionId = substitution.Id });
        }

        [HttpPost("update-match-score")]
        public async Task<IActionResult> UpdateMatchScore([FromBody] MatchScoreUpdateDto dto)
        {
            if (dto == null || dto.MatchID <= 0 || dto.ScoringTeamID <= 0 || dto.ScorerPlayerID <= 0 || dto.Minute < 0)
                return BadRequest("Geçersiz veri.");

            var match = await _context.Matches.FirstOrDefaultAsync(m => m.MatchID == dto.MatchID);
            if (match == null)
                return NotFound("Maç bulunamadı.");

            // Maç skorunu güncelle
            if (dto.HomeScore.HasValue)
                match.HomeScore = dto.HomeScore;
            if (dto.AwayScore.HasValue)
                match.AwayScore = dto.AwayScore;

            // Gol kaydını ekle
            var goal = new Goal
            {
                MatchID = dto.MatchID,
                TeamID = dto.ScoringTeamID,
                PlayerID = dto.ScorerPlayerID,
                AssistPlayerID = dto.AssistPlayerID,
                Minute = dto.Minute,
                IsPenalty = dto.IsPenalty,
                IsOwnGoal = dto.IsOwnGoal
            };
            _context.Goals.Add(goal);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, matchId = match.MatchID, goalId = goal.GoalID });
        }
    }
}