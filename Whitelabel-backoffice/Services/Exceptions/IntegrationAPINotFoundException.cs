namespace Whitelabel_backoffice.Services.Exceptions
{
    public class IntegrationAPINotFoundException : Exception
    {
        public IntegrationAPINotFoundException(string apiName)
           : base(string.Format("Integration api {0} not found", apiName))
        {

        }
    }
}
