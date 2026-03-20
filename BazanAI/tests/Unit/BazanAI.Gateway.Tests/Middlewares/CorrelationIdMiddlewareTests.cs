using System.Threading.Tasks;
using BazanAI.Gateway.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BazanAI.Gateway.Tests.Middlewares;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldAddCorrelationIdToRequestItems()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (innerContext) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items.Should().ContainKey("X-Correlation-Id");
        context.Items["X-Correlation-Id"].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_ShouldUseExistingCorrelationIdFromHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var existingCorrelationId = "test-correlation-id";
        context.Request.Headers["X-Correlation-Id"] = existingCorrelationId;
        RequestDelegate next = (innerContext) => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items["X-Correlation-Id"].ToString().Should().Be(existingCorrelationId);
    }
}
