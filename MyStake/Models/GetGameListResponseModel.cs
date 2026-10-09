namespace MyStake.Models
{
    public class GetGameListResponseModel : Base
    {
        public List<Game> gamelist { get; set; }
        public string providerCode { get; set; }
        public string type { get; set; }
    }
}
