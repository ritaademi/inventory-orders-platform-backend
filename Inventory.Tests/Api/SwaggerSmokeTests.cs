using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Tests.Api
{
    public class SwaggerSmokeTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public SwaggerSmokeTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task Swagger_endpoint_is_up()
        {
            var res = await _client.GetAsync("/swagger");
            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
