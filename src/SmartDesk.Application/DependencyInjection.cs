using Microsoft.Extensions.DependencyInjection;
using SmartDesk.Application.Tickets;

namespace SmartDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<TicketService>();
        return services;
    }
}
