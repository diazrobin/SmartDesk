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
        public int SearchCount { get; private set; }

        public Task<KnowledgeArticleDto> AddAsync(string title, string content, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(string query, int top, CancellationToken ct)
        {
            SearchCount++;
            return Task.FromResult<IReadOnlyList<KnowledgeMatch>>(matches);
        }
    }

    /// <summary>Simula la base de conocimiento con la IA caída.</summary>
    private sealed class FailingKnowledgeBase : IKnowledgeBase
    {
        public Task<KnowledgeArticleDto> AddAsync(string title, string content, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(string query, int top, CancellationToken ct) =>
            throw new AiUnavailableException("sin embeddings");
    }

    /// <summary>"Espía": registra con qué fuentes le llamaron.</summary>
    private sealed class SpyAnswerGenerator : IAnswerGenerator
    {
        public IReadOnlyList<KnowledgeMatch>? ReceivedSources { get; private set; }

        public Task<string?> GenerateAsync(string title, string description, IReadOnlyList<KnowledgeMatch> sources, CancellationToken ct)
        {
            ReceivedSources = sources;
            return Task.FromResult<string?>("Prueba esto [1]");
        }
    }

    /// <summary>Simula la redacción caída: cuenta llamadas y siempre falla.</summary>
    private sealed class FailingAnswerGenerator : IAnswerGenerator
    {
        public int Calls { get; private set; }

        public Task<string?> GenerateAsync(string title, string description, IReadOnlyList<KnowledgeMatch> sources, CancellationToken ct)
        {
            Calls++;
            throw new AiUnavailableException("IA caída");
        }
    }

    private sealed class MemoryAssistCache : IAssistCache
    {
        private readonly Dictionary<string, AssistResponse> _items = new();

        public Task<AssistResponse?> GetAsync(string key, CancellationToken ct) =>
            Task.FromResult<AssistResponse?>(_items.GetValueOrDefault(key));

        public Task SetAsync(string key, AssistResponse response, TimeSpan ttl, CancellationToken ct)
        {
            _items[key] = response;
            return Task.CompletedTask;
        }
    }

    private static KnowledgeMatch Match(string title, double similarity) =>
        new(Guid.NewGuid(), title, "contenido", similarity);

    private static async Task<(AssistanceService Service, Guid TicketId)> CreateAsync(
        IKnowledgeBase kb, IAnswerGenerator gen, IAssistCache? cache = null)
    {
        var tickets = new TicketService(
            new InMemoryTicketRepository(), new FakeCurrentUser("ana", IsAgent: false),
            new FakeTicketClassifier(), new FakeTimeProvider());
        var ticket = await tickets.CreateAsync("VPN", "Error 809", CancellationToken.None);
        return (new AssistanceService(tickets, kb, gen, cache ?? new MemoryAssistCache()), ticket.Id);
    }

    [Fact]
    public async Task AssistAsync_SoloPasaALaIALasFuentesRelevantes()
    {
        var spy = new SpyAnswerGenerator();
        var (service, id) = await CreateAsync(new FakeKnowledgeBase(Match("VPN", 0.86), Match("Impresora", 0.62)), spy);

        var result = await service.AssistAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Prueba esto [1]", result.Answer);
        Assert.Equal(AnswerStatus.Generated, result.Status);
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
        Assert.Equal(AnswerStatus.NoRelevantSources, result.Status);
        Assert.Empty(result.Sources);
        Assert.Null(spy.ReceivedSources);
    }

    [Fact]
    public async Task AssistAsync_TicketInexistente_DevuelveNull()
    {
        var (service, _) = await CreateAsync(new FakeKnowledgeBase(), new SpyAnswerGenerator());

        Assert.Null(await service.AssistAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task AssistAsync_SegundaPeticion_SaleDeLaCache()
    {
        var kb = new FakeKnowledgeBase(Match("VPN", 0.86));
        var (service, id) = await CreateAsync(kb, new SpyAnswerGenerator());

        var first = await service.AssistAsync(id, CancellationToken.None);
        var second = await service.AssistAsync(id, CancellationToken.None);

        Assert.Equal(1, kb.SearchCount);
        Assert.Equal(first!.Answer, second!.Answer);
    }

    [Fact]
    public async Task AssistAsync_BusquedaNoDisponible_DevuelveUnavailable()
    {
        var (service, id) = await CreateAsync(new FailingKnowledgeBase(), new SpyAnswerGenerator());

        var result = await service.AssistAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AnswerStatus.Unavailable, result.Status);
        Assert.Empty(result.Sources);
    }

    [Fact]
    public async Task AssistAsync_IACaida_NoSeCachea()
    {
        var generator = new FailingAnswerGenerator();
        var (service, id) = await CreateAsync(new FakeKnowledgeBase(Match("VPN", 0.86)), generator);

        var first = await service.AssistAsync(id, CancellationToken.None);
        var second = await service.AssistAsync(id, CancellationToken.None);

        Assert.Equal(AnswerStatus.Unavailable, first!.Status);
        Assert.Equal(AnswerStatus.Unavailable, second!.Status);
        Assert.Equal(2, generator.Calls);   // la segunda vuelve a intentarlo: el fallo no quedó en caché
    }
}
