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

    public class TestLdapRequest
    {
        public string Server { get; set; } = string.Empty;
        public int? Port { get; set; } = 636;
        public bool? UseSsl { get; set; } = true;
        public string? Domain { get; set; }
        public string? BaseDn { get; set; }
        public string? BindDn { get; set; }
        public string? BindPassword { get; set; }
    }

    public class TestVaultRequest
    {
        public string Address { get; set; } = string.Empty;
        public string? AuthMethod { get; set; } = "token";
        public string? Token { get; set; }
        public string? RoleId { get; set; }
        public string? SecretId { get; set; }
        public string? MountPath { get; set; } = "secret";
    }
}
