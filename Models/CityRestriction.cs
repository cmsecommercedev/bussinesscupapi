using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class CityRestriction
    {
        [Key]
        public int CityRestrictionID { get; set; }
         
        public int CityID { get; set; } 

        public bool IsTransferBanned { get; set; }
        public bool IsRegistrationStopped { get; set; }
    }
}