using System.Threading.Tasks;

namespace LinkyLink.Infrastructure
{
    public interface IBlackListChecker
    {
        Task<bool> Check(string value);
    }
}