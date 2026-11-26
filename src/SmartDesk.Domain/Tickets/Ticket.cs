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
    public ClassificationStatus Classification { get; private set; }
    public int ClassificationAttempts { get; private set; }
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
            Classification = ClassificationStatus.Pending,
            CreatedBy = createdBy.Trim(),
            CreatedAt = clock.GetUtcNow()
        };
    }

    /// <summary>Aplica la clasificación sugerida por la IA y la marca como clasificada.</summary>
    public void Classify(string category, TicketPriority priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
            throw new DomainException("No se puede reclasificar un ticket resuelto o cerrado.");

        Category = category.Trim();
        Priority = priority;
        Classification = ClassificationStatus.Classified;
    }

    /// <summary>Registra un intento de clasificación fallido. Tras maxAttempts, el ticket queda en Failed.</summary>
    public void RecordClassificationFailure(int maxAttempts)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        if (Classification != ClassificationStatus.Pending)
            throw new DomainException("Solo un ticket pendiente puede registrar fallos de clasificación.");

        ClassificationAttempts++;
        if (ClassificationAttempts >= maxAttempts)
            Classification = ClassificationStatus.Failed;
    }

    /// <summary>Renuncia a clasificar un ticket pendiente (p. ej. ya resuelto). Queda en Failed.</summary>
    public void AbandonClassification()
    {
        if (Classification != ClassificationStatus.Pending)
            throw new DomainException("Solo un ticket pendiente puede abandonar la clasificación.");
        Classification = ClassificationStatus.Failed;
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
