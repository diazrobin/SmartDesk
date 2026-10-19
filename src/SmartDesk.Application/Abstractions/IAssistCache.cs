using SmartDesk.Application.Assistance;

namespace SmartDesk.Application.Abstractions;

/// <summary>Caché de respuestas sugeridas. Es una optimización: su fallo no debe impedir responder.</summary>
public interface IAssistCache
{
    Task<AssistResponse?> GetAsync(string key, CancellationToken ct);
    Task SetAsync(string key, AssistResponse response, TimeSpan ttl, CancellationToken ct);
}
