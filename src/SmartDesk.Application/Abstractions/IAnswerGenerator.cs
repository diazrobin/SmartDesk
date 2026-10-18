using SmartDesk.Application.Knowledge;

namespace SmartDesk.Application.Abstractions;

/// <summary>Redacta una respuesta para el usuario a partir de artículos de la base de conocimiento.</summary>
public interface IAnswerGenerator
{
    /// <returns>La respuesta, o null si no es posible generarla (sin IA o con error).</returns>
    Task<string?> GenerateAsync(string title, string description, IReadOnlyList<KnowledgeMatch> sources, CancellationToken ct);
}
