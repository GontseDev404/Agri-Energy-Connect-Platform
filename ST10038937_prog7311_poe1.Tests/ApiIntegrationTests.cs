using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using ST10038937_prog7311_poe1;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_Products_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/Products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Farmers_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/Farmers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ForumPosts_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/Forum");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_AuditLogs_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/AuditLogs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Product_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/Products/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Farmer_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/Farmers/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ForumPost_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/Forum/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_AuditLog_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/AuditLogs/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // First, get the login page to extract the anti-forgery token
        var loginPageResponse = await client.GetAsync("/Identity/Account/Login");
        var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();
        
        // Extract the anti-forgery token
        var tokenMatch = Regex.Match(loginPageContent, @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""");
        var antiForgeryToken = tokenMatch.Success ? tokenMatch.Groups[1].Value : "";

        var loginData = new Dictionary<string, string>
        {
            { "Input.Email", email },
            { "Input.Password", password },
            { "Input.RememberMe", "false" },
            { "__RequestVerificationToken", antiForgeryToken }
        };
        
        var content = new FormUrlEncodedContent(loginData);
        var response = await client.PostAsync("/Identity/Account/Login", content);
        
        // Check if login was successful (should redirect or return success)
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            // If login fails, throw an exception with details
            var responseContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Login failed for {email}: {response.StatusCode}. Content: {responseContent}");
        }
        
        // The auth cookie is now set in the client
        return client;
    }

    [Fact]
    public async Task Authenticated_Forum_CRUD_Works()
    {
        try
        {
            // Use seeded Employee account for Forum management
            var client = await GetAuthenticatedClientAsync("employee@agrienergy.com", "Employee1!");

            // CREATE
            var post = new
            {
                title = "Integration Auth Forum Post",
                content = "Test Content"
            };
            var createResp = await client.PostAsJsonAsync("/api/Forum", post);
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

            // GET (list)
            var getResp = await client.GetAsync("/api/Forum");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

            // GET (single, should find the created post)
            var getOneResp = await client.GetAsync("/api/Forum/1");
            Assert.Equal(HttpStatusCode.OK, getOneResp.StatusCode);

            // UPDATE (should work for the creator)
            var update = new { title = "Updated Title" };
            var updateResp = await client.PutAsJsonAsync("/api/Forum/1", update);
            Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);

            // ADD REPLY (should work)
            var reply = new { content = "Test reply" };
            var replyResp = await client.PostAsJsonAsync("/api/Forum/1/replies", reply);
            Assert.Equal(HttpStatusCode.Created, replyResp.StatusCode);

            // DELETE (should work for the creator)
            var deleteResp = await client.DeleteAsync("/api/Forum/1");
            Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
        }
        catch (Exception ex) when (ex.Message.Contains("Login failed"))
        {
            // Skip test if login fails (user might not exist in test environment)
            Assert.Fail("Login failed - test user may not exist in test environment");
        }
    }

    [Fact]
    public async Task Authenticated_Farmer_CRUD_Works()
    {
        try
        {
            // Use seeded Employee account for Farmer management
            var client = await GetAuthenticatedClientAsync("employee@agrienergy.com", "Employee1!");

            // GET (list)
            var getResp = await client.GetAsync("/api/Farmers");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

            // GET (single, should find seeded farmers)
            var getOneResp = await client.GetAsync("/api/Farmers/1");
            Assert.Equal(HttpStatusCode.OK, getOneResp.StatusCode);

            // UPDATE (should work for admin/employee)
            var update = new { name = "Updated Farmer" };
            var updateResp = await client.PutAsJsonAsync("/api/Farmers/1", update);
            Assert.Equal(HttpStatusCode.NoContent, updateResp.StatusCode);
        }
        catch (Exception ex) when (ex.Message.Contains("Login failed"))
        {
            // Skip test if login fails (user might not exist in test environment)
            Assert.Fail("Login failed - test user may not exist in test environment");
        }
    }

    [Fact]
    public async Task Authenticated_Product_CRUD_Works()
    {
        try
        {
            // Use seeded Employee account
            var client = await GetAuthenticatedClientAsync("employee@agrienergy.com", "Employee1!");

            // GET (list) - Should work for authenticated users
            var getResp = await client.GetAsync("/api/Products");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

            // GET (single, should find seeded products)
            var getOneResp = await client.GetAsync("/api/Products/1");
            Assert.Equal(HttpStatusCode.OK, getOneResp.StatusCode);
        }
        catch (Exception ex) when (ex.Message.Contains("Login failed"))
        {
            // Skip test if login fails (user might not exist in test environment)
            Assert.Fail("Login failed - test user may not exist in test environment");
        }
    }

    [Fact]
    public async Task Authenticated_AuditLogs_Works()
    {
        try
        {
            // Use seeded Admin account (if exists)
            var client = await GetAuthenticatedClientAsync("admin@agrienergy.com", "Admin1!");
            
            // GET (list) - Should work for admin
            var getResp = await client.GetAsync("/api/AuditLogs");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        }
        catch (Exception ex) when (ex.Message.Contains("Login failed"))
        {
            // Skip test if login fails (user might not exist in test environment)
            Assert.Fail("Login failed - test user may not exist in test environment");
        }
    }
} 