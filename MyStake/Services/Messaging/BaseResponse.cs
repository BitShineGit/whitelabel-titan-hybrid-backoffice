

using MyStake.Constants.Enums;

namespace MyStake.Services.Messaging
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
