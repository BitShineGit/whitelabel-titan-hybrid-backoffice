using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStake.Services.Messaging.Common.Client
{
    public class CreateClientResponse
    {
        public string DiscountCode { get; set; }
        public int CustomerId { get; set; }
    }
}
