namespace Whitelabel_backoffice.Models
{
    public class BaseRequestModel
    {
        public BaseRequestModel()
        {
            lang = "en";
        }
        public string lang { get; set; }
    }
}