namespace Whitelabel_backoffice.Models
{
    public class AppSettings
    {
        public AppSettings()
        {
            IsTest = true;
            ApiCode = "";
            MainDomain = "";
        }
        public bool IsTest { get; set; }
        public string ApiCode { get; set; }
        public string MainDomain { get; set; }
    }
}
