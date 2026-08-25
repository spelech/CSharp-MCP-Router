namespace ModelContextGateway.Components.AppKeys
{
    public class CreateAppKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Username { get; set; } // Admins can assign to other users
        public string KeyType { get; set; } = "personal"; // "personal" | "system"
        public List<string>? Scopes { get; set; }
        public int? ExpiresInDays { get; set; }
    }
}
