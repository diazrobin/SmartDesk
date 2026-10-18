using Microsoft.Extensions.Time.Testing;
using SmartDesk.Application.Tickets;

namespace SmartDesk.Application.Tests;

public class TicketClassificationTests
{
    [Fact]
    public async Task CreateAsync_AplicaLaClasificacionSugerida()
    {
        var service = new TicketService(
            new InMemoryTicketRepository(),
            new FakeCurrentUser("ana", IsAgent: false),
            new FakeTicketClassifier(),
            new FakeTimeProvider());

        var dto = await service.CreateAsync("Pantalla", "No enciende", CancellationToken.None);

        Assert.Equal("Hardware", dto.Category);
        Assert.Equal("High", dto.Priority);
    }
}
