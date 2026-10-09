namespace MyStake.Models
{
    public class GenerateCheckoutUrlRequest
    {
        public string UserCode { get; set; }
        public decimal Amount { get; set; }
    }
}
