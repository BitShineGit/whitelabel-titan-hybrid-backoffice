namespace MyStake.Services.Exceptions
{
    public class CustomerNotFoundException : Exception
    {
        public CustomerNotFoundException(string aspNetUserId)
           : base(string.Format("Customer {0} not found", aspNetUserId))
        {

        }
    }
}
