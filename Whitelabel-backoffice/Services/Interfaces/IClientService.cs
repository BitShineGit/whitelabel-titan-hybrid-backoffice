using Whitelabel_backoffice.Database;
using Whitelabel_backoffice.Services.Messaging.IntegratedApi;

namespace Whitelabel_backoffice.Services.Interfaces
{
    public interface IClientService
    {
        //Task<User> GetCustomerFromAspNetUserId(string aspNetUserId);
        Task<User> GetCustomerFromUserCode(string userCode);
        UpdateCustomerBalanceResponse UpdateCustomerBalance(UpdateCustomerBalanceRequest request);
    }
}
