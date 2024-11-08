using System;
using Arcus.Templates.Tests.Integration.Fixture;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Represents the available options for the Azure Service Bus Topic and Queue worker projects.
    /// </summary>
    public class ServiceBusWorkerProjectOptions : WorkerProjectOptions
    {
        private ServiceBusWorkerProjectOptions(TestTemplatesConfig config) : base(config)
        {
        }

        /// <summary>
        /// Creates an <see cref="ServiceBusWorkerProjectOptions"/> instance that provides additional user-configurable options for the Azure Service Bus .NET Worker projects.
        /// </summary>
        /// <param name="configuration">The integration test configuration instance to retrieve connection secrets.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuration"/> is <c>null</c>.</exception>
        public static ServiceBusWorkerProjectOptions Create(TestTemplatesConfig configuration)
        {
            Guard.NotNull(configuration, nameof(configuration), "Requires a test configuration instance to retrieve additional connection secrets");

            var options = new ServiceBusWorkerProjectOptions(configuration);
            return options;
        }

        /// <summary>
        /// Adds the project option to exclude the Serilog logging infrastructure from the worker project.
        /// </summary>
        public ServiceBusWorkerProjectOptions WithExcludeSerilog()
        {
            ExcludeSerilog();
            return this;
        }
    }
}
