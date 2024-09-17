using Arcus.Templates.Tests.Integration.Fixture;
using Azure.Core;
using Azure.Identity;

namespace Arcus.Templates.Tests.Integration.Configuration
{
    public class ServicePrincipalConfig
    {
        public ServicePrincipalConfig(string tenantId, string clientId, string clientSecret)
        {
            TenantId = tenantId;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        public string TenantId { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }

        public TokenCredential GetCredential()
        {
            return new ClientSecretCredential(TenantId, ClientId, ClientSecret);
        }
    }

    public static class ServicePrincipalTestConfigExtensions
    {
        public static ServicePrincipalConfig GetServicePrincipal(this TestConfig config)
        {
            return new ServicePrincipalConfig(
                config["Arcus:TenantId"],
                config["Arcus:ServicePrincipal:ClientId"],
                config["Arcus:ServicePrincipal:ClientSecret"]);
        }
    }
}
