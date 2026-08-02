namespace ServiceDesk.Api.Services
{
    public class TokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
