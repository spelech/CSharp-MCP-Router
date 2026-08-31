namespace ModelContextGateway.Tests
{
    public class SecretRetrieverTests
    {
        [Fact]
        [Requirement("SEC-03", "SEC", RequirementType.Positive, "EnvironmentSecretRetriever retrieves configured environment variable value.")]
        public async Task EnvironmentSecretRetriever_ReturnsEnvVariable_WhenExists()
        {
            const string envKey = "TEST_MCG_SECRET_VAR";
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
        [Requirement("SEC-03", "SEC", RequirementType.Positive, "EnvironmentSecretRetriever returns null when environment variable does not exist.")]
        public async Task EnvironmentSecretRetriever_ReturnsNull_WhenVariableDoesNotExist()
        {
            var retriever = new EnvironmentSecretRetriever();
            var result = await retriever.GetSecretAsync("", "NON_EXISTENT_VAR_999999");
            Assert.Null(result);
        }

        [Fact]
        [Requirement("SEC-04", "SEC", RequirementType.Positive, "WindowsRegistrySecretRetriever handles non-Windows platforms gracefully and returns null.")]
        public async Task WindowsRegistrySecretRetriever_HandlesNonWindowsGracefully()
        {
            var retriever = new WindowsRegistrySecretRetriever();
            var result = await retriever.GetSecretAsync("HKCU\\Software\\NonExistentPath", "SecretKey");
            Assert.Null(result);
        }
    }
}
