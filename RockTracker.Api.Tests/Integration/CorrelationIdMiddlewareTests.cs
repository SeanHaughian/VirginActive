using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using RockTracker.Api.Common;
using Xunit;

namespace RockTracker.Api.Tests.Integration;

public class CorrelationIdMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorrelationIdMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Response_IncludesGeneratedCorrelationId_WhenNoneSentByClient()
    {
        var client = _factory.CreateAuthorizedClient();

        var response = await client.GetAsync("/members/any-member/rocks");

        response.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var values).Should().BeTrue();
        var correlationId = values!.Should().ContainSingle().Subject;
        Guid.TryParse(correlationId, out _).Should().BeTrue("a correlation id should be generated when the client does not supply one");
    }

    [Fact]
    public async Task Response_EchoesBackSameCorrelationId_WhenClientSuppliesOne()
    {
        var client = _factory.CreateAuthorizedClient();
        var requestCorrelationId = "test-correlation-id-12345";
        client.DefaultRequestHeaders.Add(CorrelationIdConstants.HeaderName, requestCorrelationId);

        var response = await client.GetAsync("/members/any-member/rocks");

        response.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var values).Should().BeTrue();
        values!.Should().ContainSingle().Which.Should().Be(requestCorrelationId);
    }
}
