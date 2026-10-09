using Titan.Repository;
using MyStake.Services.Messaging.IntegratedApi;
using Titan.Repository.Database;

namespace MyStake.Services.Interfaces
{
    public interface IClientService
    {
        //Task<User> GetCustomerFromAspNetUserId(string aspNetUserId);
        Task<User> GetCustomerFromUserCode(string userCode);
        UpdateCustomerBalanceResponse UpdateCustomerBalance(UpdateCustomerBalanceRequest request);
    }
}
