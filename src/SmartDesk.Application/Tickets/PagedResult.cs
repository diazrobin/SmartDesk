namespace SmartDesk.Application.Tickets;

/// <summary>Una página de resultados y el total, para que el cliente sepa cuántas páginas hay.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Límites de paginación compartidos por la API y la aplicación.</summary>
public static class PageLimits
{
    public const int DefaultSize = 20;
    public const int MaxSize = 100;
    public const int MaxPage = 10_000;
}
