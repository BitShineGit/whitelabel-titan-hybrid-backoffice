namespace MyStake.Models
{
    public class CreateWithdrawRequest
    {
        public string UserCode { get; set; }
        public decimal Amount { get; set; }
    }
}
