using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MOE_System.EService.Application.Interfaces;

namespace MOE_System.EService.Infrastructure.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var value = _cache.Get<T>(key);
                return Task.FromResult(value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Memory cache GET failed for key: {Key}", key);
                return Task.FromResult<T?>(null);
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var options = new MemoryCacheEntryOptions();
                
                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                }

                _cache.Set(key, value, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Memory cache SET failed for key: {Key}", key);
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            try
            {
                _cache.Remove(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Memory cache REMOVE failed for key: {Key}", key);
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            try
            {
                return Task.FromResult(_cache.TryGetValue(key, out _));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Memory cache EXISTS check failed for key: {Key}", key);
                return Task.FromResult(false);
            }
        }
    }
}
