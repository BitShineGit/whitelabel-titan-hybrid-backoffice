using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStake.Services.Messaging.Common.Client
{
    public class UpdateCreditBettingLogReuqest
    {
        public int CustomerId { get; set; }
        public string VendorCode { get; set; }
        public string GameCode { get; set; }
        public string Detail { get; set; }
        public Decimal Amount { get; set; }
        public Decimal AfterBalance { get; set; }
        public string RoundId { get; set; }
        public int WagerId { get; set; }
    }
}
