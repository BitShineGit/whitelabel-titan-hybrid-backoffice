namespace MyStake.Models
{
    public class TransactionRequestModel
    {

        public string userCode { get; set; }
        public int type { get; set; }
        public decimal amount { get; set; }

    }
}