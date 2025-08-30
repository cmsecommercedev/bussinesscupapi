using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BussinessCupApi.Models
{
    public class PlayerTransferRequest
    {
        public int PlayerTransferRequestID { get; set; }

        [Required]
        public string UserID { get; set; } 

        [Required]
        public string RequestedCaptainUserID { get; set; } 

        public string ApprovalCaptainUserID { get; set; } 

        public bool Approved { get; set; }

        public DateTime RequestDate { get; set; }

        public DateTime? ApprovalDate { get; set; }
        public bool Rejected { get; set; } = false;
        public DateTime? RejectionDate { get; set; }
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
        [ForeignKey("RequestedCaptainUserID")]
        public virtual User RequestedCaptainUser { get; set; }
        [ForeignKey("ApprovalCaptainUserID")]
        public virtual User ApprovalCaptainUser { get; set; }
    }
}