using Titan.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Titan.Repository.Database;

namespace MyStake.Services.Messaging.IntegratedApi
{
    public class UpdateCustomerBalanceRequest
    {
        public User Customer { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal BetAmount { get; set; }
        public decimal WinAmount { get; set; }
    }
}
