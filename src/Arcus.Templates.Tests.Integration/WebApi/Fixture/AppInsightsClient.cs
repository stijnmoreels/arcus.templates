using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Configuration;
using Arcus.Templates.Tests.Integration.Fixture;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Newtonsoft.Json;

namespace Arcus.Templates.Tests.Integration.WebApi.Fixture
{
    /// <summary>
    /// Represents a remote client to query telemetry data from the Azure Application Insights instance.
    /// </summary>
    public class AppInsightsClient
    {
        private readonly LogsQueryClient _queryClient;
        private readonly QueryTimeRange _timeRange;
        private readonly string _workspaceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppInsightsClient" /> class.
        /// </summary>
        public AppInsightsClient(TestConfig config)
        {
            ServicePrincipalConfig servicePrincipal = config.GetServicePrincipal();
            _queryClient = new LogsQueryClient(servicePrincipal.GetCredential());
            _workspaceId = config.GetAppInsights().WorkspaceId;
            _timeRange = new QueryTimeRange(TimeSpan.FromDays(1));
        }

        /// <summary>
        /// Gets the tracked requests from the Azure Application Insights instance.
        /// </summary>
        public async Task<EventsRequestResult[]> GetRequestsAsync()
        {
            IReadOnlyCollection<LogsTableRow> rows =
                await QueryLogsAsync(
                    "AppRequests | project Id, Name, Source, Url, Success, ResultCode, AppRoleName, OperationId, ParentId, Properties");

            return rows.Select(row =>
            {
                string id = row[0].ToString();
                string name = row[1].ToString();
                string source = row[2].ToString();
                string url = row[3].ToString();
                bool success = bool.Parse(row[4].ToString() ?? string.Empty);
                string resultCode = row[5].ToString();
                string roleName = row[6].ToString();
                string operationId = row[7].ToString();
                string operationParentId = row[8].ToString();
                var operation = new OperationResult(operationId, operationParentId, name);
                var customDimensionsTxt = row[9].ToString();
                var customDimensions = JsonConvert.DeserializeObject<Dictionary<string, string>>(customDimensionsTxt);
                return new EventsRequestResult(id, name, source, url, success, resultCode, roleName, operation, customDimensions);
                
            }).ToArray();
        }

        private async Task<IReadOnlyCollection<LogsTableRow>> QueryLogsAsync(string query)
        {
            LogsQueryResult response = await _queryClient.QueryWorkspaceAsync(
                _workspaceId,
                query,
                timeRange: _timeRange,
                new LogsQueryOptions { ServerTimeout = TimeSpan.FromSeconds(3) });

            return response.Table.Rows;
        }
    }

    public class EventsRequestResult
    {
        public EventsRequestResult(
            string id,
            string name,
            string source,
            string url,
            bool success,
            string resultCode,
            string roleName,
            OperationResult operation,
            IDictionary<string, string> customDimensions)
        {
            Request = new RequestResult(id, name, source, url, resultCode);
            Cloud = new CloudResult(roleName);
            Success = success;
            Operation = operation;
            CustomDimensions = customDimensions;
        }

        public RequestResult Request { get; }
        public CloudResult Cloud { get; }
        public bool Success { get; }
        public OperationResult Operation { get; }
        public IDictionary<string, string> CustomDimensions { get; }

        public class RequestResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RequestResult" /> class.
            /// </summary>
            public RequestResult(string id, string name, string source, string url, string resultCode)
            {
                Id = id;
                Name = name;
                Source = source;
                Url = url;
                ResultCode = resultCode;
            }
            public string Id { get; }
            public string Name { get; }
            public string Source { get; }
            public string Url { get; }
            public string ResultCode { get; }
        }
    }

    public class OperationResult
    {
        public OperationResult(string id, string parentId)
        {
            Id = id;
            ParentId = parentId;
        }

        public OperationResult(string id, string parentId, string name)
        {
            Id = id;
            ParentId = parentId;
            Name = name;
        }

        public string Id { get; }
        public string Name { get; }
        public string ParentId { get; }
    }

    public class CloudResult
    {
        public CloudResult(string roleName)
        {
            RoleName = roleName;
        }

        public string RoleName { get; }
    }
}
