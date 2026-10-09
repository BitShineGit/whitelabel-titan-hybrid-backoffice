using Microsoft.EntityFrameworkCore;
using Titan.Repository;
using Titan.Repository.Database;

namespace MyStake.Services
{
    public class SettingService : ISettingService
    {
        private readonly IWhitelabelDBStorage _dbStorage;

        public SettingService(IWhitelabelDBStorage dbStorage)
        {
            _dbStorage = dbStorage;
        }

        #region Setting

        private async Task<Setting?> GetSettingValueAsync(
            string category,
            string key)
        {
            return await _dbStorage
                .Context
                .Settings
                .FirstOrDefaultAsync(s =>
                    s.Category == category &&
                    s.Key == key);
        }

        public async Task<bool> GetBooleanValueAsync(
            string category,
            string key)
        {
            var setting = await GetSettingValueAsync(category, key);

            if (setting == null || string.IsNullOrEmpty(setting.Value))
                return false;

            return setting.Value == "1" ||
                   setting.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> GetStringValueAsync(
            string category,
            string key)
        {
            var setting = await GetSettingValueAsync(category, key);

            if (setting == null || string.IsNullOrEmpty(setting.Value))
                return string.Empty;

            return setting.Value;
        }

        public async Task<int> GetIntValueAsync(
            string category,
            string key)
        {
            var setting = await GetSettingValueAsync(category, key);

            if (setting == null || string.IsNullOrEmpty(setting.Value))
                return 0;

            return int.TryParse(setting.Value, out var value)
                ? value
                : 0;
        }

        public async Task<double> GetDoubleValueAsync(
            string category,
            string key)
        {
            var setting = await GetSettingValueAsync(category, key);

            if (setting == null || string.IsNullOrEmpty(setting.Value))
                return 0;

            return double.TryParse(
                setting.Value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value)
                ? value
                : 0;
        }

        public async Task<List<string>> GetStringValueListAsync(
            string category,
            string key)
        {
            var setting = await GetSettingValueAsync(category, key);

            if (setting == null || string.IsNullOrEmpty(setting.Value))
                return new List<string>();

            return setting.Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();
        }

        public async Task<List<int>> GetIntValueListAsync(
            string category,
            string key)
        {
            var setting = await GetSettingValueAsync(category, key);

            if (setting == null || string.IsNullOrEmpty(setting.Value))
                return new List<int>();

            return setting.Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x.Trim(), out var value) ? value : 0)
                .ToList();
        }

        #endregion
    }
}