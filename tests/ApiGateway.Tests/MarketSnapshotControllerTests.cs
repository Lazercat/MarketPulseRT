using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ApiGateway.Tests;

public class MarketSnapshotControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MarketSnapshotControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("MarketDataService:GrpcUrl", "http://localhost:5001");
        });
    }

    [Fact]
    public async Task GetTickers_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/market/tickers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload!);
    }
}
