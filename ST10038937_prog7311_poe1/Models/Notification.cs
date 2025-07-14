using System.ComponentModel.DataAnnotations;

namespace ST10038937_prog7311_poe1.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; }
        
        public NotificationPriority Priority { get; set; }
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReadAt { get; set; }
        
        // Optional: Link to related entity
        public string? RelatedEntityType { get; set; }
        public string? RelatedEntityId { get; set; }
        
        // Navigation property
        public ApplicationUser User { get; set; } = null!;
    }
    
    public enum NotificationType
    {
        ProductUpdate,
        ForumPost,
        ForumReply,
        SystemAlert,
        Welcome,
        SecurityAlert,
        AuditLog
    }
    
    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
} 