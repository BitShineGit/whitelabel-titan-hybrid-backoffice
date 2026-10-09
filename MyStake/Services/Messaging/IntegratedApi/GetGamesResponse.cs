using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyStake.Services.Messaging.IntegratedApi
{
    public class GetGamesResponse : BaseResponse
    {
        public GetGamesResponse()
        {
            Message = new List<GameResponse>();
        }
        public List<GameResponse> Message { get; set; }
    }
}
