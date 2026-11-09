using Microsoft.Extensions.Time.Testing;
using SmartDesk.Application.Tickets;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tests;

public class TicketPagingTests
{
    private static (TicketService Service, FakeTimeProvider Clock) Create(InMemoryTicketRepository repo, string user, bool isAgent)
    {
        var clock = new FakeTimeProvider();
        var service = new TicketService(repo, new FakeCurrentUser(user, isAgent), new FakeTicketClassifier(), clock);
        return (service, clock);
    }

    [Fact]
    public async Task PageAsync_DevuelveElNumeroCorrectoDeElementosYElTotal()
    {
        var repo = new InMemoryTicketRepository();
        var (service, clock) = Create(repo, "ana", isAgent: false);

        for (var i = 1; i <= 25; i++)
        {
            await service.CreateAsync($"T{i}", "x", CancellationToken.None);
            clock.Advance(TimeSpan.FromSeconds(1));
        }

        var page = await service.PageAsync(null, 1, 10, CancellationToken.None);
        var last = await service.PageAsync(null, 3, 10, CancellationToken.None);

        Assert.Equal(10, page.Items.Count);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(5, last.Items.Count);
    }

    [Fact]
    public async Task PageAsync_OrdenaPorFechaDescendente()
    {
        var repo = new InMemoryTicketRepository();
        var (service, clock) = Create(repo, "ana", isAgent: false);

        for (var i = 1; i <= 3; i++)
        {
            await service.CreateAsync($"T{i}", "x", CancellationToken.None);
            clock.Advance(TimeSpan.FromSeconds(1));
        }

        var page = await service.PageAsync(null, 1, 10, CancellationToken.None);

        Assert.Equal(["T3", "T2", "T1"], page.Items.Select(t => t.Title));
    }

    [Fact]
    public async Task PageAsync_PaginasSinDuplicadosNiHuecos()
    {
        var repo = new InMemoryTicketRepository();
        var (service, clock) = Create(repo, "ana", isAgent: false);

        // Mismo instante para todos: el desempate por Id debe mantener el orden estable
        for (var i = 1; i <= 25; i++)
            await service.CreateAsync($"T{i}", "x", CancellationToken.None);

        var ids = new List<Guid>();
        for (var p = 1; p <= 3; p++)
            ids.AddRange((await service.PageAsync(null, p, 10, CancellationToken.None)).Items.Select(t => t.Id));

        Assert.Equal(25, ids.Count);
        Assert.Equal(25, ids.Distinct().Count());
    }

    [Fact]
    public async Task PageAsync_UsuarioSoloCuentaSusTickets()
    {
        var repo = new InMemoryTicketRepository();
        var (ana, _) = Create(repo, "ana", isAgent: false);
        var (marta, _) = Create(repo, "marta", isAgent: false);

        for (var i = 0; i < 3; i++) await ana.CreateAsync("De Ana", "x", CancellationToken.None);
        for (var i = 0; i < 5; i++) await marta.CreateAsync("De Marta", "x", CancellationToken.None);

        var page = await ana.PageAsync(null, 1, 20, CancellationToken.None);

        Assert.Equal(3, page.TotalCount);
        Assert.All(page.Items, t => Assert.Equal("ana", t.CreatedBy));
    }

    [Fact]
    public async Task PageAsync_FiltraPorEstadoAntesDePaginar()
    {
        var repo = new InMemoryTicketRepository();
        var (service, _) = Create(repo, "ana", isAgent: true);

        var uno = await service.CreateAsync("Uno", "x", CancellationToken.None);
        await service.CreateAsync("Dos", "x", CancellationToken.None);
        await service.StartAsync(uno.Id, CancellationToken.None);

        var page = await service.PageAsync(TicketStatus.InProgress, 1, 10, CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Uno", Assert.Single(page.Items).Title);
    }
}
