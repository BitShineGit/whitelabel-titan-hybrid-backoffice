using Whitelabel_backoffice.Models;

namespace Whitelabel_backoffice.Models
{
    public class GetProviderListResponseModel : BaseResponseModel
    {
        public List<ProviderModel> message { get; set; }
    }
}
