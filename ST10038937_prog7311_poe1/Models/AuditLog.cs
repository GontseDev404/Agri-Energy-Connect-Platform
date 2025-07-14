using System.ComponentModel.DataAnnotations;

namespace ST10038937_prog7311_poe1.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }

        [Required]
        public string? Action { get; set; } // e.g., "User Login", "Create Product", "Delete Farmer"

        public string? Details { get; set; } // e.g., "Product ID: 123, Name: Wheat"

        [Required]
        public DateTime Timestamp { get; set; }

        // Security-related properties
        public string? IpAddress { get; set; } // Client IP address

        public string? UserAgent { get; set; } // Browser/Client information

        public bool IsSecurityEvent { get; set; } = false; // Flag for security-related events

        public string? EntityType { get; set; } // Type of entity being accessed (e.g., "Product", "Farmer")

        public string? EntityId { get; set; } // ID of the entity being accessed

        public string? SessionId { get; set; } // Session identifier for tracking user sessions
    }
}
