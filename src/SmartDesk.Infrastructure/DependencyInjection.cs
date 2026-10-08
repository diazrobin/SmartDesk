using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartDesk.Application.Abstractions;
using SmartDesk.Infrastructure.Persistence;
using SmartDesk.Infrastructure.Persistence.Repositories;

namespace SmartDesk.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra todos los servicios de infraestructura en el contenedor de DI.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SmartDeskDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
