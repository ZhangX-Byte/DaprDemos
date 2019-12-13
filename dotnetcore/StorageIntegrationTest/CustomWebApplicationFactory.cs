using Microsoft.AspNetCore.Mvc.Testing;

namespace StorageIntegrationTest
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
    }
}