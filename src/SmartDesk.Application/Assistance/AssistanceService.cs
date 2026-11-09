using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Knowledge;
using SmartDesk.Application.Tickets;

namespace SmartDesk.Application.Assistance;

/// <summary>Distingue "no hay contenido" de "la IA falló".</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnswerStatus { Generated, NoRelevantSources, Unavailable }

/// <summary>Respuesta sugerida, artículos en los que se basa y estado de la generación.</summary>
public sealed record AssistResponse(string? Answer, IReadOnlyList<KnowledgeMatch> Sources, AnswerStatus Status);

/// <summary>RAG con caché y estado explícito: solo se cachean resultados completos, nunca fallos.</summary>
public sealed class AssistanceService(TicketService tickets, IKnowledgeBase knowledge, IAnswerGenerator answers, IAssistCache cache)
{
    /// <summary>Por debajo de este parecido, el artículo no se considera relevante (valor inicial, pendiente de calibrar).</summary>
    public const double MinSimilarity = 0.70;

    /// <summary>Tiempo máximo que una respuesta sigue en caché tras los cambios en la base de conocimiento.</summary>
    public static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<AssistResponse?> AssistAsync(Guid ticketId, CancellationToken ct)
    {
        // La visibilidad se comprueba SIEMPRE antes de mirar la caché
        var ticket = await tickets.GetAsync(ticketId, ct);
        if (ticket is null) return null;

        var query = $"{ticket.Title}\n{ticket.Description}";
        var key = "assist:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(query)));
        if (await cache.GetAsync(key, ct) is { } cached) return cached;

        // 1. Búsqueda: si la IA falla, no hay "sin resultados" real, así que no se cachea
        IReadOnlyList<KnowledgeMatch> matches;
        try
        {
            matches = await knowledge.SearchAsync(query, 3, ct);
        }
        catch (AiUnavailableException)
        {
            return new AssistResponse(null, [], AnswerStatus.Unavailable);
        }

        var relevant = matches.Where(m => m.Similarity >= MinSimilarity).ToList();

        // 2. Sin contexto fiable no llamamos a la IA: evitamos respuestas inventadas
        if (relevant.Count == 0)
        {
            var empty = new AssistResponse(null, relevant, AnswerStatus.NoRelevantSources);
            await cache.SetAsync(key, empty, CacheTtl, ct);
            return empty;
        }

        // 3. Redacción: si la IA falla, devolvemos las fuentes sin respuesta y sin cachear
        string? answer;
        try
        {
            answer = await answers.GenerateAsync(ticket.Title, ticket.Description, relevant, ct);
        }
        catch (AiUnavailableException)
        {
            return new AssistResponse(null, relevant, AnswerStatus.Unavailable);
        }

        if (answer is null)
            return new AssistResponse(null, relevant, AnswerStatus.Unavailable);

        var response = new AssistResponse(answer, relevant, AnswerStatus.Generated);
        await cache.SetAsync(key, response, CacheTtl, ct);
        return response;
    }
}
