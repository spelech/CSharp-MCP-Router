namespace McpRouter.Components.Providers
{
    public class SecretProviderDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ConfigJson { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class AuthProviderDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string UserHeader { get; set; } = "Remote-User";
        public string GroupsHeader { get; set; } = "Remote-Groups";
        public string? ConfigJson { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
