using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BussinessCupApi.Data;
using BussinessCupApi.DTOs; // DTO klasörünü ekleyin
using BussinessCupApi.DTOs.Web;
using BussinessCupApi.Models;

namespace BussinessCupApi.Managers
{
    public class WebProviderManager
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WebProviderManager> _logger;

        public WebProviderManager(ApplicationDbContext context, ILogger<WebProviderManager> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. Şehir listesini çeker
        public async Task<List<WebCityDto>> GetAllCitiesAsync()
        {
            return await _context.City
                .OrderBy(c => c.Name)
                .Select(c => new WebCityDto
                {
                    CityID = c.CityID,
                    Name = c.Name
                })
                .ToListAsync();
        }

        // 2. Bir şehrin haberlerini çeker
        public async Task<List<WebMatchNewsDto>> GetCityNewsAsync(int cityId, bool onlyPublished = true)
        {
            return await _context.MatchNews
                .Where(n => n.CityID == cityId && (!onlyPublished || n.Published))
                .OrderByDescending(n => n.CreatedDate)
                .Include(n => n.Photos)
                .Select(n => new WebMatchNewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Subtitle = n.Subtitle,
                    MatchNewsMainPhoto = n.MatchNewsMainPhoto,
                    DetailsTitle = n.DetailsTitle,
                    Details = n.Details,
                    CityID = n.CityID,
                    IsMainNews = n.IsMainNews,
                    Published = n.Published,
                    CreatedDate = n.CreatedDate,
                    Photos = n.Photos.Select(p => new WebMatchNewsPhotoDto
                    {
                        Id = p.Id,
                        PhotoUrl = p.PhotoUrl
                    }).ToList()
                })
                .ToListAsync();
        }

        // 3. Bir şehrin takımlarını çeker
        public async Task<List<WebTeamDto>> GetCityTeamsAsync(int cityId)
        {
            return await _context.Teams
                .Where(t => t.CityID == cityId)
                .OrderBy(t => t.Name)
                .Select(t => new WebTeamDto
                {
                    TeamID = t.TeamID,
                    Name = t.Name,
                    CityID = t.CityID,
                    LogoUrl = t.LogoUrl,
                    Manager = t.Manager
                })
                .ToListAsync();
        }

        // 4. Şehre göre ligleri getir
        public async Task<List<WebLeagueDto>> GetLeaguesByCityAsync(int cityId)
        {
            return await _context.Leagues
                .Where(l => l.CityID == cityId)
                .OrderByDescending(l => l.StartDate)
                .Select(l => new WebLeagueDto
                {
                    LeagueID = l.LeagueID,
                    Name = l.Name,
                    StartDate = l.StartDate,
                    LogoPath = l.LogoPath,
                    CityID = l.CityID,
                    TeamSquadCount = l.TeamSquadCount
                })
                .ToListAsync();
        }

        // 5. Lige göre haftaları getir
        public async Task<List<WebWeekDto>> GetWeeksByLeagueAsync(int leagueId)
        {
            return await _context.Weeks
                .Where(w => w.LeagueID == leagueId)
                .OrderBy(w => w.WeekNumber)
                .Select(w => new WebWeekDto
                {
                    WeekID = w.WeekID,
                    LeagueID = w.LeagueID,
                    WeekNumber = w.WeekNumber,
                    WeekName = w.WeekName,
                    StartDate = w.StartDate
                })
                .ToListAsync();
        }

        // 6. Lig ve haftaya göre haftanın maçlarını, skor ve statü ile getir
        public async Task<List<WebMatchDto>> GetMatchesByLeagueAndWeekAsync(int leagueId, int weekId)
        {
            return await _context.Matches
                .Where(m => m.LeagueID == leagueId && m.WeekID == weekId)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Select(m => new WebMatchDto
                {
                    MatchID = m.MatchID,
                    LeagueID = m.LeagueID,
                    WeekID = m.WeekID,
                    GroupID = m.GroupID,
                    HomeTeamID = m.HomeTeamID,
                    AwayTeamID = m.AwayTeamID,
                    MatchDate = m.MatchDate,
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                    Status = m.Status.ToString(),
                    HomeTeam = new WebTeamDto
                    {
                        TeamID = m.HomeTeam.TeamID,
                        Name = m.HomeTeam.Name,
                        CityID = m.HomeTeam.CityID
                    },
                    AwayTeam = new WebTeamDto
                    {
                        TeamID = m.AwayTeam.TeamID,
                        Name = m.AwayTeam.Name,
                        CityID = m.AwayTeam.CityID
                    }
                })
                .ToListAsync();
        }
        public async Task<List<WebPlayerDto>> GetTeamRosterAsync(int teamId)
        {
            return await _context.Players
                .Where(p => p.TeamID == teamId && !p.isArchived)
                .OrderBy(p => p.Number)
                .Select(p => new WebPlayerDto
                {
                    PlayerID = p.PlayerID,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Position = p.Position,
                    Number = p.Number,
                    DateOfBirth = p.DateOfBirth,
                    Nationality = p.Nationality,
                    Icon = p.Icon
                })
                .ToListAsync();
        }

