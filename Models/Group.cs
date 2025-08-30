namespace BussinessCupApi.Models
{
    public class Group
    {
        public int GroupID { get; set; }
        public int LeagueID { get; set; }
        public int SeasonID { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
         
        public virtual ICollection<Match> Matches { get; set; }
        
        public Group()
        {
            Matches = new HashSet<Match>();
        }
    }
} 