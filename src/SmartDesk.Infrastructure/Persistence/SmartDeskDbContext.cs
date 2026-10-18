using Microsoft.EntityFrameworkCore;
using SmartDesk.Domain.Knowledge;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Infrastructure.Persistence;

public sealed class SmartDeskDbContext(DbContextOptions<SmartDeskDbContext> options)
    : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<KnowledgeArticle> KnowledgeArticles => Set<KnowledgeArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");   // la migración ejecutará CREATE EXTENSION vector
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartDeskDbContext).Assembly);
    }
}
