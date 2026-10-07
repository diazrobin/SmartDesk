using Microsoft.EntityFrameworkCore;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Infrastructure.Persistence;

public sealed class SmartDeskDbContext(DbContextOptions<SmartDeskDbContext> options)
    : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Carga automáticamente todas las clases IEntityTypeConfiguration de este proyecto
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartDeskDbContext).Assembly);
    }
}
