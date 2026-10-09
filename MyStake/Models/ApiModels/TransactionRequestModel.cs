namespace MyStake.Models.ApiModels
{
    public class TransactionRequestModel:BaseRequestModel
    {
        public string userCode { get; set; }

        public int type { get; set; }

        public decimal amount { get; set; }
    }
}
