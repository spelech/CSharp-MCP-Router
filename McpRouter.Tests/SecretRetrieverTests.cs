using McpRouter.Tests.Attributes;
using System;
using System.Threading.Tasks;
using McpRouter.Infrastructure.Secrets;
using Xunit;

namespace McpRouter.Tests
{
    public class SecretRetrieverTests
    {
        [Fact]
        [Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy allows principal")]
        public async Task EnvironmentSecretRetriever_ReturnsEnvVariable_WhenExists()
        {
            const string envKey = "TEST_MCP_ROUTER_SECRET_VAR";
            const string envVal = "SuperSecret123!";
            Environment.SetEnvironmentVariable(envKey, envVal);

            try
            {
                var retriever = new EnvironmentSecretRetriever();
                var result = await retriever.GetSecretAsync("", envKey);
                Assert.Equal(envVal, result);
            }
            finally
            {
                Environment.SetEnvironmentVariable(envKey, null);
            }
        }

        [Fact]

        [Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy allows principal")]
        public async Task EnvironmentSecretRetriever_ReturnsNull_WhenVariableDoesNotExist()
        {
            var retriever = new EnvironmentSecretRetriever();
            var result = await retriever.GetSecretAsync("", "NON_EXISTENT_VAR_999999");
            Assert.Null(result);
        }

        [Fact]

        [Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy allows principal")]
        public async Task WindowsRegistrySecretRetriever_HandlesNonWindowsGracefully()
        {
            var retriever = new WindowsRegistrySecretRetriever();
            var result = await retriever.GetSecretAsync("HKCU\\Software\\NonExistentPath", "SecretKey");
            Assert.Null(result);
        }
    }
}
