using Microsoft.Extensions.Time.Testing;
using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Assistance;
using SmartDesk.Application.Knowledge;
using SmartDesk.Application.Tickets;

namespace SmartDesk.Application.Tests;

public class AssistanceServiceTests
{
    private sealed class FakeKnowledgeBase(params KnowledgeMatch[] matches) : IKnowledgeBase
    {
        public Task<KnowledgeArticleDto> AddAsync(string title, string content, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(string query, int top, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<KnowledgeMatch>>(matches);
    }

    /// <summary>"Espía": registra con qué fuentes le llamaron, para comprobarlo en el test.</summary>
    private sealed class SpyAnswerGenerator : IAnswerGenerator
    {
        public IReadOnlyList<KnowledgeMatch>? ReceivedSources { get; private set; }

        public Task<string?> GenerateAsync(string title, string description, IReadOnlyList<KnowledgeMatch> sources, CancellationToken ct)
        {
            ReceivedSources = sources;
            return Task.FromResult<string?>("Prueba esto [1]");
        }
    }

    private static KnowledgeMatch Match(string title, double similarity) =>
        new(Guid.NewGuid(), title, "contenido", similarity);

    private static async Task<(AssistanceService Service, Guid TicketId)> CreateAsync(IKnowledgeBase kb, IAnswerGenerator gen)
    {
        var tickets = new TicketService(
            new InMemoryTicketRepository(), new FakeCurrentUser("ana", IsAgent: false),
            new FakeTicketClassifier(), new FakeTimeProvider());
        var ticket = await tickets.CreateAsync("VPN", "Error 809", CancellationToken.None);
        return (new AssistanceService(tickets, kb, gen), ticket.Id);
    }

    [Fact]
    public async Task AssistAsync_SoloPasaALaIALasFuentesRelevantes()
    {
        var spy = new SpyAnswerGenerator();
        var (service, id) = await CreateAsync(new FakeKnowledgeBase(Match("VPN", 0.86), Match("Impresora", 0.62)), spy);

        var result = await service.AssistAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Prueba esto [1]", result.Answer);
        Assert.Equal("VPN", Assert.Single(result.Sources).Title);
        Assert.Single(spy.ReceivedSources!);
    }

    [Fact]
    public async Task AssistAsync_SinFuentesRelevantes_NoLlamaALaIA()
    {
        var spy = new SpyAnswerGenerator();
        var (service, id) = await CreateAsync(new FakeKnowledgeBase(Match("Impresora", 0.55)), spy);

        var result = await service.AssistAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Answer);
        Assert.Empty(result.Sources);
        Assert.Null(spy.ReceivedSources);   // la IA no llegó a invocarse
    }

    [Fact]
    public async Task AssistAsync_TicketInexistente_DevuelveNull()
    {
        var (service, _) = await CreateAsync(new FakeKnowledgeBase(), new SpyAnswerGenerator());

        Assert.Null(await service.AssistAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
