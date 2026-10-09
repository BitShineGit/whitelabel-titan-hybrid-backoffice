namespace Whitelabel_backoffice.Models.Requests
{
    public class PlayerDataTableRequest
    {
        public string? SearchType { get; set; }

        public bool Active { get; set; }

        public bool Blocked { get; set; }

        public decimal? BalanceMin { get; set; }

        public decimal? BalanceMax { get; set; }

        public DateTime? CreatedFrom { get; set; }

        public DateTime? CreatedTo { get; set; }
    }
}
