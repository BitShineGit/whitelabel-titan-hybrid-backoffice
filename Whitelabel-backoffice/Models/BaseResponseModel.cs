using Whitelabel_backoffice.Constants.Enums;

namespace Whitelabel_backoffice.Models
{
    public class BaseResponseModel
    {
        public bool success { get; set; }
        public ErrorCode errorCode { get; set; }
    }
}
