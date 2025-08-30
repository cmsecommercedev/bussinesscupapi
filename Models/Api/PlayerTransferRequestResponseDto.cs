namespace BussinessCupApi.Models.Api
{
    
        public class PlayerTransferRequestResponseDto
        {
            public int PlayerTransferRequestID { get; set; }
            public string PlayerName { get; set; }
            public string UserID { get; set; }
            public string RequestedCaptainName { get; set; }
            public string RequestedCaptainUserID { get; set; }
            public string ApprovalCaptainName { get; set; }
            public string ApprovalCaptainUserID { get; set; }
            public bool Approved { get; set; }
            public DateTime RequestDate { get; set; }
            public DateTime? ApprovalDate { get; set; }
        public bool Rejected { get; set; }
        public DateTime? RejectionDate { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
            public int? ApprovalTeamId { get; set; }
            public string ApprovalTeamName { get; set; }
            public int? RequestedTeamId { get; set; }
            public string RequestedTeamName { get; set; }
        public bool ButtonShow { get; set; }
    }
     
}
