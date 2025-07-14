using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST10038937_prog7311_poe1.Models;
using System.Diagnostics;

namespace ST10038937_prog7311_poe1.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            try
            {
                using (var context = new ApplicationDbContext(
                    serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
                {
                    // Create database if it doesn't exist and apply migrations
                    context.Database.EnsureCreated();
                    Debug.WriteLine("Database created successfully.");

                    // Check if roles exist
                    if (!await roleManager.RoleExistsAsync("Farmer"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Farmer"));
                    }
                    
                    if (!await roleManager.RoleExistsAsync("Employee"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Employee"));
                    }
                    // Add Admin role
                    if (!await roleManager.RoleExistsAsync("Admin"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Admin"));
                    }

                    // Add sample admin user if it doesn't exist
                    var adminEmail = "admin@agrienergy.com";
                    if (await userManager.FindByEmailAsync(adminEmail) == null)
                    {
                        var admin = new ApplicationUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true,
                            FirstName = "System",
                            LastName = "Administrator",
                            UserRole = "Admin"
                        };
                        var result = await userManager.CreateAsync(admin, "Admin1!");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(admin, "Admin");
                        }
                    }

                    // Add sample employee user if it doesn't exist
                    var employeeEmail = "employee@agrienergy.com";
                    if (await userManager.FindByEmailAsync(employeeEmail) == null)
                    {
                        var employee = new ApplicationUser
                        {
                            UserName = employeeEmail,
                            Email = employeeEmail,
                            EmailConfirmed = true,
                            FirstName = "Admin",
                            LastName = "User",
                            UserRole = "Employee"
                        };

                        var result = await userManager.CreateAsync(employee, "Employee1!");
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(employee, "Employee");
                        }
                }

                    // Add sample farmers if they don't exist
                    if (!context.Farmers.Any())
                    {
                        // Create sample farmer user accounts
                        string[] farmerEmails = { "john@farm.com", "sarah@greenfarm.com", "michael@organicfarm.com" };
                        string[] farmerNames = { "John Smith", "Sarah Johnson", "Michael Brown" };
                        string[] farmNames = { "Smith Family Farm", "Green Valley Farm", "Organic Harvest Farm" };
                        var random = new Random(); // Create random instance here
                        
                        for (int i = 0; i < farmerEmails.Length; i++)
                        {
                            var farmerEmail = farmerEmails[i];
                            if (await userManager.FindByEmailAsync(farmerEmail) == null)
                            {
                                var farmer = new ApplicationUser
                                {
                                    UserName = farmerEmail,
                                    Email = farmerEmail,
                                    EmailConfirmed = true,
                                    FirstName = farmerNames[i].Split(' ')[0],
                                    LastName = farmerNames[i].Split(' ')[1],
                                    UserRole = "Farmer"
                                };

                                var result = await userManager.CreateAsync(farmer, "Farmer1!");
                                if (result.Succeeded)
                                {
                                    await userManager.AddToRoleAsync(farmer, "Farmer");
                                    
                                    // Create farmer profile
                                    var farmerProfile = new Farmer
                                    {
                                        Name = farmerNames[i],
                                        FarmName = farmNames[i],
                                        Address = $"{i+100} Farm Road, Farmville",
                                        PhoneNumber = GenerateSouthAfricanPhoneNumber(random),
                                        Email = farmerEmail,
                                        UserId = farmer.Id
                                    };
                                
                                    context.Farmers.Add(farmerProfile);
                                    await context.SaveChangesAsync();
                                    
                                    // Add sample products for this farmer
                                    var categories = new[] { "Vegetables", "Fruits", "Dairy", "Grains" };
                                    var productNames = new[]
                                    {
                                        new[] { "Carrots", "Potatoes", "Lettuce", "Tomatoes" },
                                        new[] { "Apples", "Oranges", "Strawberries", "Blueberries" },
                                        new[] { "Milk", "Cheese", "Yogurt", "Butter" },
                                        new[] { "Wheat", "Corn", "Barley", "Oats" }
                                    };
                                    
                                    // Using the random instance declared above
                                    
                                    for (int c = 0; c < categories.Length; c++)
                                    {
                                        var category = categories[c];
                                        var products = productNames[c];
                                        
                                        for (int p = 0; p < products.Length; p++)
                                        {
                                            if (random.Next(0, 2) == 0) // 50% chance to add this product
                                                continue;
                                                
                                            var product = new Product
                                            {
                                                Name = products[p],
                                                Category = category,
                                                ProductionDate = DateTime.Now.AddDays(-random.Next(1, 30)),
                                                Price = Math.Round((decimal)(random.NextDouble() * 200 + 50), 2), // South African Rands (ZAR)
                                                Description = $"Fresh {products[p]} from {farmNames[i]}",
                                                QuantityAvailable = random.Next(10, 100),
                                                FarmerId = farmerProfile.FarmerId
                                            };
                                            
                                            context.Products.Add(product);
                                        }
                                    }
                                    
                                    await context.SaveChangesAsync();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't throw it to prevent application startup failure
                Console.WriteLine($"Error seeding database: {ex.Message}");
            }
        }
        
        // Helper method to generate realistic South African phone numbers
        private static string GenerateSouthAfricanPhoneNumber(Random random)
        {
            // South African mobile prefixes
            string[] mobilePrefixes = { "060", "061", "062", "063", "064", "065", "066", "067", "068", "071", "072", "073", "074", "076", "078", "079", "081", "082", "083", "084" };
            
            // Generate a random prefix
            string prefix = mobilePrefixes[random.Next(mobilePrefixes.Length)];
            
            // Generate the remaining 7 digits
            int firstPart = random.Next(100, 999); // 3 digits
            int secondPart = random.Next(1000, 9999); // 4 digits
            
            // Format as +27 XX XXX XXXX (standard SA format with international code)
            return $"+27 {prefix} {firstPart} {secondPart}";
        }
    }
}
