using System.Net.Http.Json;
using PetFlowAPI.Controllers;
using PetFlowAPI.Security;

namespace PetFlowAPI.Tests;

public class AuthTests
{
    [Fact]
    public void PasswordService_DeveGerarHashEValidarSenha()
    {
        // Arrange
        var sut = new PasswordService();
        const string password = "SenhaSegura123";

        // Act
        var hash = sut.Hash(password);

        // Assert
        Assert.NotEqual(password, hash);
        Assert.True(sut.Verify(password, hash));
        Assert.False(sut.Verify("senha-incorreta", hash));
    }
}

public class AuthIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task TutorsGet_SemToken_DevePermitirAcessoComoNaSprint2()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/tutors");

        // Act
        using var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CadastroELogin_DeveEmitirTokenEPermitirRotaProtegida()
    {
        // Arrange
        var email = $"teste-{Guid.NewGuid():N}@petflow.test";
        var tutor = new { name = "Tutor Teste", email, phone = "11999999999", password = "SenhaSegura123" };

        // Act — cadastro público
        using var createResponse = await _client.PostAsJsonAsync("/tutors", tutor);
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new { email, password = "SenhaSegura123" });
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login!.AccessToken);
        using var protectedResponse = await _client.GetAsync("/tutors");

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task Cadastro_ComPayloadInvalido_DeveRetornar400()
    {
        // Arrange
        using var content = JsonContent.Create(new { });

        // Act
        using var response = await _client.PostAsync("/tutors", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ComSenhaIncorreta_DeveRetornar401()
    {
        // Arrange
        var response = await _client.PostAsJsonAsync("/auth/login", new { email = "nao-existe@petflow.test", password = "errada" });

        // Act
        var status = response.StatusCode;

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }
}
