using SmartDesk.Domain.Common;

namespace SmartDesk.Domain.Knowledge;

/// <summary>Artículo de la base de conocimiento de soporte (soluciones a problemas frecuentes).</summary>
public sealed class KnowledgeArticle
{
    public const int TitleMaxLength = 200;

    private KnowledgeArticle() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static KnowledgeArticle Create(string title, string content, TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        if (title.Length > TitleMaxLength)
            throw new DomainException($"El título no puede superar {TitleMaxLength} caracteres.");

        return new KnowledgeArticle
        {
            Id = Guid.CreateVersion7(),
            Title = title.Trim(),
            Content = content.Trim(),
            CreatedAt = clock.GetUtcNow()
        };
    }
}
