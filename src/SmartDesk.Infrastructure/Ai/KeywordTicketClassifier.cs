using System.Globalization;
using System.Text;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Tickets;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Infrastructure.Ai;

/// <summary>Clasificador por palabras clave: sin red, instantáneo y determinista. Respaldo de la IA.</summary>
internal sealed class KeywordTicketClassifier : ITicketClassifier
{
    // El orden importa: gana la primera categoría que encaje
    private static readonly (string Category, string[] Keywords)[] CategoryRules =
    [
        (TicketCategories.Network,  ["vpn", "wifi", "internet", "conexion", "conectar", "router", "proxy"]),
        (TicketCategories.Access,   ["contrasena", "password", "acceso", "login", "bloquead", "permiso", "credencial"]),
        (TicketCategories.Hardware, ["teclado", "raton", "monitor", "pantalla", "impresora", "portatil", "bateria", "enciende"]),
        (TicketCategories.Software, ["outlook", "correo", "excel", "word", "aplicacion", "programa", "instalar", "actualizar"]),
    ];

    private static readonly string[] CriticalWords = ["caido", "toda la oficina", "todos los usuarios", "produccion", "urgente"];
    private static readonly string[] HighWords     = ["no funciona", "no enciende", "no puedo", "bloquead", "error"];

    public Task<TicketClassification> ClassifyAsync(string title, string description, CancellationToken ct)
    {
        var text = Normalize($"{title} {description}");

        var category = CategoryRules.FirstOrDefault(r => r.Keywords.Any(text.Contains)).Category
                       ?? TicketCategories.Other;

        var priority = CriticalWords.Any(text.Contains) ? TicketPriority.Critical
                     : HighWords.Any(text.Contains)     ? TicketPriority.High
                     : TicketPriority.Medium;

        return Task.FromResult(new TicketClassification(category, priority));
    }

    // Minúsculas y sin tildes: "Conexión" y "conexion" cuentan igual
    private static string Normalize(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString();
    }
}
