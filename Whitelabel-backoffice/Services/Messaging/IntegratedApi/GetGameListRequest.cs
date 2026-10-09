using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whitelabel_backoffice.Services.Messaging.IntegratedApi
{
    public class GetGameListRequest
    {
        public GetGameListRequest() { 
            Language = "en";
        }
        [JsonProperty(PropertyName = "vendorCode")]
        public string VendorCode { get; set; }
        [JsonProperty(PropertyName = "apiCode")]
        public string ApiCode { get; set; }
        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; }

    }
}
