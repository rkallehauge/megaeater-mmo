using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using MegaEaterMMO.API.Data.DB;
using Microsoft.Extensions.DependencyInjection;

namespace MegaEaterMMO.Tests;

public class UserTests : IClassFixture<MegaEaterTestFactory>
{
    HttpClient _client;
    MegaEaterContext _context;

    public UserTests(MegaEaterTestFactory factory)
    {
        _client = factory.CreateClient();
        var scope = factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        _context = scopedServices.GetRequiredService<MegaEaterContext>(); // asdfasdfasdfasdf
    }

    [Fact]
    public async Task CreateUser_Login_DeleteAccount()
    {
        // Arrange
        string username, password;
        username = RandomNumberGenerator.GetHexString(16);
        password = RandomNumberGenerator.GetHexString(16);

        var authRequest = new
        {
            Username = username,
            Password = password,
        };
        // Act Assert


        var registerResponse = await _client.PostAsJsonAsync("/api/authentication/register", authRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/authentication/login", authRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var token = JsonSerializer.Deserialize<CredResponse>(await loginResponse.Content.ReadAsStringAsync());
        Assert.NotNull(token);

        HttpRequestMessage deleteRequest = new(HttpMethod.Post, "/api/authentication/requestAccountRemoval");
        deleteRequest.Headers.Add("Authorization", $"Bearer {token.Token}");
        
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        
    }


    // todo : put this in shared class lib
    private class CredResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
