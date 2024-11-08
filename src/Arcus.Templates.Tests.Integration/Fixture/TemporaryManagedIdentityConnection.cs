using System;
using Arcus.Templates.Tests.Integration.Configuration;
using Xunit;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    public class ManagedIdentityFixture : IDisposable
    {
        private readonly TemporaryManagedIdentityConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityFixture" /> class.
        /// </summary>
        public ManagedIdentityFixture()
        {
            _connection = TemporaryManagedIdentityConnection.Create(TestTemplatesConfig.Create());
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _connection.Dispose();
        }
    }

    [CollectionDefinition(TestCollections.Integration)]
    public class ManagedIdentityCollection : ICollectionFixture<ManagedIdentityFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    /// <summary>
    /// Represents a temporary managed identity authentication that is set for the duration of the test.
    /// </summary>
    internal class TemporaryManagedIdentityConnection : IDisposable
    {
        private readonly TemporaryEnvironmentVariable[] _environmentVariables;

        private TemporaryManagedIdentityConnection(string clientId, TemporaryEnvironmentVariable[] environmentVariables)
        {
            _environmentVariables = environmentVariables;
            ClientId = clientId;
        }

        /// <summary>
        /// Gets the client ID of the temporary managed identity authentication.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Creates a new <see cref="TemporaryManagedIdentityConnection"/> instance.
        /// </summary>
        public static TemporaryManagedIdentityConnection Create(TestTemplatesConfig config)
        {
            var servicePrincipal = config.GetServicePrincipal();

            var environmentVariables = new[]
            {
                TemporaryEnvironmentVariable.Create("AZURE_TENANT_ID", servicePrincipal.TenantId),
                TemporaryEnvironmentVariable.Create("AZURE_CLIENT_ID", servicePrincipal.ClientId),
                TemporaryEnvironmentVariable.Create("AZURE_CLIENT_SECRET", servicePrincipal.ClientSecret)
            };

            return new TemporaryManagedIdentityConnection(servicePrincipal.ClientId, environmentVariables);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Assert.All(_environmentVariables, variable => variable.Dispose());
        }
    }

    /// <summary>
    /// Represents a temporary environment variable that is set for the duration of the test.
    /// </summary>
    public class TemporaryEnvironmentVariable : IDisposable
    {
        private readonly string _variableName;

        private TemporaryEnvironmentVariable(string variableName)
        {
            _variableName = variableName;
        }

        /// <summary>
        /// Creates a <see cref="TemporaryEnvironmentVariable"/> test fixture.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="variableName"/> is blank.</exception>
        public static TemporaryEnvironmentVariable Create(string variableName, string variableValue)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new ArgumentException("Environment variable name cannot be blank", nameof(variableValue));
            }

            Environment.SetEnvironmentVariable(variableName, variableValue);
            return new TemporaryEnvironmentVariable(variableName);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_variableName, null);
        }
    }
}
