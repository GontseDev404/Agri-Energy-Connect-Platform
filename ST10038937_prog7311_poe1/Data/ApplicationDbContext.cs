using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ST10038937_prog7311_poe1.Models;

namespace ST10038937_prog7311_poe1.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Farmer> Farmers { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;
    public DbSet<ForumPost> ForumPosts { get; set; } = default!;
    public DbSet<AuditLog> AuditLogs { get; set; } = default!;
        public DbSet<PostReply> PostReplies { get; set; } = default!;
    public DbSet<Notification> Notifications { get; set; } = default!;
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure relationships
        builder.Entity<Farmer>()
            .HasMany(f => f.Products)
            .WithOne(p => p.Farmer)
            .HasForeignKey(p => p.FarmerId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<ApplicationUser>()
            .HasOne<Farmer>()
            .WithOne(f => f.User)
            .HasForeignKey<Farmer>(f => f.UserId);

        // Add database indexes for performance optimization
        builder.Entity<Farmer>()
            .HasIndex(f => f.UserId)
            .IsUnique();

        builder.Entity<Farmer>()
            .HasIndex(f => f.Email)
            .IsUnique();

        builder.Entity<Farmer>()
            .HasIndex(f => f.Name);

        builder.Entity<Product>()
            .HasIndex(p => p.FarmerId);

        builder.Entity<Product>()
            .HasIndex(p => p.Category);

        builder.Entity<Product>()
            .HasIndex(p => p.ProductionDate);

        builder.Entity<Product>()
            .HasIndex(p => new { p.FarmerId, p.Category });

        builder.Entity<Product>()
            .HasIndex(p => new { p.Category, p.ProductionDate });

        builder.Entity<ForumPost>()
            .HasIndex(p => p.CreatedAt);

        builder.Entity<PostReply>()
            .HasIndex(r => r.ForumPostId);

        builder.Entity<PostReply>()
            .HasIndex(r => r.UserId);

        builder.Entity<PostReply>()
            .HasIndex(r => r.CreatedAt);

        builder.Entity<PostReply>()
            .HasIndex(r => new { r.ForumPostId, r.CreatedAt });

        builder.Entity<AuditLog>()
            .HasIndex(a => a.UserId);

        builder.Entity<AuditLog>()
            .HasIndex(a => a.Action);

        builder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp);

        builder.Entity<AuditLog>()
            .HasIndex(a => new { a.UserId, a.Timestamp });

        builder.Entity<AuditLog>()
            .HasIndex(a => new { a.Action, a.Timestamp });



        // Configure query optimization hints
        builder.Entity<Product>()
            .Property(p => p.Name)
            .HasMaxLength(200);

        builder.Entity<Product>()
            .Property(p => p.Category)
            .HasMaxLength(100);

        builder.Entity<Product>()
            .Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Entity<ForumPost>()
            .Property(p => p.Title)
            .HasMaxLength(200);

        builder.Entity<ForumPost>()
            .Property(p => p.Content)
            .HasMaxLength(5000);

        builder.Entity<PostReply>()
            .Property(r => r.Content)
            .HasMaxLength(2000);

        builder.Entity<AuditLog>()
            .Property(a => a.Action)
            .HasMaxLength(100);

        builder.Entity<AuditLog>()
            .Property(a => a.Details)
            .HasMaxLength(1000);


    }
}
