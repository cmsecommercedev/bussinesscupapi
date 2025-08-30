namespace BussinessCupApi.Dtos
{
    public class PlayerTransferRequestDto
    {
        // Transfer edilmek istenen oyuncunun UserID'si
        public string PlayerUserID { get; set; }

        // Oyuncunun mevcut takım kaptanının UserID'si (teklifin gönderileceği kaptan)
        public string RequestedCaptainUserID { get; set; }
    }
}