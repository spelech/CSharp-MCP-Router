using System;
using System.Collections.Generic;

namespace McpRouter.Components.Clients
{
    public class CreateClientModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public List<string> Scopes { get; set; } = new();
        public int? ExpiresInDays { get; set; }
    }
}
