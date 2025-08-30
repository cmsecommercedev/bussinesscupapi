namespace BussinessCupApi.DTOs.Web
{
    public class WebCityDto
    {
        public int CityID { get; set; }
        public string Name { get; set; }
    }
    public class WebLeagueDto
    {
        public int LeagueID { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public string? LogoPath { get; set; }
        public int CityID { get; set; }
        public int TeamSquadCount { get; set; }
    }

    public class WebMatchNewsDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string? MatchNewsMainPhoto { get; set; }
        public string DetailsTitle { get; set; }
        public string Details { get; set; }
        public int? CityID { get; set; }
        public bool IsMainNews { get; set; }
        public bool Published { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<WebMatchNewsPhotoDto> Photos { get; set; }
    }
    public class WebMatchNewsPhotoDto
    {
        public int Id { get; set; }
        public string? PhotoUrl { get; set; }
    }
    public class WebTeamDto
    {
        public int TeamID { get; set; }
        public string Name { get; set; }
        public int CityID { get; set; }
        public string? LogoUrl { get; set; }
        public string? Manager { get; set; }
    }
    public class WebWeekDto
    {
        public int WeekID { get; set; }
        public int LeagueID { get; set; }
        public int WeekNumber { get; set; }
        public string WeekName { get; set; }
        public DateTime StartDate { get; set; }
    }
    public class WebMatchDto
    {
        public int MatchID { get; set; }
        public int LeagueID { get; set; }
        public int WeekID { get; set; }
        public int? GroupID { get; set; }
        public int HomeTeamID { get; set; }
        public int AwayTeamID { get; set; }
        public DateTime MatchDate { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string? Status { get; set; }
        public WebTeamDto HomeTeam { get; set; }
        public WebTeamDto AwayTeam { get; set; }
    }

    public class WebPlayerDto
    {
        public int PlayerID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Position { get; set; }
        public int? Number { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public string? Icon { get; set; }
    }
     public class WebActualWeekMatchesDto
    {
        public string LeagueName { get; set; }
        public string WeekName { get; set; }
        public int WeekID { get; set; }
        public List<WebMatchDto> Matches { get; set; }
    }
    public class WebMatchSquadDto
    {
        public int MatchSquadID { get; set; }
        public int MatchID { get; set; }
        public int PlayerID { get; set; }
        public int TeamID { get; set; }
        public bool IsStarting11 { get; set; }
        public bool IsSubstitute { get; set; }
        public int? ShirtNumber { get; set; }
        public float? TopPosition { get; set; }
        public float? LeftPosition { get; set; }
        public string PlayerName { get; set; }
        public string? Position { get; set; }
        public string? Icon { get; set; }
    }
    public class WebMatchDetailDto
    {
        public WebMatchDto Match { get; set; }
        public List<WebGoalDto> Goals { get; set; }
        public List<WebCardDto> Cards { get; set; }
        public List<WebFormationDto> Formations { get; set; }
        public List<WebMatchSquadDto> MatchSquads { get; set; }
    }

    public class WebGoalDto
    {
        public int GoalID { get; set; }
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public int TeamID { get; set; }
        public string TeamName { get; set; }
        public int Minute { get; set; }
        public bool IsPenalty { get; set; }
        public bool IsOwnGoal { get; set; }
        public int? AssistPlayerID { get; set; }
    }

    public class WebCardDto
    {
        public int CardID { get; set; }
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public string CardType { get; set; }
        public int Minute { get; set; }
    }

    public class WebFormationDto
    {
        public int TeamID { get; set; }
        public string? FormationImage { get; set; }
    }

}