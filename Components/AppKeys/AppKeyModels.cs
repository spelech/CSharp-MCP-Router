using System;
using System.Collections.Generic;

namespace McpRouter.Components.AppKeys
{
    public class CreateAppKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Username { get; set; } // Admins can assign to other users
        public List<string>? Scopes { get; set; }
        public int? ExpiresInDays { get; set; }
    }
}
