namespace FINE.Service.DTO.Request
{
    public class ExternalAuthRequest
    {
        public bool IsPhone { get; set; }
        public string? IdToken { get; set; }
        public string FcmToken { get; set; } = "";
    }
}
