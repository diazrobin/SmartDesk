using Microsoft.Extensions.DependencyInjection;
using SmartDesk.Application.Abstractions;
using SmartDesk.Infrastructure.Caching;

namespace SmartDesk.Infrastructure;

public static class CacheRegistration
{
    /// <summary>Con Redis configurado usa Redis; sin él, caché en memoria (válida para desarrollo).</summary>
    public static IServiceCollection AddAnswerCache(this IServiceCollection services, string? redisConnection)
    {
        if (string.IsNullOrWhiteSpace(redisConnection))
            services.AddDistributedMemoryCache();
        else
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        services.AddScoped<IAssistCache, DistributedAnswerCache>();
        return services;
    }
}
