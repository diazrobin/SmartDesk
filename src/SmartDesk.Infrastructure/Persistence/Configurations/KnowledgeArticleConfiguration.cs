using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;
using SmartDesk.Domain.Knowledge;

namespace SmartDesk.Infrastructure.Persistence.Configurations;

internal sealed class KnowledgeArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public const string EmbeddingProperty = "Embedding";

    public void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        builder.ToTable("knowledge_articles");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Title).HasMaxLength(KnowledgeArticle.TitleMaxLength).IsRequired();
        builder.Property(a => a.Content).IsRequired();

        // Propiedad "sombra": la columna existe en la tabla, pero la clase de dominio no la conoce
        builder.Property<Vector?>(EmbeddingProperty).HasColumnType("vector");
    }
}
