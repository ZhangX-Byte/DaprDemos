using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using StorageService.Api;
using Xunit;

namespace StorageIntegrationTest
{
    public sealed class StorageControllerIntegrationTest: IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        public StorageControllerIntegrationTest(CustomWebApplicationFactory<Startup> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        private readonly HttpClient _client;

        [Fact]
        public async Task InitialStorage_Success()
        {
            const string uri = "/api/Storage/InitialStorage";
            HttpResponseMessage httpResponseMessage = await _client.GetAsync(uri);

            httpResponseMessage.EnsureSuccessStatusCode();

            string resultString = await httpResponseMessage.Content.ReadAsStringAsync();
        }
    }
}
