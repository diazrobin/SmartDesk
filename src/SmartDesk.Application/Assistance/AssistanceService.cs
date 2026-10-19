using System.Security.Cryptography;
using System.Text;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Knowledge;
using SmartDesk.Application.Tickets;

namespace SmartDesk.Application.Assistance;

/// <summary>Respuesta sugerida y artículos en los que se basa.</summary>
public sealed record AssistResponse(string? Answer, IReadOnlyList<KnowledgeMatch> Sources);

/// <summary>RAG con caché: busca artículos relevantes para un ticket y pide a la IA una respuesta basada solo en ellos.</summary>
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

        // La respuesta depende solo del texto, no del usuario: por eso la clave es el hash del texto
        var key = "assist:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(query)));
        if (await cache.GetAsync(key, ct) is { } cached) return cached;

        var matches = await knowledge.SearchAsync(query, 3, ct);
        var relevant = matches.Where(m => m.Similarity >= MinSimilarity).ToList();

        // Sin contexto fiable no llamamos a la IA: evitamos respuestas inventadas
        var answer = relevant.Count == 0
            ? null
            : await answers.GenerateAsync(ticket.Title, ticket.Description, relevant, ct);

        var response = new AssistResponse(answer, relevant);
        await cache.SetAsync(key, response, CacheTtl, ct);
        return response;
    }
}
