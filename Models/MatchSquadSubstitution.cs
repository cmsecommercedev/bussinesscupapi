using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BussinessCupApi.Models
{
    

    public class MatchSquadSubstitution
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MatchID { get; set; }

        [Required]
        public int PlayerInID { get; set; }

        [Required]
        public int PlayerOutID { get; set; }

        [Required]
        public int Minute { get; set; }

        // Navigation properties
        [ForeignKey("MatchID")]
        public virtual Match Match { get; set; }

        [ForeignKey("PlayerInID")]
        public virtual Player PlayerIn { get; set; }

        [ForeignKey("PlayerOutID")]
        public virtual Player PlayerOut { get; set; }
    }
}
