using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartDesk.Infrastructure.Persistence;

namespace SmartDesk.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra todos los servicios de infraestructura en el contenedor de DI.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SmartDeskDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
