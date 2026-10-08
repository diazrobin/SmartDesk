using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tickets;

/// <summary>Lo que la API devuelve. Nunca exponemos la entidad de dominio directamente.</summary>
public sealed record TicketDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    string? Category,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt)
{
    public static TicketDto FromEntity(Ticket t) => new(
        t.Id, t.Title, t.Description, t.Status.ToString(), t.Priority.ToString(),
        t.Category, t.CreatedAt, t.ResolvedAt);
}
