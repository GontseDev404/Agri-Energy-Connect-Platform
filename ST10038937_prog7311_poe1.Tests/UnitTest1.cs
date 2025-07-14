using System.Threading.Tasks;
using Xunit;
using ST10038937_prog7311_poe1.Services;
using ST10038937_prog7311_poe1.Models;
using ST10038937_prog7311_poe1.Data;
using Microsoft.EntityFrameworkCore;

public class AuditServiceTests
{
    [Fact]
    public async Task LogActionAsync_AddsAuditLogToDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "AuditLogTestDb")
            .Options;
        using var context = new ApplicationDbContext(options);
        var service = new AuditService(context);

        await service.LogActionAsync("user1", "TestAction", "Test details");
        var log = await context.AuditLogs.FirstOrDefaultAsync();

        Assert.NotNull(log);
        Assert.Equal("user1", log.UserId);
        Assert.Equal("TestAction", log.Action);
        Assert.Equal("Test details", log.Details);
    }
}