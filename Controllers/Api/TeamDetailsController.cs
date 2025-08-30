using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BussinessCupApi.Attributes;
using BussinessCupApi.Data;
using BussinessCupApi.Models;
using BussinessCupApi.Models.Api;
using BussinessCupApi.Models.UserPlayerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Controllers.Api
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class TeamDetailsController : ControllerBase
    {
        // Cache key'lerini sabit olarak tanımlayalım
        private static class CacheKeys
        {
            private const string Prefix = "team_details_";
            public static string Leagues => $"{Prefix}leagues";
            public static string LeagueMatches(int leagueId) => $"{Prefix}league_{leagueId}_matches";
            public static string MatchSquads(int matchId) => $"{Prefix}match_{matchId}_squads";
            public static string TeamPlayers(int teamId) => $"{Prefix}team_{teamId}_players";
            // ... diğer cache key'leri
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeamDetailsController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDistributedCache _distributedCache;
        private const int CACHE_DURATION_MINUTES = 1;

        public TeamDetailsController(
            ApplicationDbContext context,
            ILogger<TeamDetailsController> logger,
            IMemoryCache cache,
            IDistributedCache distributedCache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _distributedCache = distributedCache;
        }

        [HttpGet("team-leagues/{teamId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetTeamLeagues(int teamId)
        {
            try
            {
                var cacheKey = CacheKeys.TeamPlayers(teamId);

                if (_cache.TryGetValue(cacheKey, out IEnumerable<object> cachedLeagues))
                {
                    return Ok(cachedLeagues);
                }

                var team = await _context.Teams
                    .Where(t => t.TeamID == teamId)
                    .Select(t => new { t.Name, t.LogoUrl })
                    .FirstOrDefaultAsync();

                if (team == null)
                {
                    return NotFound($"Takım ID {teamId} bulunamadı.");
                }

                var teamLeagues = await _context.Matches
               .Include(m => m.Week)
                   .ThenInclude(w => w.Season)
               .Where(m => m.HomeTeamID == teamId || m.AwayTeamID == teamId)
               .GroupBy(m => new { m.LeagueID, m.League.Name, m.League.LogoPath })
               .Select(g => new
               {
                   LeagueId = g.Key.LeagueID,
                   LeagueName = g.Key.Name,
                   LeagueIcon = g.Key.LogoPath, // LeagueIcon bilgisi eklendi
                   Seasons = g.Select(m => new
                   {
                       SeasonId = m.Week.SeasonID,
                       SeasonName = m.Week.Season.Name
                   })
                   .Distinct()
                   .OrderByDescending(s => s.SeasonName)
                   .ToList()
               })
               .OrderBy(l => l.LeagueName)
               .ToListAsync();

                if (!teamLeagues.Any())
                {
                    return NotFound($"Takım ID {teamId} için lig bilgisi bulunamadı.");
                }

                var result = new
                {
                    TeamName = team.Name,
                    TeamLogo = team.LogoUrl,
                    Leagues = teamLeagues
                };

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                _cache.Set(cacheKey, result, cacheOptions);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Takım ligleri getirilirken hata oluştu. Takım ID: {teamId}");
                return StatusCode(500, "Veriler getirilirken bir hata oluştu.");
            }
        }

        [HttpGet("team-matches/{teamId}/{seasonId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetTeamMatchesBySeason(int teamId, int seasonId)
        {
            try
            {
                var matches = await _context.Matches
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Include(m => m.Week)
                    .Where(m =>
                        (m.HomeTeamID == teamId || m.AwayTeamID == teamId) &&
                        m.Week.SeasonID == seasonId)
                    .Select(m => new
                    {
                        MatchId = m.MatchID,
                        WeekNumber = m.Week.WeekNumber,
                        MatchDate = m.MatchDate,
                        HomeTeam = new
                        {
                            TeamId = m.HomeTeam.TeamID,
                            TeamName = m.HomeTeam.Name,
                            LogoUrl = m.HomeTeam.LogoUrl
                        },
                        AwayTeam = new
                        {
                            TeamId = m.AwayTeam.TeamID,
                            TeamName = m.AwayTeam.Name,
                            LogoUrl = m.AwayTeam.LogoUrl
                        },
                        Score = m.IsPlayed ? $"{m.HomeScore}-{m.AwayScore}" : m.MatchDate.ToString("HH:mm"),
                        IsPlayed = m.IsPlayed,
                        MatchUrl = m.MatchURL,
                        Result = m.IsPlayed ?
                            (m.HomeTeamID == teamId ?
                                (m.HomeScore > m.AwayScore ? "W" :
                                 m.HomeScore < m.AwayScore ? "L" : "D") :
                                (m.AwayScore > m.HomeScore ? "W" :
                                 m.AwayScore < m.HomeScore ? "L" : "D")
                            ) : null
                    })
                    .OrderBy(m => m.WeekNumber)
                    .ToListAsync();

                if (!matches.Any())
                {
                    return NotFound($"Takım ID {teamId} ve Sezon ID {seasonId} için maç bulunamadı.");
                }                 
                return Ok(matches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Takım maçları getirilirken hata oluştu. Takım ID: {teamId}, Sezon ID: {seasonId}");
                return StatusCode(500, "Veriler getirilirken bir hata oluştu.");
            }
        }


        [HttpGet("team-standings/{teamId}/{seasonId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetTeamStandingsBySeason(int teamId, int seasonId)
        {
            try
            {
                var cacheKey = $"team_{teamId}_season_{seasonId}_standings";

                if (_cache.TryGetValue(cacheKey, out IEnumerable<object> cachedStandings))
                {
                    return Ok(cachedStandings);
                }

                // Önce takımın hangi gruplarda olduğunu bulalım
                var teamGroups = await _context.Matches
                    .Where(m => m.Week.SeasonID == seasonId &&
                               (m.HomeTeamID == teamId || m.AwayTeamID == teamId))
                    .Select(m => new { m.GroupID })
                    .Distinct()
                    .ToListAsync();

                // Şimdi bu gruplardaki tüm maçları alalım
                var matches = await _context.Matches
                    .Include(m => m.Group)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => m.Week.SeasonID == seasonId &&
                               m.IsPlayed &&
                               teamGroups.Select(g => g.GroupID).Contains(m.GroupID))
                    .ToListAsync();

                var standings = matches
                    .GroupBy(m => new
                    {
                        m.GroupID,
                        GroupName = m.Group?.GroupName ?? "Lig"
                    })
                    .Select(group => new
                    {
                        GroupId = group.Key.GroupID,
                        GroupName = group.Key.GroupName,
                        IsLeague = group.Key.GroupID == null,
                        Teams = group.SelectMany(m => new[]
                        {
                            new { TeamId = m.HomeTeamID, Team = m.HomeTeam, IsHome = true, Match = m },
                            new { TeamId = m.AwayTeamID, Team = m.AwayTeam, IsHome = false, Match = m }
                        })
                        .GroupBy(x => x.TeamId)
                        .Select(team => new
                        {
                            TeamId = team.Key,
                            TeamName = team.First().Team.Name,
                            LogoUrl = team.First().Team.LogoUrl,
                            Played = team.Count(),
                            Won = team.Count(t =>
                                (t.IsHome && t.Match.HomeScore > t.Match.AwayScore) ||
                                (!t.IsHome && t.Match.AwayScore > t.Match.HomeScore)),
                            Drawn = team.Count(t => t.Match.HomeScore == t.Match.AwayScore),
                            Lost = team.Count(t =>
                                (t.IsHome && t.Match.HomeScore < t.Match.AwayScore) ||
                                (!t.IsHome && t.Match.AwayScore < t.Match.HomeScore)),
                            GoalsFor = team.Sum(t => t.IsHome ? t.Match.HomeScore ?? 0 : t.Match.AwayScore ?? 0),
                            GoalsAgainst = team.Sum(t => t.IsHome ? t.Match.AwayScore ?? 0 : t.Match.HomeScore ?? 0),
                            Points = team.Sum(t =>
                                (t.IsHome && t.Match.HomeScore > t.Match.AwayScore) ||
                                (!t.IsHome && t.Match.AwayScore > t.Match.HomeScore) ? 3 :
                                t.Match.HomeScore == t.Match.AwayScore ? 1 : 0),
                            IsCurrentTeam = team.Key == teamId // Görüntülenen takımı işaretleyelim
                        })
                        .OrderByDescending(t => t.Points)
                        .ThenByDescending(t => t.GoalsFor - t.GoalsAgainst)
                        .ThenByDescending(t => t.GoalsFor)
                        .ToList()
                    })
                    .OrderBy(g => g.IsLeague)
                    .ThenBy(g => g.GroupName)
                    .ToList();

                if (!standings.Any())
                {
                    return NotFound($"Sezon ID {seasonId} için puan durumu bulunamadı.");
                }

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                _cache.Set(cacheKey, standings, cacheOptions);

                return Ok(standings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Puan durumu getirilirken hata oluştu. Takım ID: {teamId}, Sezon ID: {seasonId}");
                return StatusCode(500, "Veriler getirilirken bir hata oluştu.");
            }
        }

        [HttpGet("season-players/{teamId}/{seasonId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetSeasonPlayers(int teamId, int seasonId)
        {
            try
            {
                var cacheKey = $"team_{teamId}_season_{seasonId}_players";

                if (_cache.TryGetValue(cacheKey, out IEnumerable<object> cachedPlayers))
                {
                    return Ok(cachedPlayers);
                }

                var players = await _context.Players
                    .Where(p => p.TeamID == teamId && p.isArchived != true)
                    .Select(p => new
                    {
                        PlayerId = p.PlayerID,
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        FullName = p.FirstName + " " + p.LastName,
                        Position = p.Position,
                        Number = p.Number,
                        Icon = p.Icon,
                        Nationality = p.Nationality,
                        PlayerType = p.PlayerType,
                        DateOfBirth = p.DateOfBirth,
                        MatchStats = new
                        {
                            TotalMatches = _context.MatchSquads.Count(ms =>
                                ms.PlayerID == p.PlayerID &&
                                ms.TeamID == teamId &&
                                ms.Match.Week.SeasonID == seasonId),
                            Started = _context.MatchSquads.Count(ms =>
                                ms.PlayerID == p.PlayerID &&
                                ms.TeamID == teamId &&
                                ms.Match.Week.SeasonID == seasonId &&
                                ms.IsStarting11),
                            Substitute = _context.MatchSquads.Count(ms =>
                                ms.PlayerID == p.PlayerID &&
                                ms.TeamID == teamId &&
                                ms.Match.Week.SeasonID == seasonId &&
                                ms.IsSubstitute)
                        },
                        Goals = new
                        {
                            Total = _context.Goals.Count(g =>
                                g.PlayerID == p.PlayerID &&
                                g.Match.Week.SeasonID == seasonId &&
                                !g.IsOwnGoal),
                            Penalties = _context.Goals.Count(g =>
                                g.PlayerID == p.PlayerID &&
                                g.Match.Week.SeasonID == seasonId &&
                                g.IsPenalty),
                            OwnGoals = _context.Goals.Count(g =>
                                g.PlayerID == p.PlayerID &&
                                g.Match.Week.SeasonID == seasonId &&
                                g.IsOwnGoal),
                            Assists = _context.Goals.Count(g =>
                                g.AssistPlayerID == p.PlayerID &&
                                g.Match.Week.SeasonID == seasonId)
                        },
                        Cards = new
                        {
                            Yellow = _context.Cards.Count(c =>
                                c.PlayerID == p.PlayerID &&
                                c.Match.Week.SeasonID == seasonId &&
                                c.CardType == CardType.Yellow),
                            Red = _context.Cards.Count(c =>
                                c.PlayerID == p.PlayerID &&
                                c.Match.Week.SeasonID == seasonId &&
                                c.CardType == CardType.Red)
                        }
                    })
                    .OrderBy(p => p.Number)
                    .ToListAsync();

                if (!players.Any())
                {
                    return NotFound($"Takım ID {teamId} ve Sezon ID {seasonId} için oyuncu bulunamadı.");
                }

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                _cache.Set(cacheKey, players, cacheOptions);

                return Ok(players);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Sezon oyuncuları getirilirken hata oluştu. Takım ID: {teamId}, Sezon ID: {seasonId}");
                return StatusCode(500, "Veriler getirilirken bir hata oluştu.");
            }
        }

        [HttpGet("team-season-stats/{teamId}/{seasonId}")]
        public async Task<TeamSeasonStatsResult> GetTeamSeasonStatsAsync(int teamId, int seasonId, string macId = "")
        {
            try
            {
                var team = await _context.Teams
                    .Where(t => t.TeamID == teamId)
                    .Select(t => new { t.TeamID, t.Name, t.LogoUrl, t.Manager })
                    .FirstOrDefaultAsync();

                if (team == null)
                {
                    return null;
                }

                var season = await _context.Season
                    .Where(s => s.SeasonID == seasonId)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();

                var matches = await _context.Matches
                    .Where(m => m.IsPlayed &&
                               (m.HomeTeamID == teamId || m.AwayTeamID == teamId) &&
                               m.Week.SeasonID == seasonId)
                    .Select(m => new
                    {
                        m.HomeTeamID,
                        m.AwayTeamID,
                        m.HomeScore,
                        m.AwayScore
                    })
                    .ToListAsync();

                // Favori kontrolü
                bool isFavorite = false;
                if (!string.IsNullOrEmpty(macId))
                {
                    isFavorite = await _context.FavouriteTeams
                        .AnyAsync(f => f.TeamID == teamId && f.MacID == macId);
                }

                var stats = new TeamSeasonStatsResult
                {
                    TeamId = team.TeamID,
                    TeamName = team.Name,
                    TeamIcon = team.LogoUrl,
                    Manager = team.Manager, // Manager bilgisi eklendi
                    SeasonId = seasonId,
                    SeasonName = season ?? "Bilinmeyen Sezon",
                    Played = matches.Count,
                    Won = matches.Count(m =>
                        (m.HomeTeamID == teamId && m.HomeScore > m.AwayScore) ||
                        (m.AwayTeamID == teamId && m.AwayScore > m.HomeScore)),
                    Drawn = matches.Count(m => m.HomeScore == m.AwayScore),
                    Lost = matches.Count(m =>
                        (m.HomeTeamID == teamId && m.HomeScore < m.AwayScore) ||
                        (m.AwayTeamID == teamId && m.AwayScore < m.HomeScore)),
                    GoalsFor = matches.Sum(m => m.HomeTeamID == teamId ? m.HomeScore ?? 0 : m.AwayScore ?? 0),
                    GoalsAgainst = matches.Sum(m => m.HomeTeamID == teamId ? m.AwayScore ?? 0 : m.HomeScore ?? 0),
                    Points = (matches.Count(m =>
                        (m.HomeTeamID == teamId && m.HomeScore > m.AwayScore) ||
                        (m.AwayTeamID == teamId && m.AwayScore > m.HomeScore)) * 3) +
                        matches.Count(m => m.HomeScore == m.AwayScore),
                    IsFavorite = isFavorite // Favori bilgisi eklendi
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Takım sezon istatistikleri hesaplanırken hata oluştu. TeamID: {TeamID}, SeasonID: {SeasonID}", teamId, seasonId);
                throw;
            }
        }
    }
}
