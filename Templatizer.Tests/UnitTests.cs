using Microsoft.Extensions.Logging;
using Xunit;
using Templatizer.Controllers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Templatizer.Tests
{
    public class UnitTests
    {
        [Fact]
        public void Test1()
        {
            var logger = Mock.Of<ILogger<GitHubController>>();
            var config = Mock.Of<IConfiguration>();
            var server = new GitHubController(logger, config);
            Assert.True(true);
        }
    }
}
