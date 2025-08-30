//using Bogus;
//using BussinessCupApi.Data;
//using BussinessCupApi.Models;
//using BussinessCupApi.Models.UserPlayerTypes;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;

//namespace BussinessCupApi
//{
//    public static class DummyDataSeeder
//    {
//        public static void Seed(ApplicationDbContext context, UserManager<User> userManager)
//        {
//            var faker = new Faker("tr");

//            // 1. Cities
//            var cities = new List<City>();
//            for (int i = 0; i < 4; i++)
//                cities.Add(new City { Name = faker.Address.City() });
//            context.City.AddRange(cities);
//            context.SaveChanges();

//            // 2. Leagues per City
//            var leagues = new List<League>();
//            foreach (var city in cities)
//            {
//                leagues.Add(new League
//                {
//                    Name = faker.Company.CompanyName() + " Ligi",
//                    LeagueType = LeagueType.League,
//                    StartDate = faker.Date.Past(1),
//                    EndDate = DateTime.Now.AddMonths(4),
//                    IsActive = true,
//                    LogoPath = faker.Image.PicsumUrl(),
//                    CityID = city.CityID,
//                    TeamSquadCount = 7
//                });
//            }
//            context.Leagues.AddRange(leagues);
//            context.SaveChanges();

//            // 3. Seasons per League
//            var seasons = new List<Season>();
//            foreach (var league in leagues)
//            {
//                seasons.Add(new Season
//                {
//                    Name = $"{faker.Date.Past(1).Year} Sezonu",
//                    LeagueID = league.LeagueID
//                });
//            }
//            context.Season.AddRange(seasons);
//            context.SaveChanges();

//            // 4. Teams per City
//            var teams = new List<Team>();
//            foreach (var city in cities)
//            {
//                for (int i = 0; i < 5; i++)
//                {
//                    teams.Add(new Team
//                    {
//                        Name = faker.Company.CompanyName(),
//                        Stadium = faker.Address.State(),
//                        LogoUrl = faker.Image.PicsumUrl(),
//                        Manager = faker.Name.FullName(),
//                        CityID = city.CityID
//                    });
//                }
//            }
//            context.Teams.AddRange(teams);
//            context.SaveChanges();

//            var players = new List<Player>();

//            // 5. Players per Team, and Users per Player
//            foreach (var team in teams)
//            {
//                players = new List<Player>();
//                for (int i = 0; i < 11; i++)
//                {
//                    // Create user for player
//                    var user = new User
//                    {
//                        UserName = faker.Internet.UserName(),
//                        Email = faker.Internet.Email(),
//                        UserType = UserType.Player,
//                        UserRole = "Player",
//                        Firstname = faker.Name.FirstName(),
//                        Lastname = faker.Name.LastName(),
//                        CityID = team.CityID,
//                        UserKey = Guid.NewGuid().ToString()
//                    };
//                    userManager.CreateAsync(user, "123456").Wait();

//                    var player = new Player
//                    {
//                        FirstName = faker.Name.FirstName(),
//                        LastName = faker.Name.LastName(),
//                        Position = faker.PickRandom("GK", "DF", "MF", "FW"),
//                        Number = faker.Random.Int(1, 99),
//                        DateOfBirth = faker.Date.Past(20, DateTime.Now.AddYears(-18)),
//                        Nationality = faker.Address.Country(),
//                        IdentityNumber = faker.Random.Replace("###########"),
//                        Icon = faker.Internet.Avatar(),
//                        isArchived = false,
//                        TeamID = team.TeamID,
//                        UserId = user.Id
//                    };
//                    players.Add(player);
//                }
//                context.Players.AddRange(players);
//                context.SaveChanges();
//            }


//            var weeks = new List<Week>();

//            // 6. Weeks per League/Season
//            foreach (var league in leagues)
//            {
//                var leagueSeasons = seasons.Where(s => s.LeagueID == league.LeagueID).ToList();
//                foreach (var season in leagueSeasons)
//                {
//                    for (int w = 1; w <= 5; w++)
//                    {
//                        weeks.Add(new Week
//                        {
//                            LeagueID = league.LeagueID,
//                            SeasonID = season.SeasonID,
//                            WeekNumber = w,
//                            StartDate = league.StartDate.AddDays((w - 1) * 7),
//                            EndDate = league.StartDate.AddDays(w * 7 - 1),
//                            IsCompleted = false,
//                            WeekName = $"Hafta {w}",
//                            WeekStatus = "League"
//                        });
//                    }
//                }
//            }
//            context.Weeks.AddRange(weeks);
//            context.SaveChanges();

