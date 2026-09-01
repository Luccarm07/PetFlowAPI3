namespace PetFlowAPI.Tests;

public class ApiIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_DeveRetornar200ESemDependerDoBanco()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");

        // Act
        using var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Requisicao_DevePreservarCorrelationId()
    {
        // Arrange
        const string correlationId = "teste-sprint3-001";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", correlationId);

        // Act
        using var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(correlationId, response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task RotaInexistente_DeveRetornar404()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/rota-que-nao-existe");

        // Act
        using var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
