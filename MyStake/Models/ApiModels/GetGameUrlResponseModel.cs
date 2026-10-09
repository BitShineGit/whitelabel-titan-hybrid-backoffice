namespace MyStake.Models.ApiModels
{
    public class GetGameUrlResponseModel : BaseResponseModel
    {
        public string gameUrl { get; set; }

        public string providerCode { get; set; }

        public string gameCode { get; set; }
    }
}
