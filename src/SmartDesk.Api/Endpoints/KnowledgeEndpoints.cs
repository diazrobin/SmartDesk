using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Assistance;
using SmartDesk.Application.Knowledge;
using SmartDesk.Application.Tickets;
using SmartDesk.Domain.Knowledge;

namespace SmartDesk.Api.Endpoints;

public sealed record CreateArticleRequest(
    [Required, MaxLength(KnowledgeArticle.TitleMaxLength)] string Title,
    [Required, MaxLength(20000)] string Content);

public static class KnowledgeEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/knowledge").WithTags("Knowledge").RequireAuthorization();

        // Solo los agentes alimentan la base de conocimiento
        group.MapPost("/", async (CreateArticleRequest request, IKnowledgeBase kb, CancellationToken ct) =>
        {
            var article = await kb.AddAsync(request.Title, request.Content, ct);
            return TypedResults.Created($"/api/knowledge/{article.Id}", article);
        }).RequireAuthorization(TicketEndpoints.AgentPolicy);

        group.MapGet("/search", async (string q, int? top, IKnowledgeBase kb, CancellationToken ct) =>
            TypedResults.Ok(await kb.SearchAsync(q, Math.Clamp(top ?? 3, 1, 10), ct)));

        // Artículos relacionados con un ticket (respeta la visibilidad: un usuario solo ve sus tickets)
        app.MapGet("/api/tickets/{id:guid}/suggestions", async Task<Results<Ok<IReadOnlyList<KnowledgeMatch>>, NotFound>> (
            Guid id, TicketService tickets, IKnowledgeBase kb, CancellationToken ct) =>
        {
            var ticket = await tickets.GetAsync(id, ct);
            if (ticket is null) return TypedResults.NotFound();

            return TypedResults.Ok(await kb.SearchAsync($"{ticket.Title}\n{ticket.Description}", 3, ct));
        }).WithTags("Tickets").RequireAuthorization();
        // RAG completo: artículos relevantes + respuesta redactada por la IA
        app.MapGet("/api/tickets/{id:guid}/assist", async Task<Results<Ok<AssistResponse>, NotFound>> (
            Guid id, AssistanceService assistance, CancellationToken ct) =>
            await assistance.AssistAsync(id, ct) is { } result ? TypedResults.Ok(result) : TypedResults.NotFound())
            .WithTags("Tickets").RequireAuthorization();

        return app;
    }
}

