using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class FavouritePlayers
    {
        [Key]
        public int FavouritePlayerID { get; set; }

        [Required]
        public int PlayerID { get; set; }
        [Required]
        public string UserToken { get; set; }
        public string MacID { get; set; }
        public string? Culture { get; set; } // eklendi
    }
}