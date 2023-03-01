namespace FINE.Service.DTO.Request
{
    public class ExternalAuthRequest
    {
        public string? IdToken { get; set; }
        public string FcmToken { get; set; } = "";
    }
}
