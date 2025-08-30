public class GroupInfoViewModel
{
    public int GroupID { get; set; }
    public string GroupName { get; set; }
}

public class LeagueStandingsViewModel
{
    public int LeagueID { get; set; }
    public string LeagueName { get; set; }
    public int SeasonId { get; set; } // Eklendi
    public string SeasonName { get; set; } // 'Season' yerine daha açıklayıcı
    public List<TeamStandingViewModel> Standings { get; set; }
    public List<GroupInfoViewModel> Groups { get; set; } // List<string>'den GroupInfoViewModel listesine dönüştü
    public int? CurrentGroupId { get; set; } // Hangi grubun aktif olduğunu belirtmek için eklendi

}


public class TeamStandingViewModel
{
    public int TeamID { get; set; }
    public string TeamName { get; set; }
    public string TeamIcon { get; set; }
    public string Manager { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public List<string> LastFiveMatches { get; set; }
    public string Group { get; set; }

    public int PenaltyPoints { get; set; }
    public string PenaltyDescription { get; set; }
    public int Points { get; set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;
} 