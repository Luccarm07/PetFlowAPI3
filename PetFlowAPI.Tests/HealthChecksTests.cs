using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using PetFlowAPI.HealthChecks;

namespace PetFlowAPI.Tests;

public class HealthChecksTests
{
    [Fact]
    public async Task ExternalServicesHealthCheck_SemServicosConfigurados_DeveRetornarHealthy()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var sut = new ExternalServicesHealthCheck(httpClientFactory.Object, configuration);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        httpClientFactory.Verify(factory => factory.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExternalServicesHealthCheck_ServicoResponde200_DeveRetornarHealthy()
    {
        // Arrange
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK);
        var client = new HttpClient(handler);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(factory => factory.CreateClient("health-check")).Returns(client);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["HealthChecks:ExternalServices:0"] = "https://service.test/health"
        }).Build();
        var sut = new ExternalServicesHealthCheck(httpClientFactory.Object, configuration);

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("https://service.test/health", handler.LastRequest?.RequestUri?.ToString());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        public HttpRequestMessage? LastRequest { get; private set; }
        public StubHttpMessageHandler(HttpStatusCode statusCode) => _statusCode = statusCode;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}
