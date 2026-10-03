using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RockTracker.Api.Tests.Integration;

public class ExceptionHandlingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExceptionHandlingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnhandledException_IsSanitized_WhenThrownDuringRequest()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter>(new TestExceptionStartupFilter());
            });
        });

        var client = factory.CreateAuthorizedClient();

        var resp = await client.GetAsync("/test-exception");

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, resp.StatusCode);
        Assert.Equal("application/problem+json", resp.Content.Headers.ContentType.MediaType);

        var txt = await resp.Content.ReadAsStringAsync();
        var problem = System.Text.Json.JsonSerializer.Deserialize<Microsoft.AspNetCore.Mvc.ProblemDetails>(txt, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.Equal("An unexpected error occurred.", problem?.Title);
        Assert.DoesNotContain("sensitive details", txt);
    }

    private class TestExceptionStartupFilter : Microsoft.AspNetCore.Hosting.IStartupFilter
    {
        public Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);

                app.Use(next => async context =>
                {
                    if (context.Request.Path == "/test-exception")
                        throw new InvalidOperationException("sensitive details should not be returned");

                    await next(context);
                });
            };
        }
    }
}
