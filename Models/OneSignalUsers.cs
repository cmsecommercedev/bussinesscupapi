using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace BussinessCupApi.Models
{
    public class OneSignalUsers
    {
        [Key]
        public int OneSignalUserID { get; set; }

        [Required]
        [StringLength(100)]
        public string ExternalID { get; set; }
        public string? UserKey { get; set; }
 
    }
}