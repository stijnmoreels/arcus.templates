using Arcus.Templates.Tests.Integration.Fixture;

namespace Arcus.Templates.Tests.Integration.Configuration
{
    public class AppInsightsConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppInsightsConfig" /> class.
        /// </summary>
        public AppInsightsConfig(string connectionString, string workspaceId)
        {
            ConnectionString = connectionString;
            WorkspaceId = workspaceId;
        }
        public string ConnectionString { get; }
        public string WorkspaceId { get; }
    }

    public static class AppInsightsConfigExtensions
    {
        public static AppInsightsConfig GetAppInsights(this TestConfig config)
        {
            return new AppInsightsConfig(
                config["Arcus:ApplicationInsights:ConnectionString"],
                config["Arcus:ApplicationInsights:LogAnalytics:WorkspaceId"]);
        }
    }
}
