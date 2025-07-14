using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ST10038937_prog7311_poe1.Controllers;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using Microsoft.AspNetCore.Identity;
using ST10038937_prog7311_poe1.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

public class FarmerControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfFarmers()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "FarmerControllerTestDb")
            .Options;
        using var context = new ApplicationDbContext(options);
        context.Farmers.Add(new Farmer { Name = "Test Farmer", FarmName = "Test Farm", Email = "farmer@agrienergy.com", UserId = "test-user-id" });
        context.SaveChanges();

        var userManager = MockUserManager();
        var auditService = new Mock<IAuditService>().Object;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var controller = new FarmerController(context, userManager.Object, auditService, memoryCache);

        // Set up a fake user for the controller context (Employee)
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "employee@agrienergy.com"),
            new Claim(ClaimTypes.Role, "Employee")
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await controller.Index(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Farmer>>(viewResult.Model);
        Assert.Single(model);
        Assert.Equal("Test Farmer", model.First().Name);
    }

    // Helper to mock UserManager
    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
    }
} 