namespace MyStake.Models
{
    public class AppSettings
    {
        public AppSettings()
        {
            IsTest = true;
            ApiCode = "";
            MainDomain = "";
            BackOfficeDomain = "";
        }
        public bool IsTest { get; set; }
        public string ApiCode { get; set; }
        public string MainDomain { get; set; }
        public string BackOfficeDomain { get; set; }
    }
}
