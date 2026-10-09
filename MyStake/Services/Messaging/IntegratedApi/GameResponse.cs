namespace MyStake.Services.Messaging.IntegratedApi
{
    public class GameResponse
    {
        public string Provider { get; set; }
        public string VendorCode { get; set; }
        public string GameCode { get; set; } = null!;

        public string GameName { get; set; } = null!;

        public string Slug { get; set; } = null!;

        public string Thumbnail { get; set; } = null!;

        public DateTime UpdatedAt { get; set; }
        public bool IsNew { get; set; }
        public bool UnderMaintenance { get; set; }
    }
}
