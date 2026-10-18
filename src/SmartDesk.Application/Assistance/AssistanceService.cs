using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Knowledge;
using SmartDesk.Application.Tickets;

namespace SmartDesk.Application.Assistance;

/// <summary>Respuesta sugerida y artículos en los que se basa.</summary>
public sealed record AssistResponse(string? Answer, IReadOnlyList<KnowledgeMatch> Sources);

/// <summary>RAG: busca artículos relevantes para un ticket y pide a la IA una respuesta basada solo en ellos.</summary>
public sealed class AssistanceService(TicketService tickets, IKnowledgeBase knowledge, IAnswerGenerator answers)
{
    /// <summary>Por debajo de este parecido, el artículo no se considera relevante (calibrado con datos reales).</summary>
    public const double MinSimilarity = 0.70;

    public async Task<AssistResponse?> AssistAsync(Guid ticketId, CancellationToken ct)
    {
        // Reutilizamos TicketService: respeta la visibilidad (un usuario solo accede a sus tickets)
        var ticket = await tickets.GetAsync(ticketId, ct);
        if (ticket is null) return null;

        var matches = await knowledge.SearchAsync($"{ticket.Title}\n{ticket.Description}", 3, ct);
        var relevant = matches.Where(m => m.Similarity >= MinSimilarity).ToList();

        // Sin contexto fiable no llamamos a la IA: evitamos respuestas inventadas
        var answer = relevant.Count == 0
            ? null
            : await answers.GenerateAsync(ticket.Title, ticket.Description, relevant, ct);

        return new AssistResponse(answer, relevant);
    }
}
