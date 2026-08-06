using System;
using Microsoft.EntityFrameworkCore;

namespace McpRouter.Models
{
    public class McpServer
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public bool Hidden { get; set; }
        public string Type { get; set; } = "sse"; // "sse" or "http"
        public string SecretProvider { get; set; } = "Vault"; // "Vault", "WindowsRegistry", "Environment", "None"
        public string? SecretItemKey { get; set; }
        public string AuthShape { get; set; } = "bearer"; // "bearer", "basic", "raw", "x-api-key", "custom-header", "query"
        public string? CustomHeaderName { get; set; }
        public System.Collections.Generic.List<string> Categories { get; set; } = new();
        public string? ApiKey { get; set; }
        public string? HeadersJson { get; set; } // JSON dictionary of custom headers
        public bool AutoDiscovered { get; set; } = false;
    }

    public class OAuthClient
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string RedirectUrisJson { get; set; } = "[]"; // JSON array of redirect URIs
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class McpAccessPolicy
    {
        public string Id { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty; // e.g., "server:ha", "tool:docker__list_containers", "prompt:router__diagnose_failure", "resource:router://status"
        public string RequiredGroup { get; set; } = string.Empty;
        public bool IsAllowed { get; set; } = true;
    }

    public class GroupMapping
    {
        public string Id { get; set; } = string.Empty; // Guid or key
        public string ExternalId { get; set; } = string.Empty; // e.g., S-1-5-... or pocketid_admins
        public string InternalGroup { get; set; } = string.Empty; // e.g., database_users
    }

    public class RouterDbContext : DbContext
    {
        private readonly string _encryptionKey;

        public DbSet<McpServer> Servers => Set<McpServer>();
        public DbSet<OAuthClient> Clients => Set<OAuthClient>();
        public DbSet<RouterSettings> Settings => Set<RouterSettings>();
        public DbSet<McpAccessPolicy> AccessPolicies => Set<McpAccessPolicy>();
        public DbSet<GroupMapping> GroupMappings => Set<GroupMapping>();
        public DbSet<AppKey> AppKeys => Set<AppKey>();

        public RouterDbContext(DbContextOptions<RouterDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _encryptionKey = McpRouter.Core.Secrets.EncryptionKeyProvider.GetDbEncryptionKey(configuration);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // In Docker, the database path is in the /app/data volume
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "mcp_router.db");
                var dir = Path.GetDirectoryName(dbPath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Password=... is how Microsoft.Data.Sqlite with SQLCipher applies the key
                optionsBuilder.UseSqlite($"Data Source={dbPath};Password={_encryptionKey}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<McpServer>().HasKey(s => s.Id);
            modelBuilder.Entity<OAuthClient>().HasKey(c => c.ClientId);
            modelBuilder.Entity<McpAccessPolicy>().HasKey(p => p.Id);
            modelBuilder.Entity<GroupMapping>().HasKey(m => m.Id);
            modelBuilder.Entity<AppKey>().HasKey(k => k.Id);

            // Register OpenIddict Entity Framework Core entities
            modelBuilder.UseOpenIddict();
        }
    }
}
