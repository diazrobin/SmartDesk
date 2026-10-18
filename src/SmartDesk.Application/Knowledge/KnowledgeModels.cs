namespace SmartDesk.Application.Knowledge;

public sealed record KnowledgeArticleDto(Guid Id, string Title, string Content);

/// <summary>Artículo encontrado y su parecido con la consulta (1 = idéntico, 0 = nada que ver).</summary>
public sealed record KnowledgeMatch(Guid Id, string Title, string Content, double Similarity);
