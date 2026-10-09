namespace MyStake.Models
{
    public class Game
    {
        public string ProviderCode { get; set; }
        public string GameCode { get; set; }
        public string GameName { get; set; }
        public string Thumbnail { get; set; }
        public decimal HotRate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsNew { get; set; }
        public bool UnderMaintenance { get; set; }

    }
}
