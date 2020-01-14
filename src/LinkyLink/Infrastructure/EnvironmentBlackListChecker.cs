using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinkyLink.Infrastructure
{

    public class EnvironmentBlackListChecker : IBlackListChecker
    {
        private readonly string[] _blackList;

        public EnvironmentBlackListChecker(string key = "URL_BLACKLIST")
        {
            string settingsValue = Environment.GetEnvironmentVariable(key);
            this._blackList = settingsValue != null ? settingsValue.Split(',') : new string[] { };
        }

        public Task<bool> Check(string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            return Task.FromResult(_blackList.Any()? _blackList.Contains(value): true);
        }
    }
}