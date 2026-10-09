namespace Whitelabel_backoffice.Constants.Enums
{
    public enum ErrorCode
    {
        NO_ERROR = 0,
        USER_ALREADY_EXISTS = 1,
        USER_DOES_NOT_EXIST = 2,
        INSUFFICIENT_AGENT_BALANCE = 3,
        INSUFFICIENT_USER_BALANCE = 4,
        NO_BETTING_LOG_EXIST = 5,
        UNAUTHORIZED = 401,
        UNKNOWN_SERVER_ERROR = 500,
    }
}
