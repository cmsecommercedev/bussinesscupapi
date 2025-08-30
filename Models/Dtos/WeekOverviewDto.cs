using Controllers.Api;

public class WeekOverviewDto
{
    public int WeekID { get; set; }
    public int WeekNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int SeasonID { get; set; }
    public List<GroupMatchesDto> GroupedMatches { get; set; } = new List<GroupMatchesDto>();
    public List<MatchOverviewDto> UngroupedMatches { get; set; } = new List<MatchOverviewDto>();
}

public class GroupMatchesDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public List<MatchOverviewDto> Matches { get; set; } = new List<MatchOverviewDto>();
} 