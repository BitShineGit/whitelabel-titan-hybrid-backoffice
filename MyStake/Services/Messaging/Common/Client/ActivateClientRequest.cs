using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStake.Services.Messaging.Common.Client
{
    public  class ActivateClientRequest
    {
        public string AspNetUserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
    }
}
