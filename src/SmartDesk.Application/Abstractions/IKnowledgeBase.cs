using SmartDesk.Application.Knowledge;

namespace SmartDesk.Application.Abstractions;

/// <summary>Base de conocimiento con búsqueda por significado. Infrastructure decide cómo (embeddings + pgvector).</summary>
public interface IKnowledgeBase
{
    Task<KnowledgeArticleDto> AddAsync(string title, string content, CancellationToken ct);
    Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(string query, int top, CancellationToken ct);
}
