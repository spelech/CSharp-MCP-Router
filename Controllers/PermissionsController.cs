using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using McpRouter.Core.Database;
using McpRouter.Models;
using Microsoft.AspNetCore.Mvc;

namespace McpRouter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly IDbConnectionFactory _dbFactory;

        public PermissionsController(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        // --- POLICIES ---

        [HttpGet("policies")]
        public async Task<IActionResult> GetPolicies()
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                const string sql = "SELECT Id, TargetId, RequiredGroup, IsAllowed FROM AccessPolicies;";
                var policies = await conn.QueryAsync<McpAccessPolicy>(sql);
                return Ok(policies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("policies")]
        public async Task<IActionResult> SavePolicy([FromBody] McpAccessPolicy policy)
        {
            if (string.IsNullOrEmpty(policy.TargetId)) return BadRequest(new { error = "TargetId is required" });
            if (string.IsNullOrEmpty(policy.RequiredGroup)) return BadRequest(new { error = "RequiredGroup is required" });

            if (string.IsNullOrEmpty(policy.Id))
            {
                policy.Id = Guid.NewGuid().ToString("N");
            }

            try
            {
                using var conn = _dbFactory.CreateConnection();

                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string sql = @"
                        INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed)
                        VALUES (@Id, @TargetId, @RequiredGroup, @IsAllowed)
                        ON CONFLICT(Id) DO UPDATE SET TargetId = @TargetId, RequiredGroup = @RequiredGroup, IsAllowed = @IsAllowed;";
                    await conn.ExecuteAsync(sql, policy);
                }
                else if (_dbFactory.ProviderName == "mysql")
                {
                    const string mysqlSql = @"
                        INSERT INTO AccessPolicies (Id, TargetId, RequiredGroup, IsAllowed)
                        VALUES (@Id, @TargetId, @RequiredGroup, @IsAllowed)
                        ON DUPLICATE KEY UPDATE TargetId = VALUES(TargetId), RequiredGroup = VALUES(RequiredGroup), IsAllowed = VALUES(IsAllowed);";
                    await conn.ExecuteAsync(mysqlSql, policy);
                }
                else // mssql
                {
                    const string mssqlSql = @"
                        MERGE AccessPolicies AS target
                        USING (SELECT @Id AS Id) AS source
                        ON (target.Id = source.Id)
                        WHEN MATCHED THEN
                            UPDATE SET TargetId = @TargetId, RequiredGroup = @RequiredGroup, IsAllowed = @IsAllowed
                        WHEN NOT MATCHED THEN
                            INSERT (Id, TargetId, RequiredGroup, IsAllowed)
                            VALUES (@Id, @TargetId, @RequiredGroup, @IsAllowed);";
                    await conn.ExecuteAsync(mssqlSql, policy);
                }

                return Ok(new { success = true, policy });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("policies/{id}")]
        public async Task<IActionResult> DeletePolicy(string id)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                const string sql = "DELETE FROM AccessPolicies WHERE Id = @Id;";
                await conn.ExecuteAsync(sql, new { Id = id });
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // --- GROUP MAPPINGS ---

        [HttpGet("mappings")]
        public async Task<IActionResult> GetMappings()
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                const string sql = "SELECT Id, ExternalId, InternalGroup FROM GroupMappings;";
                var mappings = await conn.QueryAsync<GroupMapping>(sql);
                return Ok(mappings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("mappings")]
        public async Task<IActionResult> SaveMapping([FromBody] GroupMapping mapping)
        {
            if (string.IsNullOrEmpty(mapping.ExternalId)) return BadRequest(new { error = "ExternalId is required" });
            if (string.IsNullOrEmpty(mapping.InternalGroup)) return BadRequest(new { error = "InternalGroup is required" });

            if (string.IsNullOrEmpty(mapping.Id))
            {
                mapping.Id = Guid.NewGuid().ToString("N");
            }

            try
            {
                using var conn = _dbFactory.CreateConnection();

                if (_dbFactory.ProviderName == "sqlite")
                {
                    const string sql = @"
                        INSERT INTO GroupMappings (Id, ExternalId, InternalGroup)
                        VALUES (@Id, @ExternalId, @InternalGroup)
                        ON CONFLICT(Id) DO UPDATE SET ExternalId = @ExternalId, InternalGroup = @InternalGroup;";
                    await conn.ExecuteAsync(sql, mapping);
                }
                else if (_dbFactory.ProviderName == "mysql")
                {
                    const string mysqlSql = @"
                        INSERT INTO GroupMappings (Id, ExternalId, InternalGroup)
                        VALUES (@Id, @ExternalId, @InternalGroup)
                        ON DUPLICATE KEY UPDATE ExternalId = VALUES(ExternalId), InternalGroup = VALUES(InternalGroup);";
                    await conn.ExecuteAsync(mysqlSql, mapping);
                }
                else // mssql
                {
                    const string mssqlSql = @"
                        MERGE GroupMappings AS target
                        USING (SELECT @Id AS Id) AS source
                        ON (target.Id = source.Id)
                        WHEN MATCHED THEN
                            UPDATE SET ExternalId = @ExternalId, InternalGroup = @InternalGroup
                        WHEN NOT MATCHED THEN
                            INSERT (Id, ExternalId, InternalGroup)
                            VALUES (@Id, @ExternalId, @InternalGroup);";
                    await conn.ExecuteAsync(mssqlSql, mapping);
                }

                return Ok(new { success = true, mapping });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("mappings/{id}")]
        public async Task<IActionResult> DeleteMapping(string id)
        {
            try
            {
                using var conn = _dbFactory.CreateConnection();
                const string sql = "DELETE FROM GroupMappings WHERE Id = @Id;";
                await conn.ExecuteAsync(sql, new { Id = id });
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
