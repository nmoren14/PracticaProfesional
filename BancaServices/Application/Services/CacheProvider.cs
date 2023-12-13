using Microsoft.Extensions.Caching.Memory;
using System;
using BancaServices.Domain.Interfaces;

namespace BancaServices.Application.Services
{
    public class CacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _memoryCache;

        public CacheProvider(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public TItem Get<TItem>(string key)
        {
            if (_memoryCache.TryGetValue(key, out TItem value))
            {
                return value;
            }

            return default;
        }

        public void Set<TItem>(string key, TItem value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration,
                SlidingExpiration = slidingExpiration,
            };

            _memoryCache.Set(key, value, cacheEntryOptions);
        }


        public void Remove(string key)
        {
            _memoryCache.Remove(key);
        }
    }

}
