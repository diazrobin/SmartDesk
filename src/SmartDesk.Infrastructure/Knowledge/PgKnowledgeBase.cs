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

/// <summary>Base de conocimiento en PostgreSQL. Con embeddings busca por significado; sin ellos, por texto.</summary>
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

        // El vector representa el significado de título + contenido
        var vector = await TryEmbedAsync($"{article.Title}\n{article.Content}", ct);
        if (vector is not null)
            db.Entry(article).Property(Emb).CurrentValue = vector;

        await db.SaveChangesAsync(ct);
        return new KnowledgeArticleDto(article.Id, article.Title, article.Content);
    }

    public async Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(string query, int top, CancellationToken ct)
    {
        var vector = await TryEmbedAsync(query, ct);

        if (vector is null)
        {
            // Respaldo sin IA: búsqueda de texto simple
            var term = $"%{query.Trim()}%";
            return await db.KnowledgeArticles.AsNoTracking()
                .Where(a => EF.Functions.ILike(a.Title, term) || EF.Functions.ILike(a.Content, term))
                .Take(top)
                .Select(a => new KnowledgeMatch(a.Id, a.Title, a.Content, 0))
                .ToListAsync(ct);
        }

        // Distancia coseno: 0 = mismo significado. PostgreSQL la calcula con el operador <=> de pgvector
        return await db.KnowledgeArticles.AsNoTracking()
            .Where(a => EF.Property<Vector?>(a, Emb) != null)
            .Select(a => new { a.Id, a.Title, a.Content, Distance = EF.Property<Vector>(a, Emb).CosineDistance(vector) })
            .OrderBy(x => x.Distance)
            .Take(top)
            .Select(x => new KnowledgeMatch(x.Id, x.Title, x.Content, 1 - x.Distance))
            .ToListAsync(ct);
    }

    private async Task<Vector?> TryEmbedAsync(string text, CancellationToken ct)
    {
        if (embeddings is null) return null;
        try
        {
            var values = await embeddings.GenerateVectorAsync(text, cancellationToken: ct);
            return new Vector(values.ToArray());
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "No se pudo generar el embedding; se usa búsqueda de texto.");
            return null;
        }
    }
}
