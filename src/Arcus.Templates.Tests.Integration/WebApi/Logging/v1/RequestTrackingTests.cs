using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.WebApi.Fixture;
using Arcus.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using TestConfig = Arcus.Templates.Tests.Integration.Fixture.TestConfig;

namespace Arcus.Templates.Tests.Integration.WebApi.Logging.v1
{
    [Collection(TestCollections.Integration)]
    [Trait("Category", TestTraits.Integration)]
    public class RequestTrackingTests
    {
        private readonly ITestOutputHelper _outputWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTrackingTests" /> class.
        /// </summary>
        public RequestTrackingTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;
        }

        [Fact]
        public async Task GetSabotagedEndpoint_TracksFailedResponse_ReturnsFailedResponse()
        {
            // Arrange
            var config = TestConfig.Create();
            var options = new WebApiProjectOptions().WithSerilogLogging(config);

            using var project = WebApiProject.CreateNew(config, options, _outputWriter);
            
            project.AddTypeAsFile<SaboteurController>();
            await project.StartAsync();

            // Act
            using HttpResponseMessage response = await project.Root.GetAsync(SaboteurController.Route);
                
            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var client = new AppInsightsClient(config);
            await Poll.UntilAvailableAsync<XunitException>(async () =>
            {
                EventsRequestResult[] requests = await client.GetRequestsAsync();
                Assert.Contains(requests, req => req.Request.Url.Contains("sabotage") && req.Request.ResultCode == "500");
            }, opt => opt.Timeout = TimeSpan.FromMinutes(5));
        }
    }
}
