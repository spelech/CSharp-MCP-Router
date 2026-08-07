using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Core.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace McpRouter.Controllers
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

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class ProvidersController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;

        public ProvidersController(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        [HttpGet("secrets")]
        public async Task<IActionResult> GetSecretProviders()
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                const string sql = "SELECT ProviderName, DisplayName, EncryptedConfigJson AS ConfigJson, IsEnabled FROM SecretProviders;";
                var providers = await conn.QueryAsync<SecretProviderDto>(sql);
                return Ok(providers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("secrets")]
        public async Task<IActionResult> SaveSecretProvider([FromBody] SecretProviderDto dto)
        {
            if (string.IsNullOrEmpty(dto.ProviderName)) return BadRequest(new { error = "ProviderName is required" });

            try
            {
                McpRouter.Core.Security.SecurityValidationHelper.ValidateJsonUrlsRequireHttps(dto.ConfigJson);

                using var conn = _dbFactory.CreateConnection();
                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string sql = @"
                        INSERT INTO SecretProviders (ProviderName, DisplayName, EncryptedConfigJson, IsEnabled)
                        VALUES (@ProviderName, @DisplayName, @ConfigJson, @IsEnabled)
                        ON CONFLICT(ProviderName) DO UPDATE SET DisplayName = @DisplayName, EncryptedConfigJson = @ConfigJson, IsEnabled = @IsEnabled;";
                    await conn.ExecuteAsync(sql, dto);
                }
                else
                {
                    await conn.ExecuteAsync("sp_SaveSecretProvider", new {
                        dto.ProviderName,
                        dto.DisplayName,
                        EncryptedConfigJson = dto.ConfigJson,
                        dto.IsEnabled
                    }, commandType: System.Data.CommandType.StoredProcedure);
                }

                return Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("auth")]
        public async Task<IActionResult> GetAuthProviders()
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                const string sql = "SELECT ProviderName, DisplayName, UserHeader, GroupsHeader, ConfigJson, IsEnabled FROM AuthProviderConfigs;";
                var providers = await conn.QueryAsync<AuthProviderDto>(sql);
                return Ok(providers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("auth")]
        public async Task<IActionResult> SaveAuthProvider([FromBody] AuthProviderDto dto)
        {
            if (string.IsNullOrEmpty(dto.ProviderName)) return BadRequest(new { error = "ProviderName is required" });

            try
            {
                McpRouter.Core.Security.SecurityValidationHelper.ValidateJsonUrlsRequireHttps(dto.ConfigJson);

                using var conn = _dbFactory.CreateConnection();
                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string sql = @"
                        INSERT INTO AuthProviderConfigs (ProviderName, DisplayName, UserHeader, GroupsHeader, ConfigJson, IsEnabled)
                        VALUES (@ProviderName, @DisplayName, @UserHeader, @GroupsHeader, @ConfigJson, @IsEnabled)
                        ON CONFLICT(ProviderName) DO UPDATE SET DisplayName = @DisplayName, UserHeader = @UserHeader, GroupsHeader = @GroupsHeader, ConfigJson = @ConfigJson, IsEnabled = @IsEnabled;";
                    await conn.ExecuteAsync(sql, dto);
                }
                else
                {
                    await conn.ExecuteAsync("sp_SaveAuthProvider", new {
                        dto.ProviderName,
                        dto.DisplayName,
                        dto.UserHeader,
                        dto.GroupsHeader,
                        dto.IsEnabled
                    }, commandType: System.Data.CommandType.StoredProcedure);
                }

                return Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
