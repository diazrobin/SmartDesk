using System.ClientModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using Pgvector.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Infrastructure.Ai;
using SmartDesk.Infrastructure.Knowledge;
using SmartDesk.Infrastructure.Persistence;
using SmartDesk.Infrastructure.Persistence.Repositories;

namespace SmartDesk.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra todos los servicios de infraestructura en el contenedor de DI.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, IConfigurationSection ai)
    {
        services.AddDbContext<SmartDeskDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IKnowledgeBase, PgKnowledgeBase>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<KeywordTicketClassifier>();

        var apiKey = ai["ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Sin clave de IA: clasificación por reglas y búsqueda por texto
            services.AddSingleton<ITicketClassifier>(sp => sp.GetRequiredService<KeywordTicketClassifier>());
            services.AddSingleton<IAnswerGenerator, NoAnswerGenerator>();
            return services;
        }

        var endpoint       = ai["Endpoint"] ?? throw new InvalidOperationException("Falta Ai:Endpoint.");
        var chatModel      = ai["Model"]    ?? throw new InvalidOperationException("Falta Ai:Model.");
        var embeddingModel = ai["EmbeddingModel"] ?? "gemini-embedding-001";

        // Un único cliente compatible con OpenAI para chat y embeddings
        var openAi = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        services.AddChatClient(_ => openAi.GetChatClient(chatModel).AsIChatClient());
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ =>
            openAi.GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator());

        services.AddScoped<ITicketClassifier, LlmTicketClassifier>();
        services.AddScoped<IAnswerGenerator, LlmAnswerGenerator>();
        return services;
    }
}

