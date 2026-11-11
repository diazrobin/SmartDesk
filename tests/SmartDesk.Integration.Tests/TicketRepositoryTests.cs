using SmartDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using SmartDesk.Domain.Tickets;
using SmartDesk.Infrastructure.Persistence.Repositories;

namespace SmartDesk.Integration.Tests;

[Collection("Postgres")]
public class TicketRepositoryTests(PostgresFixture pg)
{
    private static FakeTimeProvider NuevoReloj() =>
        new(new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero));

    /// <summary>Base limpia para cada test, con su propio contexto.</summary>
    private async Task<(SmartDeskDbContext Db, TicketRepository Repo)> RepoLimpioAsync()
    {
        var db = pg.CreateDb();
        await db.Tickets.ExecuteDeleteAsync();
        return (db, new TicketRepository(db));
    }

    [Fact]
    public async Task Migraciones_NoTienenPendientesTrasAplicarlas()
    {
        await using var db = pg.CreateDb();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task PageAsync_OrdenaPorFechaDescendente()
    {
        var (db, repo) = await RepoLimpioAsync();
        await using var _ = db;
        var reloj = NuevoReloj();

        for (var i = 1; i <= 5; i++)
        {
            repo.Add(Ticket.Create($"T{i}", "x", "ana", reloj));
            reloj.Advance(TimeSpan.FromMinutes(1));
        }
        await repo.SaveChangesAsync(CancellationToken.None);

        var (items, total) = await repo.PageAsync(null, null, 1, 2, CancellationToken.None);

        Assert.Equal(5, total);
        Assert.Equal(["T5", "T4"], items.Select(t => t.Title));
    }

    [Fact]
    public async Task PageAsync_PaginasSinDuplicados_ConMismoInstante()
    {
        var (db, repo) = await RepoLimpioAsync();
        await using var _ = db;
        var reloj = NuevoReloj();

        // Todos con la misma fecha: el desempate por Id debe mantener el orden estable en PostgreSQL
        for (var i = 1; i <= 25; i++)
            repo.Add(Ticket.Create($"T{i}", "x", "ana", reloj));
        await repo.SaveChangesAsync(CancellationToken.None);

        var ids = new List<Guid>();
        for (var p = 1; p <= 3; p++)
        {
            var (items, total) = await repo.PageAsync(null, null, p, 10, CancellationToken.None);
            Assert.Equal(25, total);
            ids.AddRange(items.Select(t => t.Id));
        }

        Assert.Equal(25, ids.Count);
        Assert.Equal(25, ids.Distinct().Count());
    }

    [Fact]
    public async Task PageAsync_FiltraPorCreador_CuentaSoloLosSuyos()
    {
        var (db, repo) = await RepoLimpioAsync();
        await using var _ = db;
        var reloj = NuevoReloj();

        for (var i = 0; i < 3; i++) repo.Add(Ticket.Create("De Ana", "x", "ana", reloj));
        for (var i = 0; i < 5; i++) repo.Add(Ticket.Create("De Marta", "x", "marta", reloj));
        await repo.SaveChangesAsync(CancellationToken.None);

        var (items, total) = await repo.PageAsync(null, "ana", 1, 20, CancellationToken.None);

        Assert.Equal(3, total);
        Assert.All(items, t => Assert.Equal("ana", t.CreatedBy));
    }

    [Fact]
    public async Task PageAsync_FiltraPorEstado_AntesDePaginar()
    {
        var (db, repo) = await RepoLimpioAsync();
        await using var _ = db;
        var reloj = NuevoReloj();

        var uno = Ticket.Create("Uno", "x", "ana", reloj);
        repo.Add(uno);
        repo.Add(Ticket.Create("Dos", "x", "ana", reloj));
        await repo.SaveChangesAsync(CancellationToken.None);

        uno.StartProgress();
        await repo.SaveChangesAsync(CancellationToken.None);

        var (items, total) = await repo.PageAsync(TicketStatus.InProgress, null, 1, 10, CancellationToken.None);

        Assert.Equal(1, total);
        Assert.Equal("Uno", Assert.Single(items).Title);
    }

    [Fact]
    public async Task PageAsync_SinResultados_DevuelveTotalCero()
    {
        var (db, repo) = await RepoLimpioAsync();
        await using var _ = db;

        var (items, total) = await repo.PageAsync(null, null, 1, 10, CancellationToken.None);

        Assert.Equal(0, total);
        Assert.Empty(items);
    }
}
