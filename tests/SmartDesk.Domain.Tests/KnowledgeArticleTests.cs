using Microsoft.Extensions.Time.Testing;
using SmartDesk.Domain.Knowledge;

namespace SmartDesk.Domain.Tests;

public class KnowledgeArticleTests
{
    [Fact]
    public void Create_SinContenido_LanzaArgumentException() =>
        Assert.Throws<ArgumentException>(() => KnowledgeArticle.Create("Título", "   ", new FakeTimeProvider()));
}
