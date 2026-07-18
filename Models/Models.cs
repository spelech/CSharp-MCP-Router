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
        public string Category { get; set; } = "default";
        public string? ApiKey { get; set; }
        public string? HeadersJson { get; set; } // JSON dictionary of custom headers
    }

    public class OAuthClient
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string RedirectUrisJson { get; set; } = "[]"; // JSON array of redirect URIs
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class RouterDbContext : DbContext
    {
        private readonly string _encryptionKey;

        public DbSet<McpServer> Servers => Set<McpServer>();
        public DbSet<OAuthClient> Clients => Set<OAuthClient>();
        public DbSet<RouterSettings> Settings => Set<RouterSettings>();

        public RouterDbContext(DbContextOptions<RouterDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _encryptionKey = configuration["DB_ENCRYPTION_KEY"] ?? "DefaultSecureKey123!";
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

            // Register OpenIddict Entity Framework Core entities
            modelBuilder.UseOpenIddict();
        }
    }
}
