namespace MyStake.Models.ApiModels
{
    public class GetGameUrlRequestModel:BaseRequestModel
    {
        public string providerCode { get; set; }
        public string gameCode { get; set; }
    }
}
