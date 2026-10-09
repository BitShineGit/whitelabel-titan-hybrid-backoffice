using Whitelabel_backoffice.Constants.Enums;
using Whitelabel_backoffice.Database;
using Whitelabel_backoffice.Models;
using Whitelabel_backoffice.Services.Interfaces;
using Whitelabel_backoffice.Services.Messaging.IntegratedApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Whitelabel_backoffice.Services.Implementations
{
    public class ClientService : IClientService
    {
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly ILogger<ClientService> _logger;
        private readonly AppSettings _appSettings = null;
        private readonly object balanceLock = new object();

        public ClientService(ILogger<ClientService> logger, IWhitelabelDBStorage dbStorage, IOptions<AppSettings> appsettings)
        {
            _dbStorage = dbStorage;
            _logger = logger;
            _appSettings = appsettings.Value;
        }
        public async Task<User> GetCustomerFromUserCode(string userCode)
        {
            return await _dbStorage.Context.Users.AsNoTracking().Where(x => x.UserCode == userCode).FirstOrDefaultAsync();
        }

        public UpdateCustomerBalanceResponse UpdateCustomerBalance(UpdateCustomerBalanceRequest request)
        {
            var response = new UpdateCustomerBalanceResponse();
            lock (balanceLock)
            {
                try
                {
                    var user = request.Customer;
                    var _response = _dbStorage.Context.Database.SqlQueryRaw<decimal>($"UPDATE Users SET TotalBalance += {request.ChangeAmount} OUTPUT INSERTED.TotalBalance WHERE Id = {user.Id}").ToList();
                    response.Balance = _response[0];
                }
                catch (Exception e)
                {
                    _logger.LogError($"UpdateCustomerBalance: {e}");
                    response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
                }
            }
            return response;
        }

        //public async Task<User> GetCustomerFromAspNetUserId(string aspNetUserId)
        //{
        //    var customer = await _dbStorage.Context.Users.FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId);
        //    return customer;
        //}
    }
}
