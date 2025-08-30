using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BussinessCupApi.Attributes;
using BussinessCupApi.Data;
using BussinessCupApi.DTOs;
using BussinessCupApi.Managers;
using BussinessCupApi.Models;
using BussinessCupApi.Models.Dtos;
using BussinessCupApi.Models.UserPlayerTypes;
using BussinessCupApi.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BussinessCupApi.Models.Match;

namespace Controllers.Api
{
    [ApiKeyAuth]
    [Route("api/[controller]")]
    [ApiController]
    public class LeagueDetailsController : ControllerBase
    {
        // Cache key'lerini sabit olarak tanımlayalım
        private static class CacheKeys
        {
            private const string Prefix = "league_details_";
            public static string Leagues => $"{Prefix}leagues";
            public static string LeaguesByCity(int cityId) => $"{Prefix}leagues_city_{cityId}";
            public static string Cities => $"{Prefix}cities";
            public static string LeagueMatches(int leagueId) => $"{Prefix}league_{leagueId}_matches";
            public static string MatchSquads(int matchId) => $"{Prefix}match_{matchId}_squads";
            public static string TeamPlayers(int teamId) => $"{Prefix}team_{teamId}_players";
            // ... diğer cache key'leri
        }

        private readonly ApplicationDbContext _context;
        private readonly ILogger<LeagueDetailsController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IDistributedCache _distributedCache;
        private readonly LeagueManager _leagueManager;
        private const int CACHE_DURATION_MINUTES = 1;

        public LeagueDetailsController(
            ApplicationDbContext context,
            ILogger<LeagueDetailsController> logger,
            IMemoryCache cache,
            IDistributedCache distributedCache,
            LeagueManager leagueManager)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _distributedCache = distributedCache;
            _leagueManager = leagueManager;
        }

        // Yeni Endpoint: Şehir Listesi
        [HttpGet("cities")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<string>>> GetCityList()
        {
            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<IEnumerable<string>>>(
                    CacheKeys.Cities, // Şehir listesi için tanımlanan cache key
                    async entry =>
                    {
                        entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES * 500)); // Şehir listesi daha uzun cache'lenebilir
                        entry.SetPriority(CacheItemPriority.Normal);

                        _logger.LogInformation("Cache miss - Getting city list");

                        // Varsayım: League modelinde 'City' adında bir string property var.
                        // Gerçek property adını buraya yazın.
                        var cities = await _context.Leagues
                            .Select(l => l.City) // Sadece şehir bilgisini seç 
                            .Distinct() // Tekrarları kaldır
                            .OrderBy(city => city.Order) // Alfabetik sırala
                            .ToListAsync();

                        return Ok(cities);
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şehir listesi yüklenirken hata oluştu");
                return StatusCode(500, new { error = "Şehir listesi yüklenirken bir hata oluştu" });
            }
        }

