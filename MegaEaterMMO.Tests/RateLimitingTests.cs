using MegaEaterMMO.API.Data.DB;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace MegaEaterMMO.Tests;

public class RateLimitingTests : IClassFixture<MegaEaterTestFactory>
{
    HttpClient _client;
    MegaEaterContext _context;
    public RateLimitingTests(MegaEaterTestFactory factory)
    {
        _client = factory.CreateClient();
        var scope = factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;
        _context = scopedServices.GetRequiredService<MegaEaterContext>();
    }

    [Fact]
    public async Task Registering_10Times_GetsLimited()
    {
        
        var transaction = await _context.Database.BeginTransactionAsync();
        // Arrange
        List<Task<HttpResponseMessage>> tasks = new();
        for(int i = 0; i<10; i++)
        {
            string username = RandomNumberGenerator.GetHexString(16);
            var req = new AuthReq()
            {
                Username = username,
                Password = RandomNumberGenerator.GetHexString(16)
            };
            tasks.Add(SendRegisterRequest(req));
        }

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.Contains(responses, r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests);


        // Cleanup
        await transaction.RollbackAsync();
    }

    private async Task<HttpResponseMessage> SendRegisterRequest(AuthReq req)
    {
        return await _client.PostAsJsonAsync<AuthReq>("/api/authentication/register", req);
    }

    private class AuthReq
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
