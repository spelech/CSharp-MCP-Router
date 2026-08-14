using System;
using System.Threading.Tasks;
using McpRouter.Core.Secrets;
using Xunit;

namespace McpRouter.Tests
{
    public class EnvironmentSecretRetrieverTests
    {
        [Fact]
        public async Task EnvironmentSecretRetriever_RetrievesSecret_FromEnvironmentVariables()
        {
            var retriever = new EnvironmentSecretRetriever();
            Assert.Equal("Environment", retriever.ProviderName);

            Environment.SetEnvironmentVariable("TEST_ENV_KEY_123", "secret_value_77");
            Environment.SetEnvironmentVariable("TEST_ENV_PATH_456", "path_value_88");

            try
            {
                var val1 = await retriever.GetSecretAsync("", "TEST_ENV_KEY_123");
                Assert.Equal("secret_value_77", val1);

                var val2 = await retriever.GetSecretAsync("TEST_ENV_PATH_456", "NON_EXISTENT_KEY");
                Assert.Equal("path_value_88", val2);

                var val3 = await retriever.GetSecretAsync("NON_EXISTENT_PATH", "NON_EXISTENT_KEY");
                Assert.Null(val3);
            }
            finally
            {
                Environment.SetEnvironmentVariable("TEST_ENV_KEY_123", null);
                Environment.SetEnvironmentVariable("TEST_ENV_PATH_456", null);
            }
        }
    }
}