        public async Task<List<WebMatchNewsDto>> GetMainNewsAsync(bool onlyPublished = true)
        {
            return await _context.MatchNews
                .Where(n => n.IsMainNews && (!onlyPublished || n.Published))
                .OrderByDescending(n => n.CreatedDate)
                .Include(n => n.Photos)
                .Select(n => new WebMatchNewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Subtitle = n.Subtitle,
                    MatchNewsMainPhoto = n.MatchNewsMainPhoto,
                    DetailsTitle = n.DetailsTitle,
                    Details = n.Details,
                    CityID = n.CityID,
                    IsMainNews = n.IsMainNews,
                    Published = n.Published,
                    CreatedDate = n.CreatedDate,
                    Photos = n.Photos.Select(p => new WebMatchNewsPhotoDto
                    {
                        Id = p.Id,
                        PhotoUrl = p.PhotoUrl
                    }).ToList()
                })
                .ToListAsync();
        }

        // Takım ID'si ile takım detayını getir
        public async Task<WebTeamDto?> GetTeamByIdAsync(int teamId)
        {
            return await _context.Teams
                .Where(t => t.TeamID == teamId)
                .Select(t => new WebTeamDto
                {
                    TeamID = t.TeamID,
                    Name = t.Name,
                    CityID = t.CityID,
                    LogoUrl = t.LogoUrl,
                    Manager = t.Manager
                })
                .FirstOrDefaultAsync();
        }

        // Haber ID'si ile haber detayını getir
        public async Task<WebMatchNewsDto?> GetNewsByIdAsync(int newsId)
        {
            return await _context.MatchNews
                .Where(n => n.Id == newsId)
                .Include(n => n.Photos)
                .Select(n => new WebMatchNewsDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Subtitle = n.Subtitle,
                    MatchNewsMainPhoto = n.MatchNewsMainPhoto,
                    DetailsTitle = n.DetailsTitle,
                    Details = n.Details,
                    CityID = n.CityID,
                    IsMainNews = n.IsMainNews,
                    Published = n.Published,
                    CreatedDate = n.CreatedDate,
                    Photos = n.Photos.Select(p => new WebMatchNewsPhotoDto
                    {
                        Id = p.Id,
                        PhotoUrl = p.PhotoUrl
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        // Lig için günümüze en yakın haftanın maçlarını, lig ve hafta adıyla getir
        public async Task<WebActualWeekMatchesDto?> GetActualWeekMatchesAsync(int leagueId)
        {
            var today = DateTime.UtcNow.Date;

            // Haftaları çek
            var weeks = await _context.Weeks
                .Where(w => w.LeagueID == leagueId)
                .ToListAsync();

            // En yakın haftayı bul
            var closestWeek = weeks
                .OrderBy(w => Math.Abs((w.StartDate.Date - today).TotalDays))
                .FirstOrDefault();

            if (closestWeek == null)
                return new WebActualWeekMatchesDto();


            int weekId = closestWeek.WeekID;

            // Lig adı
            var league = await _context.Leagues
                .Where(l => l.LeagueID == leagueId)
                .Select(l => new { l.Name })
                .FirstOrDefaultAsync();

            // Maçlar
            var matches = await _context.Matches
                .Where(m => m.LeagueID == leagueId && m.WeekID == weekId)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Select(m => new WebMatchDto
                {
                    MatchID = m.MatchID,
                    LeagueID = m.LeagueID,
                    WeekID = m.WeekID,
                    GroupID = m.GroupID,
                    HomeTeamID = m.HomeTeamID,
                    AwayTeamID = m.AwayTeamID,
                    MatchDate = m.MatchDate,
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                    Status = m.Status.ToString(),
                    HomeTeam = new WebTeamDto
                    {
                        TeamID = m.HomeTeam.TeamID,
                        Name = m.HomeTeam.Name,
                        CityID = m.HomeTeam.CityID
                    },
                    AwayTeam = new WebTeamDto
                    {
                        TeamID = m.AwayTeam.TeamID,
                        Name = m.AwayTeam.Name,
                        CityID = m.AwayTeam.CityID
                    }
                })
                .ToListAsync();

            return new WebActualWeekMatchesDto
            {
                LeagueName = league?.Name ?? "",
                WeekName = closestWeek.WeekName,
                WeekID = closestWeek.WeekID,
                Matches = matches
            };
        }

        // Takımın tüm maçlarını, maçın lig ve hafta adıyla birlikte getir
        public async Task<List<WebActualWeekMatchesDto>> GetAllMatchesByTeamAsync(int teamId)
        {
            // Takımın oynadığı tüm maçları çek
            var matches = await _context.Matches
                .Where(m => m.HomeTeamID == teamId || m.AwayTeamID == teamId)
                .Include(m => m.Week)
                .Include(m => m.League)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .ToListAsync();

            // Maçları hafta ve lig bazında grupla
            var grouped = matches
                .GroupBy(m => new { m.LeagueID, m.League.Name, m.WeekID, m.Week.WeekName })
                .Select(g => new WebActualWeekMatchesDto
                {
                    LeagueName = g.Key.Name,
                    WeekName = g.Key.WeekName,
                    WeekID = g.Key.WeekID,
                    Matches = g.Select(m => new WebMatchDto
                    {
                        MatchID = m.MatchID,
                        LeagueID = m.LeagueID,
                        WeekID = m.WeekID,
                        GroupID = m.GroupID,
                        HomeTeamID = m.HomeTeamID,
                        AwayTeamID = m.AwayTeamID,
                        MatchDate = m.MatchDate,
                        HomeScore = m.HomeScore,
                        AwayScore = m.AwayScore,
                        Status = m.Status.ToString(),
                        HomeTeam = new WebTeamDto
                        {
                            TeamID = m.HomeTeam.TeamID,
                            Name = m.HomeTeam.Name,
                            CityID = m.HomeTeam.CityID
                        },
                        AwayTeam = new WebTeamDto
                        {
                            TeamID = m.AwayTeam.TeamID,
                            Name = m.AwayTeam.Name,
                            CityID = m.AwayTeam.CityID
                        }
                    }).ToList()
                })
                .OrderBy(x => x.LeagueName).ThenBy(x => x.WeekID)
                .ToList();

            return grouped;
        }

        public async Task<WebMatchDetailDto?> GetMatchDetailsByIdAsync(int matchId)
        {
            var match = await _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .FirstOrDefaultAsync(m => m.MatchID == matchId);

            if (match == null)
                return null;

            var goals = await _context.Goals
                .Where(g => g.MatchID == matchId)
                .Include(g => g.Player)
                .Include(g => g.Team)
                .Select(g => new WebGoalDto
                {
                    GoalID = g.GoalID,
                    PlayerID = g.PlayerID,
                    PlayerName = g.Player.FirstName + " " + g.Player.LastName,
                    TeamID = g.TeamID,
                    TeamName = g.Team.Name,
                    Minute = g.Minute,
                    IsPenalty = g.IsPenalty,
                    IsOwnGoal = g.IsOwnGoal,
                    AssistPlayerID = g.AssistPlayerID
                })
                .ToListAsync();

            var cards = await _context.Cards
                .Where(c => c.MatchID == matchId)
                .Include(c => c.Player)
                .Select(c => new WebCardDto
                {
                    CardID = c.CardID,
                    PlayerID = c.PlayerID,
                    PlayerName = c.Player.FirstName + " " + c.Player.LastName,
                    CardType = c.CardType.ToString(),
                    Minute = c.Minute
                })
                .ToListAsync();

            var formations = await _context.MatchSquadFormations
                .Where(f => f.MatchID == matchId)
                .Select(f => new WebFormationDto
                {
                    TeamID = f.TeamID,
                    FormationImage = f.FormationImage
                })
                .ToListAsync();

            var matchDto = new WebMatchDto
            {
                MatchID = match.MatchID,
                LeagueID = match.LeagueID,
                WeekID = match.WeekID,
                GroupID = match.GroupID,
                HomeTeamID = match.HomeTeamID,
                AwayTeamID = match.AwayTeamID,
                MatchDate = match.MatchDate,
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore,
                Status = match.Status.ToString(),
                HomeTeam = new WebTeamDto
                {
                    TeamID = match.HomeTeam.TeamID,
                    Name = match.HomeTeam.Name,
                    CityID = match.HomeTeam.CityID
                },
                AwayTeam = new WebTeamDto
                {
                    TeamID = match.AwayTeam.TeamID,
                    Name = match.AwayTeam.Name,
                    CityID = match.AwayTeam.CityID
                }
            };
            var matchSquads = await _context.MatchSquads
     .Where(ms => ms.MatchID == matchId)
     .Include(ms => ms.Player)
     .Select(ms => new WebMatchSquadDto
     {
         MatchSquadID = ms.MatchSquadID,
         MatchID = ms.MatchID,
         PlayerID = ms.PlayerID,
         TeamID = ms.TeamID,
         IsStarting11 = ms.IsStarting11,
         IsSubstitute = ms.IsSubstitute,
         ShirtNumber = ms.ShirtNumber,
         TopPosition = ms.TopPosition,
         LeftPosition = ms.LeftPosition,
         PlayerName = ms.Player.FirstName + " " + ms.Player.LastName,
         Position = ms.Player.Position,
         Icon = ms.Player.Icon
     })
     .ToListAsync();
            return new WebMatchDetailDto
            {
                Match = matchDto,
                Goals = goals,
                Cards = cards,
                Formations = formations,
                MatchSquads = matchSquads
            };
        }

    }
}