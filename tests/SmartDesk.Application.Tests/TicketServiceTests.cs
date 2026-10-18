using Microsoft.Extensions.Time.Testing;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Tickets;
using SmartDesk.Domain.Common;

namespace SmartDesk.Application.Tests;

public class TicketServiceTests
{
    private static readonly FakeCurrentUser Ana   = new("ana", IsAgent: false);
    private static readonly FakeCurrentUser Marta = new("marta", IsAgent: false);
    private static readonly FakeCurrentUser Luis  = new("luis", IsAgent: true);

    private readonly InMemoryTicketRepository _repo = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 15, 9, 0, 0, TimeSpan.Zero));

    // Cada test elige "quién" ejecuta la operación; el repositorio es compartido
    private TicketService As(ICurrentUser user) => new(_repo, user, new FakeTicketClassifier(), _clock);

    [Fact]
    public async Task CreateAsync_GuardaElTicketConSuCreador()
    {
        var dto = await As(Ana).CreateAsync("VPN caída", "Error 809", CancellationToken.None);

        Assert.Single(_repo.Items);
        Assert.Equal(1, _repo.SaveCount);
        Assert.Equal("Open", dto.Status);
        Assert.Equal("ana", dto.CreatedBy);
    }

    [Fact]
    public async Task StartAsync_TicketInexistente_DevuelveNullSinGuardar()
    {
        var result = await As(Luis).StartAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, _repo.SaveCount);
    }

    [Fact]
    public async Task ResolveAsync_TicketAbierto_LanzaDomainExceptionYNoGuarda()
    {
        var dto = await As(Ana).CreateAsync("Impresora", "No imprime", CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => As(Luis).ResolveAsync(dto.Id, CancellationToken.None));
        Assert.Equal(1, _repo.SaveCount); // solo el guardado de la creación
    }

    [Fact]
    public async Task ListAsync_Usuario_SoloVeSusTickets()
    {
        await As(Ana).CreateAsync("De Ana", "x", CancellationToken.None);
        await As(Marta).CreateAsync("De Marta", "x", CancellationToken.None);

        var lista = await As(Ana).ListAsync(null, CancellationToken.None);

        var unico = Assert.Single(lista);
        Assert.Equal("ana", unico.CreatedBy);
    }

    [Fact]
    public async Task ListAsync_Agente_VeTodos()
    {
        await As(Ana).CreateAsync("De Ana", "x", CancellationToken.None);
        await As(Marta).CreateAsync("De Marta", "x", CancellationToken.None);

        var lista = await As(Luis).ListAsync(null, CancellationToken.None);

        Assert.Equal(2, lista.Count);
    }

    [Fact]
    public async Task GetAsync_TicketDeOtroUsuario_DevuelveNull()
    {
        var deMarta = await As(Marta).CreateAsync("Privado", "x", CancellationToken.None);

        var resultado = await As(Ana).GetAsync(deMarta.Id, CancellationToken.None);

        Assert.Null(resultado);
    }
}
