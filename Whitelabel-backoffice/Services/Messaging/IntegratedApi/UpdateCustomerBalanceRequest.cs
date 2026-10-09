using Whitelabel_backoffice.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Whitelabel_backoffice.Services.Messaging.IntegratedApi
{
    public class UpdateCustomerBalanceRequest
    {
        public User Customer { get; set; }
        public decimal ChangeAmount { get; set; }
    }
}
