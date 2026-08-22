using System;
using System.ComponentModel.DataAnnotations;

namespace McpRouter.Components.AppKeys
{
    public class UserQuota
    {
        [Key]
        public string Username { get; set; } = string.Empty;
        public int MaxKeys { get; set; } = 5;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SetUserQuotaRequest
    {
        public string Username { get; set; } = string.Empty;
        public int MaxKeys { get; set; } = 5;
    }
}
