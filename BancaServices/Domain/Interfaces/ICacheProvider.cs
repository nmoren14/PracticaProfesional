using BancaServices;
using Microsoft.Extensions.Caching.Memory;

namespace BancaServices.Domain.Interfaces
{
    public interface ICacheProvider
    {
        TItem Get<TItem>(string key);
        void Set<TItem>(string key, TItem value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null);
        void Remove(string key);

    }

}
