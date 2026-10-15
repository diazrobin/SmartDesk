using Microsoft.Extensions.Time.Testing;
using SmartDesk.Application.Tickets;
using SmartDesk.Domain.Common;

namespace SmartDesk.Application.Tests;

public class TicketServiceTests
{
    private readonly InMemoryTicketRepository _repo = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 15, 9, 0, 0, TimeSpan.Zero));
    private readonly TicketService _service;

    public TicketServiceTests() => _service = new TicketService(_repo, _clock);

    [Fact]
    public async Task CreateAsync_GuardaElTicketYDevuelveDto()
    {
        var dto = await _service.CreateAsync("VPN caída", "Error 809", CancellationToken.None);

        Assert.Single(_repo.Items);
        Assert.Equal(1, _repo.SaveCount);
        Assert.Equal("Open", dto.Status);
    }

    [Fact]
    public async Task StartAsync_TicketInexistente_DevuelveNullSinGuardar()
    {
        var result = await _service.StartAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, _repo.SaveCount);
    }

    [Fact]
    public async Task ResolveAsync_TicketAbierto_LanzaDomainExceptionYNoGuarda()
    {
        var dto = await _service.CreateAsync("Impresora", "No imprime", CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => _service.ResolveAsync(dto.Id, CancellationToken.None));
        Assert.Equal(1, _repo.SaveCount); // solo el guardado de la creación
    }
}
