using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartDesk.Application.Tickets;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Api.Endpoints;

/// <summary>Cuerpo de la petición para crear un ticket. Se valida automáticamente antes de llegar al endpoint.</summary>
public sealed record CreateTicketRequest(
    [Required, MaxLength(Ticket.TitleMaxLength)] string Title,
    [Required, MaxLength(4000)] string Description);

public static class TicketEndpoints
{
    public const string AgentPolicy = "Agente";

    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        // Todo el grupo exige un usuario autenticado (sin token => 401)
        var group = app.MapGroup("/api/tickets")
                       .WithTags("Tickets")
                       .RequireAuthorization();

        group.MapPost("/", async (CreateTicketRequest request, TicketService service, CancellationToken ct) =>
        {
            var ticket = await service.CreateAsync(request.Title, request.Description, ct);
            return TypedResults.Created($"/api/tickets/{ticket.Id}", ticket);
        });

        // Listado paginado: ?status=&page=&pageSize=  (por defecto página 1, 20 elementos)
        group.MapGet("/", async Task<Results<Ok<PagedResult<TicketDto>>, ValidationProblem>> (
            TicketStatus? status, int? page, int? pageSize, TicketService service, CancellationToken ct) =>
        {
            var currentPage = page ?? 1;
            var size = pageSize ?? PageLimits.DefaultSize;

            var errors = new Dictionary<string, string[]>();
            if (currentPage < 1 || currentPage > PageLimits.MaxPage)
                errors["page"] = [$"page debe estar entre 1 y {PageLimits.MaxPage}."];
            if (size < 1 || size > PageLimits.MaxSize)
                errors["pageSize"] = [$"pageSize debe estar entre 1 y {PageLimits.MaxSize}."];

            if (errors.Count > 0)
                return TypedResults.ValidationProblem(errors);

            return TypedResults.Ok(await service.PageAsync(status, currentPage, size, ct));
        });

        group.MapGet("/{id:guid}", async Task<Results<Ok<TicketDto>, NotFound>> (
            Guid id, TicketService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } ticket ? TypedResults.Ok(ticket) : TypedResults.NotFound());

        // Cambios de estado: solo agentes (usuario autenticado sin el rol => 403)
        group.MapPost("/{id:guid}/start", async Task<Results<Ok<TicketDto>, NotFound>> (
            Guid id, TicketService service, CancellationToken ct) =>
            await service.StartAsync(id, ct) is { } ticket ? TypedResults.Ok(ticket) : TypedResults.NotFound())
            .RequireAuthorization(AgentPolicy);

        group.MapPost("/{id:guid}/resolve", async Task<Results<Ok<TicketDto>, NotFound>> (
            Guid id, TicketService service, CancellationToken ct) =>
            await service.ResolveAsync(id, ct) is { } ticket ? TypedResults.Ok(ticket) : TypedResults.NotFound())
            .RequireAuthorization(AgentPolicy);

        return app;
    }
}
