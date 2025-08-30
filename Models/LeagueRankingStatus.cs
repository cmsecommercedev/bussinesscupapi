using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class LeagueRankingStatus
    {
        [Key]
        public int LeagueRankingStatusID { get; set; }
         
        public int LeagueID { get; set; } 

        public int OrderNo { get; set; }

        [MaxLength(10)]
        public string ColorCode { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }
    }
}