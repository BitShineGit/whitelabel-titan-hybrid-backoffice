using System.Collections.Generic;
using System.Threading.Tasks;

namespace Whitelabel_backoffice.Services
{
    public interface ISettingService
    {
        Task<bool> GetBooleanValueAsync(string category, string key);

        Task<string> GetStringValueAsync(string category, string key);

        Task<int> GetIntValueAsync(string category, string key);

        Task<double> GetDoubleValueAsync(string category, string key);

        Task<List<string>> GetStringValueListAsync(string category, string key);

        Task<List<int>> GetIntValueListAsync(string category, string key);
    }
}