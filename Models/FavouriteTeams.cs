using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class FavouriteTeams
    {
        [Key]
        public int FavouriteTeamID { get; set; }

        [Required]
        public int TeamID { get; set; }
        [Required]
        public string UserToken { get; set; }
        public string MacID { get; set; }
    }
}