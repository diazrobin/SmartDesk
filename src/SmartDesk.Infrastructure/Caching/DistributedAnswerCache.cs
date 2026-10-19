using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Assistance;

namespace SmartDesk.Infrastructure.Caching;

/// <summary>Caché sobre IDistributedCache (Redis en producción). Degrada sin romper la petición si el almacén falla.</summary>
internal sealed class DistributedAnswerCache(IDistributedCache cache, ILogger<DistributedAnswerCache> logger) : IAssistCache
{
    public async Task<AssistResponse?> GetAsync(string key, CancellationToken ct)
    {
        try
        {
            var bytes = await cache.GetAsync(key, ct);
            return bytes is null ? null : JsonSerializer.Deserialize<AssistResponse>(bytes);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Caché no disponible al leer; se recalcula la respuesta.");
            return null;
        }
    }

    public async Task SetAsync(string key, AssistResponse response, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(response);
            await cache.SetAsync(key, bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Caché no disponible al escribir; la respuesta no se guarda.");
        }
    }
}
