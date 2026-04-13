using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace MegaEaterMMO.Tests;

public class AuthTests : IClassFixture<MegaEaterTestFactory>
{
    private HttpClient _client;

    public AuthTests(MegaEaterTestFactory factory)
    {
        _client = factory.CreateClient();
    }
    

    [Fact]
    public async Task Login_WithoutValidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "invalid_user",
            Password = "invalid_password"
        };
        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Will need to seed a test user in the database for this test to pass
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthorized()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "testuser",
            Password = "testpassword"
        };
        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithIncompleteCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "testuser"
            // Missing password
        };
        // Act
        var response = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutCredentials_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/api/authentication/logout", null);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithCredentials_ReturnsOk()
    {
        // Arrange
        var loginRequest = new
        {
            Username = "testuser",
            Password = "testpassword"
        };

        // get credentials
        var credResponse = await _client.PostAsJsonAsync("/api/authentication/login", loginRequest);
        var credString = await credResponse.Content.ReadAsStringAsync();
        var creds = JsonSerializer.Deserialize<CredResponse>(credString).Token;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/authentication/logout");
        request.Headers.Add("Authorization", $"Bearer {creds}"); // Hardcoded jwt test, bit scuffed

        var response = await _client.SendAsync(request);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    private class CredResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
    // TBD Create user -> Login -> Get token -> Access protected endpoint with token
}
