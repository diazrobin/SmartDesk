using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Infrastructure.Persistence.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever(); // el Id lo genera el dominio (Guid v7)

        builder.Property(t => t.Title)
               .HasMaxLength(Ticket.TitleMaxLength)
               .IsRequired();

        builder.Property(t => t.Description).IsRequired();

        // Enums guardados como texto: legibles en la BD y robustos si se reordena el enum
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);

        builder.Property(t => t.Category).HasMaxLength(100);

        builder.Property(t => t.CreatedBy)
               .HasMaxLength(Ticket.CreatedByMaxLength)
               .IsRequired();

        builder.HasIndex(t => t.Status);     // listados por estado
        builder.HasIndex(t => t.CreatedBy);  // "mis tickets": la consulta más frecuente de un usuario
    }
}
