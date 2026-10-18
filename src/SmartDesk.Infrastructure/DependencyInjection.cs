using System.ClientModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using SmartDesk.Application.Abstractions;
using SmartDesk.Infrastructure.Ai;
using SmartDesk.Infrastructure.Persistence;
using SmartDesk.Infrastructure.Persistence.Repositories;

namespace SmartDesk.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra todos los servicios de infraestructura en el contenedor de DI.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, IConfigurationSection ai)
    {
        services.AddDbContext<SmartDeskDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<KeywordTicketClassifier>();

        var apiKey = ai["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Sin clave de IA configurada: solo reglas por palabras clave
            services.AddSingleton<ITicketClassifier>(sp => sp.GetRequiredService<KeywordTicketClassifier>());
        }
        else
        {
            var endpoint = ai["Endpoint"] ?? throw new InvalidOperationException("Falta Ai:Endpoint.");
            var model    = ai["Model"]    ?? throw new InvalidOperationException("Falta Ai:Model.");

            // Cualquier proveedor compatible con OpenAI (Gemini, Azure OpenAI, OpenAI...): solo cambia la configuración
            services.AddChatClient(_ =>
                new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
                    .GetChatClient(model)
                    .AsIChatClient());

            services.AddScoped<ITicketClassifier, LlmTicketClassifier>();
        }

        return services;
    }
}
