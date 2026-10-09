using MyStake.Constants.Enums;
using MyStake.Controllers;
using Titan.Repository;
using MyStake.Models;
using MyStake.Services.Interfaces;
using MyStake.Services.Messaging.IntegratedApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using Titan.Repository.Database;

namespace MyStake.Services.Implementations
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
                    var _response = _dbStorage.Context.Database.SqlQuery<decimal>(
                        $@"UPDATE Users
                           SET CurrentBalance += {request.ChangeAmount},
                           TotalBetAmount += {Math.Abs(request.BetAmount)},
                           TotalWinAmount += {Math.Abs(request.WinAmount)},
                           UpdatedAt = {DateTime.UtcNow}
                           OUTPUT INSERTED.CurrentBalance AS Value
                           WHERE Id = {user.Id}").ToList();
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
