

using Whitelabel_backoffice.Constants.Enums;

namespace Whitelabel_backoffice.Services.Messaging
{
    [System.Serializable]
    public class BaseResponse
    {
        public BaseResponse()
        {
            ErrorCode = ErrorCode.NO_ERROR;
        }
        public ErrorCode ErrorCode { get; set; }
        public bool Success
        {
            get
            {
                return ErrorCode == ErrorCode.NO_ERROR;
            }
        }
    }
}
