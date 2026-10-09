using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whitelabel_backoffice.Services.Messaging.IntegratedApi
{
    public class UpdateCustomerBalanceResponse : BaseResponse
    {
        public decimal Balance { get; set; }
    }
}
