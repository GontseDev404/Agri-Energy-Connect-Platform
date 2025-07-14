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

public class ProductControllerTests
{
    [Fact]
    public async Task Index_ReturnsViewResult_WithListOfProducts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "ProductControllerTestDb")
            .Options;
        using var context = new ApplicationDbContext(options);
        // Seed a farmer and a product for the test user
        var farmer = new Farmer { FarmerId = 1, Name = "Test Farmer", FarmName = "Test Farm", Email = "testuser@agrienergy.com", UserId = "test-user-id" };
        context.Farmers.Add(farmer);
        context.Products.Add(new Product { Name = "Test Product", Category = "Test", Price = 10, QuantityAvailable = 5, FarmerId = farmer.FarmerId });
        context.SaveChanges();

        var userManager = MockUserManager();
        userManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser { Id = "test-user-id", Email = "testuser@agrienergy.com", UserName = "testuser@agrienergy.com" });
        var notifier = new ProductNotifier();
        var auditService = new Mock<IAuditService>().Object;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var controller = new ProductController(context, userManager.Object, notifier, auditService, memoryCache);

        // Set up a fake user for the controller context (Farmer)
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "testuser@agrienergy.com"),
            new Claim(ClaimTypes.Role, "Farmer")
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await controller.Index(null, null, null, null, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);
        Assert.Single(model);
        Assert.Equal("Test Product", model.First().Name);
    }

    // Helper to mock UserManager
    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
    }
} 