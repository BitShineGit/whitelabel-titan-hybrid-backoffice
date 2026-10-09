namespace Whitelabel_backoffice.Models.Requests
{
    public class UpdatePlayerProfileRequest
    {
        public string UserCode { get; set; }

        public byte Status { get; set; }

        public string NickName { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public int CurrencyId { get; set; }
    }
}
