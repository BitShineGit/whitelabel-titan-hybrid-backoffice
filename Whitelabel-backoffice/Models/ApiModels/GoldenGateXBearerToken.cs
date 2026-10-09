namespace Whitelabel_backoffice.Models.ApiModels
{
    public class GoldenGateXBearerToken
    {
        public string Token { get; set; }
        public long Expiration { get; set; }
        public bool IsExpired
        {
            get
            {
                return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds > Expiration;
            }
        }
    }
}
