namespace McpRouter.Tests
{
    public class SecretRetrieverTests
    {
        [Fact]
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
        public async Task EnvironmentSecretRetriever_ReturnsNull_WhenVariableDoesNotExist()
        {
            var retriever = new EnvironmentSecretRetriever();
            var result = await retriever.GetSecretAsync("", "NON_EXISTENT_VAR_999999");
            Assert.Null(result);
        }

        [Fact]
        public async Task WindowsRegistrySecretRetriever_HandlesNonWindowsGracefully()
        {
            var retriever = new WindowsRegistrySecretRetriever();
            var result = await retriever.GetSecretAsync("HKCU\\Software\\NonExistentPath", "SecretKey");
            Assert.Null(result);
        }
    }
}