        // 1. Lig ve Maç Skorları
        [HttpGet("leagues/{cityId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<LeagueOverviewDto>>> GetLeaguesWithMatches(int cityId)
        {
            try
            {
                var leagues = await _context.Leagues
                    .OrderByDescending(l => l.StartDate)
                    .Where(l => l.CityID == cityId)
                    .Select(l => new LeagueOverviewDto
                    {
                        LeagueID = l.LeagueID,
                        Name = l.Name,
                        StartDate = l.StartDate,
                        LeagueType = l.LeagueType,
                        EndDate = l.EndDate
                    })
                    .ToListAsync();

                foreach (var league in leagues)
                {
                    var currentWeek = await _context.Weeks
                        .Include(w => w.Season)
                        .Include(w => w.Matches)
                            .ThenInclude(m => m.HomeTeam)
                        .Include(w => w.Matches)
                            .ThenInclude(m => m.AwayTeam)
                        .Include(w => w.Matches)
                            .ThenInclude(m => m.Group)
                        .Where(w => w.LeagueID == league.LeagueID
                               && w.StartDate <= DateTime.Today
                               && w.EndDate >= DateTime.Today)
                        .OrderBy(w => w.WeekNumber)
                        .Select(w => new WeekOverviewDto
                        {
                            WeekID = w.WeekID,
                            WeekNumber = w.WeekNumber,
                            StartDate = w.StartDate,
                            EndDate = w.EndDate,
                            SeasonID = w.SeasonID,
                            SeasonName = w.Season.Name,
                            LeagueName = league.Name,
                            GroupedMatches = w.Matches
                                .Where(m => m.GroupID.HasValue)
                                .GroupBy(m => new { m.GroupID, m.Group.GroupName })
                                .Select(g => new GroupMatchesDto
                                {
                                    GroupId = g.Key.GroupID.Value,
                                    GroupName = g.Key.GroupName,
                                    Matches = g.Select(m => new MatchDetailDto
                                    {
                                        MatchID = m.MatchID,
                                        HomeTeam = m.HomeTeam.Name,
                                        AwayTeam = m.AwayTeam.Name,
                                        HomeTeamLogo = m.HomeTeam.LogoUrl,
                                        AwayTeamLogo = m.AwayTeam.LogoUrl,
                                        HomeScore = m.HomeScore,
                                        AwayScore = m.AwayScore,
                                        MatchDate = m.MatchDate,
                                        IsPlayed = m.IsPlayed,
                                        HomeTeamId = m.HomeTeamID,
                                        AwayTeamId = m.AwayTeamID,
                                        GroupId = m.GroupID,
                                        MatchStatus = m.Status
                                    })
                                    .OrderBy(m => m.MatchDate)
                                    .ToList()
                                })
                                .ToList(),
                            UngroupedMatches = w.Matches
                                .Where(m => !m.GroupID.HasValue)
                                .Select(m => new MatchOverviewDto
                                {
                                    MatchID = m.MatchID,
                                    HomeTeam = m.HomeTeam.Name,
                                    AwayTeam = m.AwayTeam.Name,
                                    HomeTeamLogo = m.HomeTeam.LogoUrl,
                                    AwayTeamLogo = m.AwayTeam.LogoUrl,
                                    HomeScore = m.HomeScore,
                                    AwayScore = m.AwayScore,
                                    MatchDate = m.MatchDate,
                                    HomeTeamId = m.HomeTeamID,
                                    AwayTeamId = m.AwayTeamID,
                                    IsPlayed = m.IsPlayed,
                                    MatchStatus = m.Status
                                })
                                .OrderBy(m => m.MatchDate)
                                .ToList()
                        })
                        .FirstOrDefaultAsync();

                    if (currentWeek == null)
                    {
                        currentWeek = await _context.Weeks
                        .Include(w => w.Season)
                        .Include(w => w.Matches)
                            .ThenInclude(m => m.HomeTeam)
                        .Include(w => w.Matches)
                            .ThenInclude(m => m.AwayTeam)
                        .Include(w => w.Matches)
                            .ThenInclude(m => m.Group)
                        .Where(w => w.LeagueID == league.LeagueID && w.StartDate < DateTime.Today)
                        .OrderByDescending(w => w.WeekNumber)
                        .Select(w => new WeekOverviewDto
                        {
                            WeekID = w.WeekID,
                            WeekNumber = w.WeekNumber,
                            StartDate = w.StartDate,
                            EndDate = w.EndDate,
                            SeasonID = w.SeasonID,
                            SeasonName = w.Season.Name,
                            LeagueName = league.Name,
                            GroupedMatches = w.Matches
                                .Where(m => m.GroupID.HasValue)
                                .GroupBy(m => new { m.GroupID, m.Group.GroupName })
                                .Select(g => new GroupMatchesDto
                                {
                                    GroupId = g.Key.GroupID.Value,
                                    GroupName = g.Key.GroupName,
                                    Matches = g.Select(m => new MatchDetailDto
                                    {
                                        MatchID = m.MatchID,
                                        HomeTeam = m.HomeTeam.Name,
                                        AwayTeam = m.AwayTeam.Name,
                                        HomeTeamLogo = m.HomeTeam.LogoUrl,
                                        AwayTeamLogo = m.AwayTeam.LogoUrl,
                                        HomeScore = m.HomeScore,
                                        AwayScore = m.AwayScore,
                                        MatchDate = m.MatchDate,
                                        IsPlayed = m.IsPlayed,
                                        HomeTeamId = m.HomeTeamID,
                                        AwayTeamId = m.AwayTeamID,
                                        GroupId = m.GroupID,
                                        MatchStatus = m.Status
                                    })
                                    .OrderBy(m => m.MatchDate)
                                    .ToList()
                                })
                                .ToList(),
                            UngroupedMatches = w.Matches
                                .Where(m => !m.GroupID.HasValue)
                                .Select(m => new MatchOverviewDto
                                {
                                    MatchID = m.MatchID,
                                    HomeTeam = m.HomeTeam.Name,
                                    AwayTeam = m.AwayTeam.Name,
                                    HomeTeamLogo = m.HomeTeam.LogoUrl,
                                    AwayTeamLogo = m.AwayTeam.LogoUrl,
                                    HomeScore = m.HomeScore,
                                    AwayScore = m.AwayScore,
                                    MatchDate = m.MatchDate,
                                    HomeTeamId = m.HomeTeamID,
                                    AwayTeamId = m.AwayTeamID,
                                    IsPlayed = m.IsPlayed,
                                    MatchStatus = m.Status
                                })
                                .OrderBy(m => m.MatchDate)
                                .ToList()
                        })
                        .FirstOrDefaultAsync();

                        if (currentWeek != null)
                        {
                            // Bu durumda bulunan hafta bir sonraki hafta değil,
                            // en son oynanan veya geçmiş bir hafta oluyor.
                            // IsNextWeek = true ataması mantıksal olarak yanlış olabilir.
                            // Belki sadece aktif hafta yoksa null bırakmak daha doğrudur.
                            // league.IsNextWeek = true; // Bu satırı yeniden değerlendirin.
                        }
                    }

                    league.CurrentWeek = currentWeek;
                }

                var leaguesWithWeeks = leagues.Where(l => l.CurrentWeek != null).ToList();

                _logger.LogInformation($"Returning {leaguesWithWeeks.Count} leagues with current/last week matches for cityId: {cityId}");

                return Ok(leaguesWithWeeks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ligler ve maçlar yüklenirken hata oluştu. CityID: {CityID}", cityId);
                return StatusCode(500, new { error = "Veriler yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet("actual-matches/{cityId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ActualMatchesResponseDto>> GetActualMatches(int cityId)
        {
            var today = DateTime.Today;

            // Şehre ait tüm ligleri al
            var leagues = await _context.Leagues
                .Where(l => l.CityID == cityId)
                .ToListAsync();

            var result = new List<ActualMatchesResponseDto>();

            foreach (var league in leagues)
            {
                // Lig için en yakın haftayı bul
                var weeks = await _context.Weeks
                    .Where(w => w.LeagueID == league.LeagueID)
                    .ToListAsync();

                var week = weeks
                    .OrderBy(w => Math.Abs((w.StartDate - today).Days))
                    .FirstOrDefault();

                if (week == null)
                    continue;

                // Haftanın tüm maçlarını al
                var matches = await _context.Matches
                    .Where(m => m.WeekID == week.WeekID)
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .ToListAsync();

                if (!matches.Any())
                    continue;

                var grouped = matches
                    .GroupBy(m => m.MatchDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new ActualMatchDayDto
                    {
                        Date = g.Key,
                        Matches = g.Select(m => new ActualMatchDto
                        {
                            MatchID = m.MatchID,
                            HomeTeam = m.HomeTeam.Name,
                            AwayTeam = m.AwayTeam.Name,
                            HomeTeamLogo = m.HomeTeam.LogoUrl,
                            AwayTeamLogo = m.AwayTeam.LogoUrl,
                            HomeScore = m.HomeScore,
                            AwayScore = m.AwayScore,
                            MatchDate = m.MatchDate,
                            IsPlayed = m.IsPlayed,
                            HomeTeamId = m.HomeTeamID,
                            AwayTeamId = m.AwayTeamID,
                            MatchStatus = m.Status,
                            MatchStarted = m.MatchStarted
                        }).OrderBy(m => m.MatchDate).ToList()
                    })
                    .ToList();

                result.Add(new ActualMatchesResponseDto
                {
                    LeagueID = league.LeagueID,
                    LeagueName = league.Name,
                    LeagueIcon = league.LogoPath,
                    WeekID = week.WeekID,
                    WeekName = week.WeekName,
                    Days = grouped
                });
            }

            return Ok(result);
        }

        // 2. Maç Detayları (Goller)
        [HttpGet("matches/{matchId}/details")]
        public async Task<ActionResult<MatchDetailsDto>> GetMatchDetails(int matchId)
        {
            string cacheKey = $"match_details_{matchId}";

            try
            {
                var result = await _cache.GetOrCreateAsync<ActionResult<MatchDetailsDto>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                    entry.SetPriority(CacheItemPriority.Normal);


                    var match = await _context.Matches
                        .Include(m => m.Goals)
                            .ThenInclude(g => g.Player)
                        .Include(m => m.Goals)
                            .ThenInclude(g => g.AssistPlayer)
                        .Include(m => m.Cards)
                            .ThenInclude(c => c.Player)
                                .ThenInclude(p => p.Team)
                        .Include(m => m.HomeTeam)
                        .Include(m => m.AwayTeam)
                        .Include(m => m.ManOfTheMatch)
                            .ThenInclude(p => p.Team)
                        .Where(m => m.MatchID == matchId)
                        .Select(m => new MatchDetailsDto
                        {
                            MatchID = m.MatchID,
                            HomeTeam = m.HomeTeam.Name,
                            HomeTeamLogo = m.HomeTeam.LogoUrl,
                            HomeTeamId = m.HomeTeam.TeamID,                // Eklendi
                            HomeTeamManager = m.HomeTeam.Manager,          // Eklendi
                            AwayTeam = m.AwayTeam.Name,
                            AwayTeamLogo = m.AwayTeam.LogoUrl,
                            AwayTeamId = m.AwayTeam.TeamID,                // Eklendi
                            AwayTeamManager = m.AwayTeam.Manager,          // Eklendi
                            MatchUrl = m.MatchURL,
                            MatchStatus = m.Status,
                            HomeScore = m.HomeScore,                       // Eklendi
                            AwayScore = m.AwayScore,                       // Eklendi
                            LeagueName = m.Week.League.Name,
                            LeagueIcon = m.Week.League.LogoPath,
                            Goals = m.Goals.Select(g => new GoalDto
                            {
                                Minute = g.Minute,
                                TeamID = g.TeamID,
                                PlayerID = g.PlayerID,
                                PlayerName = $"{g.Player.FirstName} {g.Player.LastName}",
                                PlayerIcon = g.Player.Icon,  // Yeni eklenen özellik
                                AssistPlayerName = g.AssistPlayer != null ? $"{g.AssistPlayer.FirstName} {g.AssistPlayer.LastName}" : null,
                                TeamName = g.TeamID == m.HomeTeamID ? m.HomeTeam.Name : m.AwayTeam.Name,
                                IsPenalty = g.IsPenalty,
                                IsOwnGoal = g.IsOwnGoal
                            }).OrderBy(g => g.Minute).ToList(),
                            Cards = m.Cards.Select(c => new CardDto
                            {
                                Minute = c.Minute,
                                TeamID = c.Player.TeamID,
                                PlayerID = c.PlayerID,
                                PlayerName = $"{c.Player.FirstName} {c.Player.LastName}",
                                PlayerIcon = c.Player.Icon,  // Yeni eklenen özellik
                                TeamName = c.Player.Team.Name,
                                CardType = c.CardType
                            }).OrderBy(c => c.Minute).ToList(),
                            ManOfTheMatch = m.ManOfTheMatch != null ? new PlayerBasicDto
                            {
                                PlayerID = m.ManOfTheMatch.PlayerID,
                                PlayerName = $"{m.ManOfTheMatch.FirstName} {m.ManOfTheMatch.LastName}",
                                PlayerIcon = m.ManOfTheMatch.Icon,  // Yeni eklenen özellik
                                TeamName = m.ManOfTheMatch.Team.Name,
                                TeamID = m.ManOfTheMatch.Team.TeamID,
                                Goals = m.Goals.Count(g => g.PlayerID == m.ManOfTheMatch.PlayerID && !g.IsOwnGoal && g.MatchID == m.MatchID),
                                Assists = m.Goals.Count(g => g.AssistPlayerID == m.ManOfTheMatch.PlayerID && m.MatchID == m.MatchID)
                            } : null
                        })
                        .FirstOrDefaultAsync();

                    if (match == null)
                        return NotFound(new { error = "Maç bulunamadı" });

                    return Ok(match);
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maç detayları yüklenirken hata oluştu. MatchID: {MatchID}", matchId);
                return StatusCode(500, new { error = "Maç detayları yüklenirken bir hata oluştu" });
            }
        }

        // 3. Takım Kadroları
        [HttpGet("teams/{teamId}/squad")]
        public async Task<ActionResult<TeamSquadDto>> GetTeamSquad(int teamId)
        {
            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<TeamSquadDto>>(
                    CacheKeys.TeamPlayers(teamId),
                    async entry =>
                    {
                        entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                        entry.SetPriority(CacheItemPriority.Normal);

                        _logger.LogInformation($"Cache miss - Getting team squad for teamId: {teamId}");

                        var team = await _context.Teams
                            .Include(t => t.Players)
                            .Where(t => t.TeamID == teamId)
                            .Select(t => new TeamSquadDto
                            {
                                TeamID = t.TeamID,
                                Name = t.Name,
                                TeamLogo = t.LogoUrl,
                                Players = t.Players.Select(p => new PlayerDto
                                {
                                    PlayerID = p.PlayerID,
                                    FirstName = p.FirstName,
                                    LastName = p.LastName,
                                    Position = p.Position,
                                    Number = p.Number,
                                    Age = CalculateAge(Convert.ToDateTime(p.DateOfBirth)),
                                    Country = p.Nationality,
                                    isArchived = p.isArchived
                                })
                                .Where(x => x.isArchived != true)
                                .ToList()
                            })
                            .FirstOrDefaultAsync();

                        if (team == null)
                            return NotFound(new { error = "Takım bulunamadı" });

                        _logger.LogInformation($"Returning squad for team {team.Name} with {team.Players.Count} players");
                        return Ok(team);
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Takım kadrosu yüklenirken hata oluştu. TeamID: {TeamID}", teamId);
                return StatusCode(500, new { error = "Takım kadrosu yüklenirken bir hata oluştu" });
            }
        }

        // Yaş hesaplama yardımcı metodu static olarak değiştirildi
        private static int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        [HttpGet("leagues/{leagueId}/{seasonId}/standings")]
        public async Task<ActionResult<List<LeagueStandingDto>>> GetLeagueStandings(int leagueId, int seasonId, [FromQuery] int? groupId = null)
        {
            string cacheKey = $"league_standings_{leagueId}_{seasonId}_{groupId}";

            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<List<LeagueStandingDto>>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                    var leagueStandingsResult = await _leagueManager.GetLeagueStandingsAsync(leagueId, seasonId, groupId, isAjaxRequest: false);

                    if (leagueStandingsResult.LeagueNotFound)
                    {
                        return NotFound(new { error = leagueStandingsResult.ErrorMessage ?? "Lig bulunamadı." });
                    }

                    if (!string.IsNullOrEmpty(leagueStandingsResult.ErrorMessage))
                    {
                        _logger.LogError($"Error getting league standings from manager: {leagueStandingsResult.ErrorMessage}");
                        return StatusCode(500, new { error = leagueStandingsResult.ErrorMessage });
                    }

                    if (leagueStandingsResult.ViewModel == null || leagueStandingsResult.ViewModel.Standings == null)
                    {
                        _logger.LogInformation($"No standings data returned from manager for LeagueID: {leagueId}, SeasonID: {seasonId}, GroupID: {groupId}");
                        return Ok(new List<LeagueStandingDto>());
                    }

                    // Lig statü bilgilerini çek
                    var rankingStatuses = await _context.Set<LeagueRankingStatus>()
                        .Where(x => x.LeagueID == leagueId)
                        .OrderBy(x => x.OrderNo)
                        .Select(x => new LeagueRankingStatusDto
                        {
                            LeagueRankingStatusID = x.LeagueRankingStatusID,
                            OrderNo = x.OrderNo,
                            ColorCode = x.ColorCode,
                            Description = x.Description
                        })
                        .ToListAsync();

                    var orderedStandings = leagueStandingsResult.ViewModel.Standings
                        .Select(s => new LeagueStandingDto
                        {
                            TeamID = s.TeamID,
                            TeamName = s.TeamName,
                            TeamIcon = s.TeamIcon,
                            GroupId = leagueStandingsResult.ViewModel.CurrentGroupId,
                            Played = s.Played,
                            Won = s.Won,
                            Drawn = s.Drawn,
                            Lost = s.Lost,
                            GoalsFor = s.GoalsFor,
                            GoalsAgainst = s.GoalsAgainst,
                            GoalDifference = s.GoalDifference,
                            Points = s.Points,
                            RemainingMatches = 0,
                            LeagueName = leagueStandingsResult.ViewModel.LeagueName,
                            SeasonName = leagueStandingsResult.ViewModel.SeasonName,
                            PenaltyDescription = s.PenaltyDescription,
                            PenaltyPoints = s.PenaltyPoints,
                            LeagueRankingStatuses = rankingStatuses
                        })
                        .ToList();

                    _logger.LogInformation($"Returning standings with {orderedStandings.Count} teams for {leagueStandingsResult.ViewModel.LeagueName} - {leagueStandingsResult.ViewModel.SeasonName}");
                    return Ok(orderedStandings);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Puan durumu yüklenirken hata oluştu. LeagueID: {LeagueID}, SeasonID: {SeasonID}, GroupID: {GroupID}",
                    leagueId, seasonId, groupId);
                return StatusCode(500, new { error = $"Puan durumu yüklenirken bir hata oluştu: {ex.Message}" });
            }
        }

        [HttpGet("leagues/{leagueId}/{seasonId}/weeks")]
        public async Task<ActionResult<LeagueWeeksDto>> GetLeagueAllWeeks(int leagueId, int seasonId, [FromQuery] int? groupId = null)
        {
            string cacheKey = $"league_weeks_{leagueId}_{seasonId}_{groupId}";

            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<LeagueWeeksDto>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                    var league = await _context.Leagues
                        .Include(l => l.Seasons.Where(s => s.SeasonID == seasonId))
                        .Include(l => l.Weeks.Where(w => w.SeasonID == seasonId))
                            .ThenInclude(w => w.Matches)
                                .ThenInclude(m => m.Group)
                        .Include(l => l.Weeks.Where(w => w.SeasonID == seasonId))
                            .ThenInclude(w => w.Matches)
                                .ThenInclude(m => m.HomeTeam)
                        .Include(l => l.Weeks.Where(w => w.SeasonID == seasonId))
                            .ThenInclude(w => w.Matches)
                                .ThenInclude(m => m.AwayTeam)
                        .Where(l => l.LeagueID == leagueId)
                        .Select(l => new LeagueWeeksDto
                        {
                            LeagueID = l.LeagueID,
                            Name = l.Name,
                            GroupId = groupId,
                            Seasons = l.Seasons
                                .Where(s => s.SeasonID == seasonId)
                                .Select(s => new LeagueWeekSeasonDto
                                {
                                    SeasonID = s.SeasonID,
                                    Name = s.Name,
                                    StartDate = l.Weeks
                                        .Where(w => w.SeasonID == seasonId)
                                        .Min(w => w.StartDate),
                                    EndDate = l.Weeks
                                        .Where(w => w.SeasonID == seasonId)
                                        .Max(w => w.EndDate),
                                    Weeks = l.Weeks
                                        .Where(w => w.SeasonID == seasonId)
                                        .OrderBy(w => w.StartDate)
                                        .Select(w => new LeagueWeekDto
                                        {
                                            WeekID = w.WeekID,
                                            WeekNumber = w.WeekNumber,
                                            WeekName = w.WeekName,
                                            StartDate = w.StartDate,
                                            EndDate = w.EndDate,
                                            // ... existing code ...
                                            IsCurrentWeek = w.EndDate > DateTime.Today && // Bugünden sonra başlayan
                                            (l.Weeks.Where(x => x.SeasonID == seasonId && x.EndDate > DateTime.Today)
                                            .OrderBy(x => x.EndDate)
                                            .Select(x => x.WeekID)
                                            .FirstOrDefault() == w.WeekID), // En yakın hafta
                                            DateGroupedMatches = w.Matches
                                                // ... existing code ...
                                                .GroupBy(m => m.MatchDate.Date)
                                                .OrderBy(g => g.Key)
                                                .Select(g => new DateGroupedMatchesDto
                                                {
                                                    Date = g.Key.ToString("dd.MM"),
                                                    FullDate = g.Key,
                                                    GroupedMatches = g
                                                        .Where(m => m.GroupID == groupId)
                                                        .GroupBy(m => new { m.GroupID, m.Group.GroupName })
                                                        .Select(group => new LeagueWeekGroupMatchesDto
                                                        {
                                                            GroupId = group.Key.GroupID.Value,
                                                            GroupName = group.Key.GroupName,
                                                            Matches = group
                                                                .OrderBy(m => m.MatchDate)
                                                                .Select(m => new LeagueWeekMatchDto
                                                                {
                                                                    MatchID = m.MatchID,
                                                                    HomeTeam = m.HomeTeam.Name,
                                                                    AwayTeam = m.AwayTeam.Name,
                                                                    HomeTeamLogo = m.HomeTeam.LogoUrl,
                                                                    AwayTeamLogo = m.AwayTeam.LogoUrl,
                                                                    HomeScore = m.HomeScore,
                                                                    AwayScore = m.AwayScore,
                                                                    MatchDate = m.MatchDate,
                                                                    MatchStatus = m.Status,
                                                                    HomeTeamId = m.HomeTeamID,
                                                                    AwayTeamId = m.AwayTeamID,
                                                                    GroupId = m.GroupID
                                                                })
                                                                .ToList()
                                                        })
                                                        .ToList(),
                                                    UngroupedMatches = g
                                                        .Where(m => !m.GroupID.HasValue)
                                                        .OrderBy(m => m.MatchDate)
                                                        .Select(m => new LeagueWeekMatchDto
                                                        {
                                                            MatchID = m.MatchID,
                                                            HomeTeam = m.HomeTeam.Name,
                                                            AwayTeam = m.AwayTeam.Name,
                                                            HomeTeamLogo = m.HomeTeam.LogoUrl,
                                                            AwayTeamLogo = m.AwayTeam.LogoUrl,
                                                            HomeScore = m.HomeScore,
                                                            AwayScore = m.AwayScore,
                                                            MatchDate = m.MatchDate,
                                                            MatchStatus = m.Status,
                                                            HomeTeamId = m.HomeTeamID,
                                                            AwayTeamId = m.AwayTeamID
                                                        })
                                                        .ToList()
                                                })
                                                .ToList()
                                        })
                                        .ToList()
                                })
                                .ToList()
                        })
                        .FirstOrDefaultAsync();

                    if (league == null)
                    {
                        _logger.LogWarning($"League not found with ID: {leagueId}");
                        return NotFound(new { error = "Lig bulunamadı" });
                    }

                    if (!league.Seasons.Any())
                    {
                        _logger.LogWarning($"Season not found with ID: {seasonId} for league: {leagueId}");
                        return NotFound(new { error = "Bu lige ait sezon bulunamadı" });
                    }

                    _logger.LogInformation($"Returning league {league.Name} with season {seasonId}");
                    return Ok(league);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lig hafta ve maç bilgileri yüklenirken hata oluştu. LeagueID: {LeagueID}, SeasonID: {SeasonID}, GroupID: {GroupID}",
                    leagueId, seasonId, groupId);
                return StatusCode(500, new { error = "Veriler yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet("leagues/{leagueId}/{seasonId}/statistics")]
        public async Task<ActionResult<LeagueStatisticsDto>> GetLeagueStatistics(int leagueId, int seasonId, [FromQuery] int top = 10)
        {
            string cacheKey = $"league_statistics_{leagueId}_{seasonId}_{top}";

            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<LeagueStatisticsDto>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                    // Goller ve Asistler - Grup filtresi kaldırıldı
                    var goals = await _context.Goals
                        .Include(g => g.Match)
                        .Include(g => g.Player)
                        .Include(g => g.Team)
                        .Include(g => g.AssistPlayer)
                        .Include(g => g.AssistPlayer.Team)
                        .Where(g => g.Match.Week.LeagueID == leagueId &&
                               g.Match.Week.SeasonID == seasonId)
                        .ToListAsync();

                    if (!goals.Any())
                        return NotFound(new { error = "Bu lige ait istatistik bulunamadı" });

                    // Gol krallığı
                    var topScorers = goals
                        .Where(g => !g.IsOwnGoal)
                        .GroupBy(g => new
                        {
                            g.PlayerID,
                            g.Player.FirstName,
                            g.Player.LastName,
                            TeamID = g.Player.TeamID,
                            TeamName = g.Player.Team.Name
                        })
                        .Select(g => new PlayerStatsDto
                        {
                            PlayerID = g.Key.PlayerID,
                            PlayerName = $"{g.Key.FirstName} {g.Key.LastName}",
                            TeamID = g.Key.TeamID,
                            TeamName = g.Key.TeamName,
                            Goals = g.Count(),
                            PenaltyGoals = g.Count(x => x.IsPenalty),
                            Assists = goals.Count(x => x.AssistPlayerID == g.Key.PlayerID),
                            Matches = g.Select(x => x.Match.MatchID).Distinct().Count()
                        })
                        .OrderByDescending(p => p.Goals)
                        .ThenByDescending(p => p.Assists)
                        .ThenBy(p => p.Matches)
                        .Take(top)
                        .ToList();

                    // Asist krallığı
                    var topAssists = goals
                        .Where(g => g.AssistPlayerID.HasValue)
                        .GroupBy(g => new
                        {
                            g.AssistPlayerID,
                            g.AssistPlayer.FirstName,
                            g.AssistPlayer.LastName,
                            TeamID = g.AssistPlayer.TeamID,
                            TeamName = g.AssistPlayer.Team.Name
                        })
                        .Select(a => new PlayerStatsDto
                        {
                            PlayerID = a.Key.AssistPlayerID.Value,
                            PlayerName = $"{a.Key.FirstName} {a.Key.LastName}",
                            TeamID = a.Key.TeamID,
                            TeamName = a.Key.TeamName,
                            Goals = goals.Count(g => g.PlayerID == a.Key.AssistPlayerID && !g.IsOwnGoal),
                            Assists = a.Count(),
                            Matches = a.Select(x => x.Match.MatchID).Distinct().Count()
                        })
                        .OrderByDescending(p => p.Assists)
                        .ThenByDescending(p => p.Goals)
                        .ThenBy(p => p.Matches)
                        .Take(top)
                        .ToList();

                    // Takım istatistikleri
                    var matchIds = await _context.Weeks
                        .Where(w => w.LeagueID == leagueId && w.SeasonID == seasonId)
                        .SelectMany(w => w.Matches.Select(m => m.MatchID))
                        .ToListAsync();

                    var teamStats = await _context.Teams
                        .Where(t => t.HomeMatches.Any(m => m.Week.LeagueID == leagueId && m.Week.SeasonID == seasonId) ||
                                    t.AwayMatches.Any(m => m.Week.LeagueID == leagueId && m.Week.SeasonID == seasonId))
                        .Select(t => new
                        {
                            Team = t,
                            Goals = _context.Goals.Count(g => g.TeamID == t.TeamID &&
                                                            matchIds.Contains(g.MatchID) &&
                                                            !g.IsOwnGoal),
                            PenaltyGoals = _context.Goals.Count(g => g.TeamID == t.TeamID &&
                                                                    matchIds.Contains(g.MatchID) &&
                                                                    g.IsPenalty),
                            OwnGoals = _context.Goals.Count(g => g.TeamID == t.TeamID &&
                                                                matchIds.Contains(g.MatchID) &&
                                                                g.IsOwnGoal),
                            Assists = _context.Goals.Count(g => g.TeamID == t.TeamID &&
                                                                      matchIds.Contains(g.MatchID) &&
                                                                      g.AssistPlayerID.HasValue),
                            YellowCards = _context.Cards.Count(c => c.Player.TeamID == t.TeamID &&
                                                                       matchIds.Contains(c.MatchID) &&
                                                                       c.CardType == CardType.Yellow),
                            RedCards = _context.Cards.Count(c => c.Player.TeamID == t.TeamID &&
                                                                     matchIds.Contains(c.MatchID) &&
                                                                     c.CardType == CardType.Red),
                            TotalMatches = _context.Matches.Count(m => (m.HomeTeamID == t.TeamID || m.AwayTeamID == t.TeamID) &&
                                                                      matchIds.Contains(m.MatchID) &&
                                                                      m.IsPlayed)
                        })
                        .Select(x => new TeamStatsDto
                        {
                            TeamID = x.Team.TeamID,
                            TeamName = x.Team.Name,
                            TotalGoals = x.Goals,
                            PenaltyGoals = x.PenaltyGoals,
                            OwnGoals = x.OwnGoals,
                            TotalAssists = x.Assists,
                            YellowCards = x.YellowCards,
                            RedCards = x.RedCards,
                            TotalMatches = x.TotalMatches
                        })
                        .OrderByDescending(t => t.TotalGoals)
                        .ToListAsync();

                    var totalAssists = goals.Count(g => g.AssistPlayerID.HasValue);

                    var statistics = new LeagueStatisticsDto
                    {
                        TopScorers = topScorers,
                        TopAssists = topAssists,
                        TeamStats = teamStats,
                        TotalGoals = goals.Count,
                        TotalAssists = totalAssists,
                        PenaltyGoals = goals.Count(g => g.IsPenalty),
                        OwnGoals = goals.Count(g => g.IsOwnGoal),
                        AverageGoalsPerMatch = (double)goals.Count / goals.Select(g => g.Match.MatchID).Distinct().Count()
                    };

                    return Ok(statistics);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstatistikler yüklenirken hata oluştu. LeagueID: {LeagueID}, SeasonID: {SeasonID}",
                    leagueId, seasonId);
                return StatusCode(500, new { error = "İstatistikler yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet("leagues/{leagueId}/{seasonId}/playerstatistics")]
        public async Task<ActionResult<List<PlayerSeasonStatisticsDto>>> GetLeaguePlayerStatistics(int leagueId, int seasonId, [FromQuery] int top = 10)
        {
            string cacheKey = $"league_player_statistics_{leagueId}_{seasonId}_{top}";

            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<List<PlayerSeasonStatisticsDto>>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                    _logger.LogInformation($"Getting player statistics for league {leagueId}, season {seasonId}");

                    var weekIds = await _context.Weeks
                        .Where(w => w.LeagueID == leagueId && w.SeasonID == seasonId)
                        .Select(w => w.WeekID)
                        .ToListAsync();

                    if (!weekIds.Any())
                        return NotFound(new { error = "Bu lig ve sezon için hafta bulunamadı" });

                    // Grup filtresi kaldırıldı
                    var playerStats = await _context.Players
                        .Where(p => p.Team.HomeMatches.Any(m => weekIds.Contains(m.WeekID)) ||
                               p.Team.AwayMatches.Any(m => weekIds.Contains(m.WeekID)))
                        .Select(p => new PlayerSeasonStatisticsDto
                        {
                            PlayerID = p.PlayerID,
                            FirstName = p.FirstName,
                            LastName = p.LastName,
                            TeamID = p.TeamID,
                            PlayerType = p.PlayerType,
                            TeamName = p.Team.Name,
                            PlayerIcon = p.Icon,
                            Position = p.Position,
                            Number = p.Number,
                            Goals = p.Goals
                                .Count(g => weekIds.Contains(g.Match.WeekID) && !g.IsOwnGoal),
                            Assists = p.Assists
                                .Count(g => weekIds.Contains(g.Match.WeekID)),
                            PenaltyGoals = p.Goals
                                .Count(g => weekIds.Contains(g.Match.WeekID) && g.IsPenalty),
                            OwnGoals = p.Goals
                                .Count(g => weekIds.Contains(g.Match.WeekID) && g.IsOwnGoal),
                            YellowCards = p.Cards
                                .Count(c => weekIds.Contains(c.Match.WeekID) && c.CardType == CardType.Yellow),
                            RedCards = p.Cards
                                .Count(c => weekIds.Contains(c.Match.WeekID) && c.CardType == CardType.Red),
                            ManOfTheMatch = p.Goals
                                .Count(g => weekIds.Contains(g.Match.WeekID) && !g.IsOwnGoal)
                        })
                        .ToListAsync();

                    // İstatistikleri olan oyuncuları filtrele ve sırala
                    var activePlayers = playerStats
                        .Where(p => p.Matches > 0 || p.Goals > 0 || p.Assists > 0 ||
                                   p.YellowCards > 0 || p.RedCards > 0)
                        .OrderByDescending(p => p.Goals)
                        .ThenByDescending(p => p.Assists)
                        .ThenByDescending(p => p.ManOfTheMatch)
                        .Take(top)
                        .ToList();

                    _logger.LogInformation($"Returning statistics for {activePlayers.Count} players");
                    return Ok(activePlayers);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Oyuncu istatistikleri yüklenirken hata oluştu. LeagueID: {LeagueID}, SeasonID: {SeasonID}",
                    leagueId, seasonId);
                return StatusCode(500, new { error = "İstatistikler yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet("leagues/{leagueId}/seasons")]
        public async Task<ActionResult<List<SeasonBasicDto>>> GetLeagueSeasons(int leagueId)
        {
            string cacheKey = $"league_seasons_{leagueId}";

            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<List<SeasonBasicDto>>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                    var league = await _context.Leagues
                        .Include(l => l.Seasons)
                        .Include(l => l.Weeks) // Haftaları da dahil edelim
                        .Where(l => l.LeagueID == leagueId)
                        .FirstOrDefaultAsync();

                    if (league == null)
                    {
                        _logger.LogWarning($"League not found with ID: {leagueId}");
                        return NotFound(new { error = "Lig bulunamadı" });
                    }

                    var seasons = league.Seasons
                        .Select(s => new
                        {
                            Season = s,
                            SeasonWeeks = league.Weeks.Where(w => w.SeasonID == s.SeasonID).ToList() // Haftaları filtrele ve listeye çevir
                        })
                        .Where(x => x.SeasonWeeks.Any()) // Sadece haftası olan sezonları al
                        .Select(x => new SeasonBasicDto
                        {
                            SeasonID = x.Season.SeasonID,
                            Name = x.Season.Name,
                            StartDate = x.SeasonWeeks.Min(w => w.StartDate), // Artık boş koleksiyon olmayacak
                            EndDate = x.SeasonWeeks.Max(w => w.EndDate),     // Artık boş koleksiyon olmayacak
                            IsActive = x.Season.IsActive
                        })
                        .OrderByDescending(s => s.StartDate)
                        .ToList();


                    if (!seasons.Any())
                    {
                        _logger.LogWarning($"No seasons with weeks found for league with ID: {leagueId}");
                        // Haftası olan sezon bulunamadıysa boş liste veya NotFound döndürebiliriz.
                        // Şimdilik boş liste dönelim.
                        return Ok(new List<SeasonBasicDto>());
                    }

                    _logger.LogInformation($"Returning {seasons.Count} seasons for league {leagueId}");
                    return Ok(seasons);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lig sezonları yüklenirken hata oluştu. LeagueID: {LeagueID}", leagueId);
                return StatusCode(500, new { error = "Veriler yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet("leagues/team/{teamId}")]
        public async Task<ActionResult<IEnumerable<LeagueOverviewDto>>> GetLeaguesWithMatchesByTeam(int teamId)
        {
            string cacheKey = $"team_leagues_{teamId}_{DateTime.Today:yyyyMMdd}";

            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<IEnumerable<LeagueOverviewDto>>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                    entry.SetPriority(CacheItemPriority.Normal);

                    _logger.LogInformation($"Getting leagues and matches for team: {teamId}");

                    var leagues = await _context.Leagues
                        .Where(l => l.Weeks.Any(w => w.Matches.Any(m => m.HomeTeamID == teamId || m.AwayTeamID == teamId)))
                        .Select(l => new LeagueOverviewDto
                        {
                            LeagueID = l.LeagueID,
                            Name = l.Name,
                            StartDate = l.StartDate,
                            EndDate = l.EndDate,
                            Weeks = l.Weeks
                                .Where(w => w.Matches.Any(m => m.HomeTeamID == teamId || m.AwayTeamID == teamId))
                                .OrderBy(w => w.WeekNumber)
                                .Select(w => new WeekOverviewDto
                                {
                                    WeekID = w.WeekID,
                                    WeekNumber = w.WeekNumber,
                                    StartDate = w.StartDate,
                                    EndDate = w.EndDate,
                                    UngroupedMatches = w.Matches
                                        .Where(m => m.HomeTeamID == teamId || m.AwayTeamID == teamId)
                                        .Select(m => new MatchOverviewDto
                                        {
                                            MatchID = m.MatchID,
                                            HomeTeam = m.HomeTeam.Name,
                                            AwayTeam = m.AwayTeam.Name,
                                            HomeTeamLogo = m.HomeTeam.LogoUrl,
                                            AwayTeamLogo = m.AwayTeam.LogoUrl,
                                            HomeScore = m.HomeScore,
                                            AwayScore = m.AwayScore,
                                            MatchDate = m.MatchDate,
                                            IsPlayed = m.IsPlayed,
                                            Result = m.IsPlayed ?
                                                (m.HomeTeamID == teamId ?
                                                    (m.HomeScore > m.AwayScore ? "Kazandı" :
                                                     m.HomeScore < m.AwayScore ? "Kaybetti" :
                                                     "Berabere") :
                                                    (m.AwayScore > m.HomeScore ? "Kazandı" :
                                                     m.AwayScore < m.HomeScore ? "Kaybetti" :
                                                     "Berabere")) :
                                                null
                                        })
                                        .OrderBy(m => m.MatchDate)
                                        .ToList()
                                })
                                .ToList()
                        })
                        .OrderByDescending(l => l.StartDate)
                        .ToListAsync();

                    var today = DateTime.Today;

                    // Her lig için aktif veya gelecek haftayı belirle
                    foreach (var league in leagues)
                    {
                        // Aktif haftayı bul
                        var currentWeek = league.Weeks
                            .FirstOrDefault(w => w.StartDate <= today && w.EndDate >= today);

                        // Aktif hafta yoksa gelecek ilk haftayı bul
                        if (currentWeek == null)
                        {
                            currentWeek = league.Weeks
                                //  .Where(w => w.StartDate > today)
                                .OrderBy(w => w.StartDate)
                                .FirstOrDefault();

                            if (currentWeek != null)
                            {
                                league.IsNextWeek = true;
                            }
                        }

                        league.CurrentWeek = currentWeek;
                        league.Weeks = null; // Hafta listesini temizle, sadece current week kalsın
                    }

                    var leaguesWithWeeks = leagues.Where(l => l.CurrentWeek != null).ToList();

                    return Ok(leaguesWithWeeks);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Takım için lig ve maç bilgileri yüklenirken hata oluştu. TeamID: {TeamID}", teamId);
                return StatusCode(500, new { error = "Veriler yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet("matches/{matchId}/squads")]
        public async Task<ActionResult<MatchSquadsDto>> GetMatchSquads(int matchId)
        {
            try
            {
                return await _cache.GetWithDependencyAsync<ActionResult<MatchSquadsDto>>(
                    _distributedCache,
                    CacheKeys.MatchSquads(matchId),
                    async () =>
                    {
                        _logger.LogInformation($"Cache miss - Getting match squads for matchId: {matchId}");

                        var match = await _context.Matches
                            .Include(m => m.HomeTeam)
                            .Include(m => m.AwayTeam)
                            .Include(m => m.MatchSquads)
                                .ThenInclude(ms => ms.Player)
                            .FirstOrDefaultAsync(m => m.MatchID == matchId);

                        if (match == null)
                            return NotFound(new { error = "Maç bulunamadı" });

                        var result = new MatchSquadsDto
                        {
                            MatchId = match.MatchID,
                            HomeTeam = new TeamSquadDto
                            {
                                TeamID = match.HomeTeam.TeamID,
                                Name = match.HomeTeam.Name,
                                Manager = match.HomeTeam.Manager,
                                StartingEleven = match.MatchSquads
                                    .Where(ms => ms.TeamID == match.HomeTeamID && ms.IsStarting11 && !ms.IsSubstitute)
                                    .Select(ms => new MatchPlayerDto
                                    {
                                        PlayerID = ms.Player.PlayerID,
                                        Number = ms.ShirtNumber == 0 ? ms.Player.Number : ms.ShirtNumber,
                                        FirstName = ms.Player.FirstName,
                                        LastName = ms.Player.LastName,
                                        PlayerType = ms.Player.PlayerType,
                                        Position = ms.Player.Position,
                                        PlayerIcon = ms.Player.Icon,
                                        ShirtNumber=ms.Player.Number,
                                        IsCaptain = false // Kaptanlık bilgisi MatchSquad'a eklenebilir
                                    }).ToList(),
                                Substitutes = match.MatchSquads
                                    .Where(ms => ms.TeamID == match.HomeTeamID && ms.IsSubstitute)
                                    .Select(ms => new MatchPlayerDto
                                    {
                                        PlayerID = ms.Player.PlayerID,
                                        Number = ms.ShirtNumber == 0 ? ms.Player.Number : ms.ShirtNumber,
                                        FirstName = ms.Player.FirstName,
                                        LastName = ms.Player.LastName,
                                        Position = ms.Player.Position,
                                        IsCaptain = false,
                                        ShirtNumber = ms.Player.Number,
                                        PlayerIcon = ms.Player.Icon
                                    }).ToList()
                            },
                            AwayTeam = new TeamSquadDto
                            {
                                TeamID = match.AwayTeam.TeamID,
                                Name = match.AwayTeam.Name,
                                Manager = match.AwayTeam.Manager,
                                StartingEleven = match.MatchSquads
                                    .Where(ms => ms.TeamID == match.AwayTeamID && ms.IsStarting11 && !ms.IsSubstitute)
                                    .Select(ms => new MatchPlayerDto
                                    {
                                        PlayerID = ms.Player.PlayerID,
                                        Number = ms.ShirtNumber == 0 ? ms.Player.Number : ms.ShirtNumber,
                                        FirstName = ms.Player.FirstName,
                                        LastName = ms.Player.LastName,
                                        Position = ms.Player.Position,
                                        IsCaptain = false,
                                        ShirtNumber = ms.Player.Number,
                                        PlayerIcon = ms.Player.Icon
                                    }).ToList(),
                                Substitutes = match.MatchSquads
                                    .Where(ms => ms.TeamID == match.AwayTeamID && ms.IsSubstitute)
                                    .Select(ms => new MatchPlayerDto
                                    {
                                        PlayerID = ms.Player.PlayerID,
                                        Number = ms.ShirtNumber == 0 ? ms.Player.Number : ms.ShirtNumber,
                                        FirstName = ms.Player.FirstName,
                                        LastName = ms.Player.LastName,
                                        Position = ms.Player.Position,
                                        IsCaptain = false,
                                        ShirtNumber = ms.Player.Number,
                                        PlayerIcon = ms.Player.Icon

                                    }).ToList()
                            }
                        };

                        return Ok(result);
                    },
                    TimeSpan.FromMinutes(30));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maç kadroları yüklenirken hata oluştu. MatchID: {MatchID}", matchId);
                return StatusCode(500, new { error = "Maç kadroları yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet("app-settings")]
        public async Task<ActionResult<Settings>> GetAppSettings()
        {
            string cacheKey = "app_settings";

            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<Settings>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                    entry.SetPriority(CacheItemPriority.High); // Önemli ayarlar olduğu için yüksek öncelik

                    _logger.LogInformation("Cache miss - Getting app settings");

                    var settings = await _context.Settings
                        .OrderByDescending(s => s.LastUpdated)
                        .FirstOrDefaultAsync();

                    if (settings == null)
                    {
                        _logger.LogWarning("Ayarlar bulunamadı");
                        return NotFound(new { error = "Ayarlar bulunamadı" });
                    }

                    return Ok(settings);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uygulama ayarları yüklenirken hata oluştu");
                return StatusCode(500, new { error = "Uygulama ayarları yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet("leagues/{leagueId}/groups")]
        public async Task<ActionResult<List<GroupDto>>> GetLeagueGroups(int leagueId,int seasonId)
        {
            string cacheKey = $"league_groups_{leagueId}{seasonId}";

            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<List<GroupDto>>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                    entry.SetPriority(CacheItemPriority.Normal);

                    _logger.LogInformation($"Getting groups for league: {leagueId}");

                    var groups = await _context.Group
                        .Where(g => g.LeagueID == leagueId && g.SeasonID== seasonId)
                        .Select(g => new GroupDto
                        {
                            GroupID = g.GroupID,
                            GroupName = g.GroupName,
                            LeagueID = g.LeagueID,
                            Description = g.Description                            
                        })
                        .OrderBy(g => g.GroupName)
                        .ToListAsync();

                    if (!groups.Any())
                    {
                        _logger.LogWarning($"No groups found for league with ID: {leagueId}");
                        return NotFound(new { error = "Bu lige ait grup bulunamadı" });
                    }


                    return Ok(groups);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lig grupları yüklenirken hata oluştu. LeagueID: {LeagueID}", leagueId);
                return StatusCode(500, new { error = "Veriler yüklenirken bir hata oluştu" });
            }
        }

        [HttpGet("players/{playerId}/details")]
        public async Task<ActionResult<PlayerDto>> GetPlayerDetails(int playerId)
        {
            string cacheKey = $"player_details_{playerId}";

            try
            {
                return await _cache.GetOrCreateAsync<ActionResult<PlayerDto>>(cacheKey, async entry =>
                {
                    entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                    entry.SetPriority(CacheItemPriority.Normal);

                    _logger.LogInformation($"Getting player details for playerId: {playerId}");

                    // Oyuncu temel bilgileri
                    var player = await _context.Players
                        .Where(p => p.PlayerID == playerId)
                        .Select(p => new PlayerDto
                        {
                            PlayerID = p.PlayerID,
                            FirstName = p.FirstName,
                            LastName = p.LastName,
                            Position = p.Position,
                            Height= p.Height,
                            Weight = p.Weight,
                            PreferredFoot = p.PreferredFoot,
                            Number = p.Number,
                            Icon = p.Icon,
                            UserID=p.UserId,
                            LicensedPlayer = Convert.ToBoolean(p.LicensedPlayer),
                            Age = CalculateAge(Convert.ToDateTime(p.DateOfBirth)),
                            Country = p.Nationality,
                            PlayerValue = Convert.ToInt32(p.PlayerValue),
                            Statistics = new List<PlayerLeagueStatisticsDto>(),
                            PlayerTeam = new TeamBasicDto
                            {
                                TeamID = p.Team.TeamID,
                                Name = p.Team.Name,
                                LogoUrl = p.Team.LogoUrl
                            }
                        })
                        .FirstOrDefaultAsync();

                    if (player == null)
                    {
                        _logger.LogWarning($"Player not found with ID: {playerId}");
                        return NotFound(new { error = "Oyuncu bulunamadı" });
                    }

                    // Ligleri ve sezonları getirelim
                    var leagueSeasons = await _context.Leagues
                        .SelectMany(l => l.Seasons.Select(s => new
                        {
                            LeagueId = l.LeagueID,
                            LeagueName = l.Name,
                            SeasonId = s.SeasonID,
                            SeasonName = s.Name,
                            WeekIds = l.Weeks.Where(w => w.SeasonID == s.SeasonID).Select(w => w.WeekID)
                        }))
                        .ToListAsync();

                    foreach (var ls in leagueSeasons)
                    {
                        // Her lig ve sezon için oyuncunun oynadığı takımları bulalım
                        var teamMatches = await _context.Matches
                            .Where(m => ls.WeekIds.Contains(m.WeekID) &&
                                   m.IsPlayed &&
                                   (m.HomeTeam.Players.Any(p => p.PlayerID == playerId) ||
                                    m.AwayTeam.Players.Any(p => p.PlayerID == playerId)))
                            .Select(m => new
                            {
                                MatchId = m.MatchID,
                                TeamId = m.HomeTeam.Players.Any(p => p.PlayerID == playerId) ? m.HomeTeamID : m.AwayTeamID,
                                TeamName = m.HomeTeam.Players.Any(p => p.PlayerID == playerId) ? m.HomeTeam.Name : m.AwayTeam.Name,
                                TeamIcon = m.HomeTeam.Players.Any(p => p.PlayerID == playerId) ? m.HomeTeam.LogoUrl : m.AwayTeam.LogoUrl
                            })
                            .ToListAsync();

                        if (!teamMatches.Any()) continue;

                        // Takım bazında grupla
                        var teamGroups = teamMatches.GroupBy(tm => new { tm.TeamId, tm.TeamName });

                        foreach (var team in teamGroups)
                        {
                            var teamMatchIds = team.Select(t => t.MatchId).ToList();

                            // Gol ve asist istatistiklerini alalım
                            var goals = await _context.Goals
                                .Where(g => teamMatchIds.Contains(g.MatchID))
                                .ToListAsync();

                            // Kart istatistiklerini alalım
                            var cards = await _context.Cards
                                .Where(c => teamMatchIds.Contains(c.MatchID) && c.PlayerID == playerId)
                                .ToListAsync();

                            // Maç MVP'lerini alalım
                            var mvpCount = await _context.Matches
                                .CountAsync(m => teamMatchIds.Contains(m.MatchID) &&
                                               m.ManOfTheMatchID == playerId);

                            // Burada TotalMatches'ı MatchSquad'dan alıyoruz
                            var totalMatches = await _context.MatchSquads
                                .CountAsync(ms => ms.PlayerID == playerId && teamMatchIds.Contains(ms.MatchID));

                            var stats = new PlayerLeagueStatisticsDto
                            {
                                LeagueId = ls.LeagueId,
                                LeagueName = ls.LeagueName,
                                SeasonId = ls.SeasonId,
                                SeasonName = ls.SeasonName,
                                TeamId = team.Key.TeamId,
                                TeamName = team.Key.TeamName,
                                TeamIcon = team.First().TeamIcon, // Eklenen kısım
                                Statistics = new PlayerStatisticsDto
                                {
                                    TotalMatches = totalMatches,
                                    Goals = goals.Count(g => g.PlayerID == playerId && !g.IsOwnGoal),
                                    Assists = goals.Count(g => g.AssistPlayerID == playerId),
                                    PenaltyGoals = goals.Count(g => g.PlayerID == playerId && g.IsPenalty),
                                    OwnGoals = goals.Count(g => g.PlayerID == playerId && g.IsOwnGoal),
                                    YellowCards = cards.Count(c => c.CardType == CardType.Yellow),
                                    RedCards = cards.Count(c => c.CardType == CardType.Red),
                                    ManOfTheMatch = mvpCount
                                }
                            };

                            // Sadece istatistik olan kayıtları ekle
                            if (stats.Statistics.HasActivity())
                            {
                                player.Statistics.Add(stats);
                            }
                        }
                    }

                    // İstatistikleri tarihe göre sıralayalım
                    player.Statistics = player.Statistics
                        .OrderByDescending(s => s.SeasonId)
                        .ThenBy(s => s.TeamName)
                        .ToList();

                    _logger.LogInformation($"Returning details for player {player.FirstName} {player.LastName} with {player.Statistics.Count} team statistics");
                    return Ok(player);
                });
            }
            catch (Exception ex)
            { 
                return StatusCode(500, new { error = "Oyuncu detayları yüklenirken bir hata oluştu" });
            }
        }
    }

    // DTO'lar
    public class LeagueOverviewDto
    {
        public int LeagueID { get; set; }
        public string Name { get; set; }
        public LeagueType LeagueType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public WeekOverviewDto CurrentWeek { get; set; }
        public bool IsNextWeek { get; set; }
        public List<WeekOverviewDto> Weeks { get; set; } // Geçici kullanım için eklendi
    }

    public class WeekOverviewDto
    {
        public int WeekID { get; set; }
        public int SeasonID { get; set; }
        public string SeasonName { get; set; }
        public string LeagueName { get; set; }
        public int WeekNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<GroupMatchesDto> GroupedMatches { get; set; } = new List<GroupMatchesDto>();
        public List<MatchOverviewDto> UngroupedMatches { get; set; } = new List<MatchOverviewDto>();
    }

    public class GroupMatchesDto
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public List<MatchDetailDto> Matches { get; set; } = new List<MatchDetailDto>();
    }

    public class MatchOverviewDto
    {
        public int MatchID { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string HomeTeamLogo { get; set; }
        public string AwayTeamLogo { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public DateTime MatchDate { get; set; }
        public bool IsPlayed { get; set; }
        public string Result { get; set; }
        public MatchStatus MatchStatus { get; set; }
    }

    public class MatchDetailsDto
    {
        public int MatchID { get; set; }
        public string HomeTeam { get; set; }
        public string HomeTeamLogo { get; set; }
        public int HomeTeamId { get; set; }           // Eklendi
        public string HomeTeamManager { get; set; }   // Eklendi
        public string AwayTeam { get; set; }
        public string AwayTeamLogo { get; set; }
        public int AwayTeamId { get; set; }           // Eklendi
        public string AwayTeamManager { get; set; }   // Eklendi
        public string MatchUrl { get; set; }
        public MatchStatus MatchStatus { get; set; }
        public string LeagueName { get; set; }
        public string LeagueIcon { get; set; }
        public int? HomeScore { get; set; }           // Eklendi
        public int? AwayScore { get; set; }           // Eklendi
        public List<GoalDto> Goals { get; set; }
        public List<CardDto> Cards { get; set; }
        public PlayerBasicDto ManOfTheMatch { get; set; }
    }

    public class TeamSquadDto
    {
        public int TeamID { get; set; }
        public string Name { get; set; }
        public string Manager { get; set; }
        public string TeamLogo { get; set; }
        public List<MatchPlayerDto> StartingEleven { get; set; }
        public List<MatchPlayerDto> Substitutes { get; set; }
        public List<PlayerDto> Players { get; set; }
    }

    public class PlayerDto
    {
        public int PlayerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Position { get; set; }
        public int? Number { get; set; }
        public string Icon { get; set; }
        public string UserID { get; set; }
        public int Age { get; set; }
        public string Height { get; set; } // Boy (cm)
        public string Weight { get; set; } // Kilo (kg)
        public string PreferredFoot { get; set; } // Sağ/Sol ayak
        public string Country { get; set; }
        public bool isArchived { get; set; }
        public bool LicensedPlayer { get; set; }
        public int PlayerValue { get; set; }
        public List<PlayerLeagueStatisticsDto> Statistics { get; set; }
        public TeamBasicDto PlayerTeam { get; set; }
    }

    public class TeamBasicDto
    {
        public int TeamID { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
    }

    public class GoalDto
    {
        public int Minute { get; set; }
        public int TeamID { get; set; }
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public string AssistPlayerName { get; set; }
        public string PlayerIcon { get; set; }  // Yeni eklenen özellik

        public string TeamName { get; set; }
        public bool IsPenalty { get; set; }
        public bool IsOwnGoal { get; set; }
    }

    public class CardDto
    {
        public int Minute { get; set; }
        public int TeamID { get; set; }
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public string PlayerIcon { get; set; }  // Yeni eklenen özellik

        public string TeamName { get; set; }
        public CardType CardType { get; set; }
    }

    public class PlayerBasicDto
    {
        public int PlayerID { get; set; }
        public int TeamID { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public string PlayerIcon { get; set; }  // Yeni eklenen özellik

        public string PlayerName { get; set; }
        public string TeamName { get; set; }
    }

    public class PlayerStatsDto
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public int Goals { get; set; }
        public int PenaltyGoals { get; set; }
        public int Assists { get; set; }
        public int Matches { get; set; }
        public string PlayerIcon { get; set; }
        public string TeamIcon { get; set; }

    }

    public class TeamStatsDto
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public int TotalGoals { get; set; }
        public int PenaltyGoals { get; set; }
        public int OwnGoals { get; set; }
        public int TotalAssists { get; set; }
        public int YellowCards { get; set; }
        public int RedCards { get; set; }
        public int TotalMatches { get; set; }
        public string TeamIcon { get; set; }
        public double AverageGoalsPerMatch => TotalMatches > 0 ? (double)TotalGoals / TotalMatches : 0;
    }

    public class SeasonDto
    {
        public int SeasonID { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<WeekWithMatchesDto> Weeks { get; set; }
    }

    public class WeekWithMatchesDto
    {
        public int WeekID { get; set; }
        public int WeekNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsCurrentWeek { get; set; }
        public bool IsNextWeek { get; set; }
        public List<GroupMatchesDto> GroupedMatches { get; set; } = new List<GroupMatchesDto>();
        public List<MatchDetailDto> UngroupedMatches { get; set; } = new List<MatchDetailDto>();
    }

    public class MatchDetailDto
    {
        public int MatchID { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public string? HomeTeamLogo { get; set; }
        public string? AwayTeamLogo { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public DateTime MatchDate { get; set; }
        public bool IsPlayed { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public int? GroupId { get; set; }
        public MatchStatus MatchStatus { get; set; }
    }

    public class PlayerStatisticsDto
    {
        public int TotalMatches { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int PenaltyGoals { get; set; }
        public int OwnGoals { get; set; }
        public int YellowCards { get; set; }
        public int RedCards { get; set; }
        public int ManOfTheMatch { get; set; }
    }

    public class MatchSquadsDto
    {
        public int MatchId { get; set; }
        public TeamSquadDto HomeTeam { get; set; }
        public TeamSquadDto AwayTeam { get; set; }
    }

    public class MatchPlayerDto
    {
        public int PlayerID { get; set; }
        public int? Number { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? ShirtNumber { get; set; }
        public string PlayerIcon { get; set; }
        public string Position { get; set; }
        public PlayerType? PlayerType { get; set; }
        public bool IsCaptain { get; set; }
    }

    public class PlayerLeagueStatisticsDto
    {
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int SeasonId { get; set; }
        public string SeasonName { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string TeamIcon { get; set; } // Yeni eklenen özellik

        public PlayerStatisticsDto Statistics { get; set; }
    }

    public class SeasonBasicDto
    {
        public int SeasonID { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class GroupDto
    {
        public int GroupID { get; set; }
        public string GroupName { get; set; }
        public int LeagueID { get; set; }
        public string Description { get; set; }
    }

    public static class PlayerStatisticsExtensions
    {
        public static bool HasActivity(this PlayerStatisticsDto stats)
        {
            return stats.TotalMatches > 0 ||
                   stats.Goals > 0 ||
                   stats.Assists > 0 ||
                   stats.YellowCards > 0 ||
                   stats.RedCards > 0 ||
                   stats.ManOfTheMatch > 0;
        }
    }

    // LeagueStandingDto'ya yeni alanları ekleyelim
    public class LeagueStandingDto
    {
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public string TeamIcon { get; set; } // Yeni eklenen özellik
        public List<LeagueRankingStatusDto> LeagueRankingStatuses { get; set; } // EKLENDİ

        public int? GroupId { get; set; }
        public string LeagueName { get; set; }
        public string SeasonName { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifference { get; set; }
        public int Points { get; set; }
        public int RemainingMatches { get; set; }
        public int PenaltyPoints { get; set; }
        public string PenaltyDescription { get; set; }
    }

    // Yeni DTO
    public class LeagueRankingStatusDto
    {
        public int LeagueRankingStatusID { get; set; }
        public int OrderNo { get; set; }
        public string ColorCode { get; set; }
        public string Description { get; set; }
    }
}