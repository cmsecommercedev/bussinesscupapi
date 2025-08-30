using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace BussinessCupApi.Models
{
    public class City
    {
        [Key]
        public int CityID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public int Order { get; set; }

        // İstersen liglerle ilişkiyi de ekleyebilirsin
        public virtual ICollection<League> Leagues { get; set; }
    }
}