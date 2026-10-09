using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStake.Services.Messaging.IntegratedApi
{
    public class GetGameDetailResponse : BaseResponse
    {
        public GameResponse Message { get; set; }
    }
}
