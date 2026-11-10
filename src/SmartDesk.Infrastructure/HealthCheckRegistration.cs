using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartDesk.Infrastructure.Persistence;

namespace SmartDesk.Infrastructure;

public static class HealthCheckRegistration
{
    /// <summary>Readiness: solo la base de datos. Redis y la IA son opcionales y no bloquean el tráfico.</summary>
    public static IHealthChecksBuilder AddSmartDeskChecks(this IHealthChecksBuilder builder) =>
        builder.AddDbContextCheck<SmartDeskDbContext>("database", tags: ["ready"]);
}
