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
    public class StatisticsController : ControllerBase
    { 
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StatisticsController> _logger;
        private readonly IMemoryCache _cache; 

        public StatisticsController(ApplicationDbContext context, ILogger<StatisticsController> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }
         
        [HttpGet("top-player-stats")]
        public async Task<IActionResult> GetTopPlayerStats(int leagueId, int seasonId, int top = 10)
        {
            // En çok gol atan oyuncular
            var topScorers = await _context.Players
                .Where(p => !p.isArchived && p.Goals.Any(g => g.Match.LeagueID == leagueId && 
                                                             g.Match.Week.SeasonID == seasonId && 
                                                             !g.IsOwnGoal))
                .Select(p => new {
                    PlayerId = p.PlayerID,
                    PlayerName = p.FirstName + " " + p.LastName,
                    TeamId = p.TeamID,
                    TeamIcon = p.Team.LogoUrl,
                    TeamName = p.Team.Name,
                    PlayerIcon = p.Icon,
                    GoalCount = p.Goals.Count(g => g.Match.LeagueID == leagueId && 
                                                 g.Match.Week.SeasonID == seasonId && 
                                                 !g.IsOwnGoal)
                })
                .OrderByDescending(p => p.GoalCount)
                .Take(top)
                .ToListAsync();

            // En çok asist yapan oyuncular
            var topAssists = await _context.Players
                .Where(p => !p.isArchived && p.Assists.Any(a => a.Match.LeagueID == leagueId && 
                                                               a.Match.Week.SeasonID == seasonId))
                .Select(p => new {
                    PlayerId = p.PlayerID,
                    PlayerName = p.FirstName + " " + p.LastName,
                    TeamId = p.TeamID,
                    TeamIcon = p.Team.LogoUrl,
                    TeamName = p.Team.Name,
                    PlayerIcon = p.Icon,
                    AssistCount = p.Assists.Count(a => a.Match.LeagueID == leagueId && 
                                                     a.Match.Week.SeasonID == seasonId)
                })
                .OrderByDescending(p => p.AssistCount)
                .Take(top)
                .ToListAsync();

            // En çok gol+asist yapan oyuncular
            var topGoalAssists = await _context.Players
                .Where(p => !p.isArchived && 
                           (p.Goals.Any(g => g.Match.LeagueID == leagueId && 
                                           g.Match.Week.SeasonID == seasonId && 
                                           !g.IsOwnGoal) || 
                            p.Assists.Any(a => a.Match.LeagueID == leagueId && 
                                     a.Match.Week.SeasonID == seasonId)))
                .Select(p => new {
                    PlayerId = p.PlayerID,
                    PlayerName = p.FirstName + " " + p.LastName,
                    TeamId = p.TeamID,
                    TeamIcon = p.Team.LogoUrl,
                    TeamName = p.Team.Name,
                    PlayerIcon = p.Icon,
                    GoalCount = p.Goals.Count(g => g.Match.LeagueID == leagueId && 
                                                 g.Match.Week.SeasonID == seasonId && 
                                                 !g.IsOwnGoal),
                    AssistCount = p.Assists.Count(a => a.Match.LeagueID == leagueId && 
                                                     a.Match.Week.SeasonID == seasonId)
                })
                .OrderByDescending(p => p.GoalCount + p.AssistCount)
                .Take(top)
                .ToListAsync();

            // En çok sarı kart gören oyuncular
            var topYellows = await _context.Players
                .Where(p => !p.isArchived && p.Cards.Any(c => c.Match.LeagueID == leagueId && 
                                                             c.Match.Week.SeasonID == seasonId && 
                                                             c.CardType == CardType.Yellow))
                .Select(p => new {
                    PlayerId = p.PlayerID,
                    PlayerName = p.FirstName + " " + p.LastName,
                    TeamId = p.TeamID,
                    TeamIcon = p.Team.LogoUrl,
                    TeamName = p.Team.Name,
                    PlayerIcon = p.Icon,
                    YellowCount = p.Cards.Count(c => c.Match.LeagueID == leagueId && 
                                                   c.Match.Week.SeasonID == seasonId && 
                                                   c.CardType == CardType.Yellow)
                })
                .OrderByDescending(p => p.YellowCount)
                .Take(top)
                .ToListAsync();

            // En çok kırmızı kart gören oyuncular
            var topReds = await _context.Players
                .Where(p => !p.isArchived && p.Cards.Any(c => c.Match.LeagueID == leagueId && 
                                                             c.Match.Week.SeasonID == seasonId && 
                                                             c.CardType == CardType.Red))
                .Select(p => new {
                    PlayerId = p.PlayerID,
                    PlayerName = p.FirstName + " " + p.LastName,
                    TeamId = p.TeamID,
                    TeamIcon = p.Team.LogoUrl,
                    TeamName = p.Team.Name,
                    PlayerIcon = p.Icon,
                    RedCount = p.Cards.Count(c => c.Match.LeagueID == leagueId && 
                                                 c.Match.Week.SeasonID == seasonId && 
                                                 c.CardType == CardType.Red)
                })
                .OrderByDescending(p => p.RedCount)
                .Take(top)
                .ToListAsync();

            return Ok(new
            {
                TopScorers = topScorers,
                TopAssists = topAssists,
                TopGoalAssists = topGoalAssists,
                TopYellows = topYellows,
                TopReds = topReds
            });
        }

        [HttpGet("top-team-stats")]
        public async Task<IActionResult> GetTopTeamStats(int leagueId, int seasonId, int top = 10)
        {
            // En çok gol atan takımlar
            var topScoringTeams = await _context.Goals
                .Where(g => g.Match.LeagueID == leagueId && g.Match.Week.SeasonID == seasonId && !g.IsOwnGoal)
                .GroupBy(g => g.Team)
                .Select(g => new {
                    TeamId = g.Key.TeamID,
                    TeamName = g.Key.Name,
                    TeamIcon = g.Key.LogoUrl,
                    GoalCount = g.Count()
                })
                .OrderByDescending(g => g.GoalCount)
                .Take(top)
                .ToListAsync();

            // En çok asist yapan takımlar
            var topAssistingTeams = await _context.Goals
                .Where(g => g.Match.LeagueID == leagueId && g.Match.Week.SeasonID == seasonId && g.AssistPlayerID != null)
                .GroupBy(g => g.AssistPlayer.Team)
                .Select(g => new {
                    TeamId = g.Key.TeamID,
                    TeamName = g.Key.Name,
                    TeamIcon = g.Key.LogoUrl,
                    AssistCount = g.Count()
                })
                .OrderByDescending(g => g.AssistCount)
                .Take(top)
                .ToListAsync();

            // En çok sarı kart gören takımlar
            var topYellowTeams = await _context.Cards
                .Where(c => c.Match.LeagueID == leagueId && c.Match.Week.SeasonID == seasonId && c.CardType == CardType.Yellow)
                .GroupBy(c => c.Player.Team)
                .Select(g => new {
                    TeamId = g.Key.TeamID,
                    TeamName = g.Key.Name,
                    TeamIcon = g.Key.LogoUrl,
                    YellowCount = g.Count()
                })
                .OrderByDescending(g => g.YellowCount)
                .Take(top)
                .ToListAsync();

            // En çok kırmızı kart gören takımlar
            var topRedTeams = await _context.Cards
                .Where(c => c.Match.LeagueID == leagueId && c.Match.Week.SeasonID == seasonId && c.CardType == CardType.Red)
                .GroupBy(c => c.Player.Team)
                .Select(g => new {
                    TeamId = g.Key.TeamID,
                    TeamName = g.Key.Name,
                    TeamIcon = g.Key.LogoUrl,
                    RedCount = g.Count()
                })
                .OrderByDescending(g => g.RedCount)
                .Take(top)
                .ToListAsync();

            return Ok(new
            {
                TopScoringTeams = topScoringTeams,
                TopAssistingTeams = topAssistingTeams,
                TopYellowTeams = topYellowTeams,
                TopRedTeams = topRedTeams
            });
        }

        [HttpGet("top-valuable-players")]
        public async Task<IActionResult> GetTopValuablePlayers(int cityId, int top = 10)
        {
            var topPlayers = await _context.Players
                .Where(p => !p.isArchived && 
                            p.Team.CityID == cityId && 
                            p.PlayerValue != null)
                .OrderByDescending(p => p.PlayerValue)
                .Take(top)
                .Select(p => new ValuablePlayerDto
                {
                    PlayerId = p.PlayerID,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    FullName = p.FirstName + " " + p.LastName,
                    Position = p.Position,
                    TeamId = p.TeamID,
                    TeamName = p.Team.Name,
                    TeamIcon = p.Team.LogoUrl,
                    PlayerIcon = p.Icon,
                    PlayerValue = p.PlayerValue,
                    PreferredFoot = p.PreferredFoot
                })
                .ToListAsync();

            return Ok(topPlayers);
        }
    }
}