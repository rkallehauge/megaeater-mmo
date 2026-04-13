using Microsoft.AspNetCore.Mvc.Testing;

namespace MegaEaterMMO.Tests;

public class MarketTests : IClassFixture<MegaEaterTestFactory>
{
    HttpClient _client;

    public MarketTests(MegaEaterTestFactory factory)
    {
        _client = factory.CreateClient();
    }
}
