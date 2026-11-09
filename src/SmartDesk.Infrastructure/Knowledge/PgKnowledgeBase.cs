using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Knowledge;
using SmartDesk.Domain.Knowledge;
using SmartDesk.Infrastructure.Persistence;
using SmartDesk.Infrastructure.Persistence.Configurations;

namespace SmartDesk.Infrastructure.Knowledge;

/// <summary>Base de conocimiento en PostgreSQL. Con IA busca por significado; sin IA, por texto. Si la IA falla, lanza AiUnavailableException.</summary>
internal sealed class PgKnowledgeBase(
    SmartDeskDbContext db,
    TimeProvider clock,
    ILogger<PgKnowledgeBase> logger,
    IEmbeddingGenerator<string, Embedding<float>>? embeddings = null) : IKnowledgeBase
{
    private const string Emb = KnowledgeArticleConfiguration.EmbeddingProperty;

    public async Task<KnowledgeArticleDto> AddAsync(string title, string content, CancellationToken ct)
    {
        var article = KnowledgeArticle.Create(title, content, clock);
        db.KnowledgeArticles.Add(article);

        // Si la IA falla, no se guarda el artículo: evitamos artículos invisibles para la búsqueda semántica
        var vector = await EmbedAsync($"{article.Title}\n{article.Content}", ct);
        if (vector is not null)
            db.Entry(article).Property(Emb).CurrentValue = vector;

        await db.SaveChangesAsync(ct);
        return new KnowledgeArticleDto(article.Id, article.Title, article.Content);
    }

    public async Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(string query, int top, CancellationToken ct)
    {
        var vector = await EmbedAsync(query, ct);

        if (vector is null)
        {
            // Sin IA configurada: búsqueda de texto simple
            var term = $"%{query.Trim()}%";
            return await db.KnowledgeArticles.AsNoTracking()
                .Where(a => EF.Functions.ILike(a.Title, term) || EF.Functions.ILike(a.Content, term))
                .Take(top)
                .Select(a => new KnowledgeMatch(a.Id, a.Title, a.Content, 0))
                .ToListAsync(ct);
        }

        // Distancia coseno: 0 = mismo significado
        return await db.KnowledgeArticles.AsNoTracking()
            .Where(a => EF.Property<Vector?>(a, Emb) != null)
            .Select(a => new { a.Id, a.Title, a.Content, Distance = EF.Property<Vector>(a, Emb).CosineDistance(vector) })
            .OrderBy(x => x.Distance)
            .Take(top)
            .Select(x => new KnowledgeMatch(x.Id, x.Title, x.Content, 1 - x.Distance))
            .ToListAsync(ct);
    }

    /// <returns>El vector, o null si la IA no está configurada.</returns>
    private async Task<Vector?> EmbedAsync(string text, CancellationToken ct)
    {
        if (embeddings is null) return null;

        try
        {
            var values = await embeddings.GenerateVectorAsync(text, cancellationToken: ct);
            return new Vector(values.ToArray());
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "No se pudo generar el embedding.");
            throw new AiUnavailableException("La IA no está disponible para generar embeddings.", ex);
        }
    }
}
