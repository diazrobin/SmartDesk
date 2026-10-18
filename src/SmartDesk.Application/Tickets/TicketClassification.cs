using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tickets;

/// <summary>Categorías válidas. La IA solo puede elegir entre estas.</summary>
public static class TicketCategories
{
    public const string Hardware = "Hardware";
    public const string Network  = "Red";
    public const string Software = "Software";
    public const string Access   = "Accesos";
    public const string Other    = "Otros";

    public static readonly IReadOnlyList<string> All = [Hardware, Network, Software, Access, Other];
}

/// <summary>Resultado de clasificar un ticket.</summary>
public sealed record TicketClassification(string Category, TicketPriority Priority);
