using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whitelabel_backoffice.Services.Messaging.IntegratedApi
{
    public class GetLaunchUrlRequest
    {
        public GetLaunchUrlRequest()
        {
            LobbyUrl = string.Empty;
        }
        public string ApiCode { get; set; }
        public string VendorCode { get; set; }
        public string GameCode { get; set; }
        public string UserCode { get; set; }
        public string Language { get; set; }
        public string Currency { get; set; }
        public string? LobbyUrl { get; set; }
    }
}
