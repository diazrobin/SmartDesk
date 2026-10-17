using SmartDesk.Domain.Common;

namespace SmartDesk.Domain.Tickets;

public sealed class Ticket
{
    public const int TitleMaxLength = 200;
    public const int CreatedByMaxLength = 100;

    // Constructor privado: EF Core lo usará para materializar,
    // pero el código de negocio está obligado a pasar por Create().
    private Ticket() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public string? Category { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public static Ticket Create(string title, string description, string createdBy, TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        if (title.Length > TitleMaxLength)
            throw new DomainException($"El título no puede superar {TitleMaxLength} caracteres.");
        if (createdBy.Length > CreatedByMaxLength)
            throw new DomainException($"El identificador del creador no puede superar {CreatedByMaxLength} caracteres.");

        return new Ticket
        {
            Id = Guid.CreateVersion7(),
            Title = title.Trim(),
            Description = description.Trim(),
            Status = TicketStatus.Open,
            Priority = TicketPriority.Unassigned,
            CreatedBy = createdBy.Trim(),
            CreatedAt = clock.GetUtcNow()
        };
    }

    /// <summary>Lo usará el clasificador de IA en la fase 5.</summary>
    public void Classify(string category, TicketPriority priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
            throw new DomainException("No se puede reclasificar un ticket resuelto o cerrado.");

        Category = category.Trim();
        Priority = priority;
    }

    public void StartProgress()
    {
        if (Status != TicketStatus.Open)
            throw new DomainException($"Solo un ticket abierto puede pasar a en curso (estado actual: {Status}).");
        Status = TicketStatus.InProgress;
    }

    public void Resolve(TimeProvider clock)
    {
        if (Status != TicketStatus.InProgress)
            throw new DomainException("Solo un ticket en curso puede resolverse.");
        Status = TicketStatus.Resolved;
        ResolvedAt = clock.GetUtcNow();
    }
}