//            // 7. Matches per Week
//            var matches = new List<Match>();
//            var rnd = new Random();
//            foreach (var week in weeks)
//            {
//                var leagueTeams = teams.Where(t => t.CityID == leagues.First(l => l.LeagueID == week.LeagueID).CityID).ToList();
//                var matchups = new HashSet<(int, int)>();
//                int matchCount = leagueTeams.Count / 2; // Each team plays once per week

//                for (int i = 0; i < matchCount; i++)
//                {
//                    int homeIdx, awayIdx;
//                    do
//                    {
//                        homeIdx = rnd.Next(leagueTeams.Count);
//                        awayIdx = rnd.Next(leagueTeams.Count);
//                    } while (homeIdx == awayIdx || matchups.Contains((leagueTeams[homeIdx].TeamID, leagueTeams[awayIdx].TeamID)));

//                    matchups.Add((leagueTeams[homeIdx].TeamID, leagueTeams[awayIdx].TeamID));

//                    matches.Add(new Match
//                    {
//                        LeagueID = week.LeagueID,
//                        WeekID = week.WeekID,
//                        HomeTeamID = leagueTeams[homeIdx].TeamID,
//                        AwayTeamID = leagueTeams[awayIdx].TeamID,
//                        MatchDate = week.StartDate.AddDays(rnd.Next(0, 7)),
//                        MatchStarted = week.StartDate.AddDays(rnd.Next(0, 7)),
//                        HomeScore = rnd.Next(0, 5),
//                        AwayScore = rnd.Next(0, 5),
//                        MatchURL = faker.Internet.Url(),
//                        IsPlayed = true,
//                        Status = Match.MatchStatus.Finished
//                    });
//                }
//            }
//            context.Matches.AddRange(matches);
//            context.SaveChanges();

//            // 8. News per City
//            var newsList = new List<MatchNews>();
//            foreach (var city in cities)
//            {
//                for (int i = 0; i < 3; i++)
//                {
//                    newsList.Add(new MatchNews
//                    {
//                        Title = faker.Lorem.Sentence(5, 3),
//                        Subtitle = faker.Lorem.Sentence(8, 5),
//                        MatchNewsMainPhoto = faker.Image.PicsumUrl(),
//                        DetailsTitle = faker.Lorem.Sentence(6, 2),
//                        Details = faker.Lorem.Paragraphs(2),
//                        Published = faker.Random.Bool(0.8f),
//                        CreatedDate = faker.Date.Past(1),
//                        CityID = city.CityID,
//                        Photos = new List<MatchNewsPhoto>
//                        {
//                            new MatchNewsPhoto { PhotoUrl = faker.Image.PicsumUrl() }
//                        }
//                    });
//                }
//            }
//            context.MatchNews.AddRange(newsList);
//            context.SaveChanges();

//            // 9. Goals and Cards per Match
//            var goals = new List<Goal>();
//            var cards = new List<Card>();
//            foreach (var match in matches)
//            {
//                for (int i = 0; i < 3; i++)
//                {
//                    var team = teams.First(t => t.TeamID == match.HomeTeamID || t.TeamID == match.AwayTeamID);
//                    var player = players.First(p => p.TeamID == team.TeamID);
//                    goals.Add(new Goal
//                    {
//                        MatchID = match.MatchID,
//                        TeamID = team.TeamID,
//                        PlayerID = player.PlayerID,
//                        AssistPlayerID = null,
//                        Minute = faker.Random.Int(1, 90),
//                        IsPenalty = faker.Random.Bool(0.1f),
//                        IsOwnGoal = faker.Random.Bool(0.05f)
//                    });
//                    cards.Add(new Card
//                    {
//                        PlayerID = player.PlayerID,
//                        MatchID = match.MatchID,
//                        CardType = CardType.Yellow,
//                        Minute = faker.Random.Int(1, 90),
//                        MatchBan = faker.Random.Int(0, 3)
//                    });
//                }
//            }
//            context.Goals.AddRange(goals);
//            context.Cards.AddRange(cards);
//            context.SaveChanges();
//        }
//    }
//}
