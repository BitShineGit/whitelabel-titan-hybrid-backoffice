using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whitelabel_backoffice.Services.Messaging.IntegratedApi
{
    public class GetGameDetailRequest
    {
        public string ApiCode { get; set; }
        public string VendorCode { get; set; }
        public string GameCode { get; set; }
    }
}
