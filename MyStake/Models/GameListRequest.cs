namespace MyStake.Models
{
    public class GameListRequest
    {
        public string ProviderCode { get; set; } = string.Empty;
        public int Category { get; set; } = 0;
        public string type { get; set; } = string.Empty;
    }
}
