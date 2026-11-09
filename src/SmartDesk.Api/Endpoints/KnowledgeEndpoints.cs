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
        group.MapPost("/", async Task<Results<Created<KnowledgeArticleDto>, ProblemHttpResult>> (
            CreateArticleRequest request, IKnowledgeBase kb, CancellationToken ct) =>
        {
            try
            {
                var article = await kb.AddAsync(request.Title, request.Content, ct);
                return TypedResults.Created($"/api/knowledge/{article.Id}", article);
            }
            catch (AiUnavailableException)
            {
                return ServiceUnavailable();
            }
        }).RequireAuthorization(TicketEndpoints.AgentPolicy);

        group.MapGet("/search", async Task<Results<Ok<IReadOnlyList<KnowledgeMatch>>, ProblemHttpResult>> (
            string q, int? top, IKnowledgeBase kb, CancellationToken ct) =>
        {
            try
            {
                return TypedResults.Ok(await kb.SearchAsync(q, Math.Clamp(top ?? 3, 1, 10), ct));
            }
            catch (AiUnavailableException)
            {
                return ServiceUnavailable();
            }
        });

        // Artículos relacionados con un ticket (respeta la visibilidad: un usuario solo ve sus tickets)
        app.MapGet("/api/tickets/{id:guid}/suggestions", async Task<Results<Ok<IReadOnlyList<KnowledgeMatch>>, NotFound, ProblemHttpResult>> (
            Guid id, TicketService tickets, IKnowledgeBase kb, CancellationToken ct) =>
        {
            var ticket = await tickets.GetAsync(id, ct);
            if (ticket is null) return TypedResults.NotFound();

            try
            {
                return TypedResults.Ok(await kb.SearchAsync($"{ticket.Title}\n{ticket.Description}", 3, ct));
            }
            catch (AiUnavailableException)
            {
                return ServiceUnavailable();
            }
        }).WithTags("Tickets").RequireAuthorization();

        // RAG completo: artículos relevantes + respuesta redactada, con estado explícito
        app.MapGet("/api/tickets/{id:guid}/assist", async Task<Results<Ok<AssistResponse>, NotFound>> (
            Guid id, AssistanceService assistance, CancellationToken ct) =>
            await assistance.AssistAsync(id, ct) is { } result ? TypedResults.Ok(result) : TypedResults.NotFound())
            .WithTags("Tickets").RequireAuthorization();

        return app;
    }

    private static ProblemHttpResult ServiceUnavailable() =>
        TypedResults.Problem(
            detail: "La IA no está disponible en este momento. Inténtalo más tarde.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Servicio de IA no disponible");
}
