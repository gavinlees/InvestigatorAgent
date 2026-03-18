using FluentAssertions;
using InvestigatorAgent.Resilience;
using Polly;

namespace InvestigatorAgent.Tests.Resilience;

/// <summary>
/// Unit tests for <see cref="RetryPolicies"/>.
/// </summary>
public sealed class ResilienceTests
{
    [Fact]
    public void CreateLlmRetryPolicy_ReturnsNonNullPolicy()
    {
        // Arrange
        var config = new RetryConfiguration { MaxRetryAttempts = 3 };

        // Act
        var policy = RetryPolicies.CreateLlmRetryPolicy(config);

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void CreateToolRetryPolicy_ReturnsNonNullPolicy()
    {
        // Arrange
        var config = new RetryConfiguration { MaxRetryAttempts = 5 };

        // Act
        var policy = RetryPolicies.CreateToolRetryPolicy(config);

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task LlmRetryPolicy_ExecutesSuccessfullyOnSuccess()
    {
        // Arrange
        var config = new RetryConfiguration { MaxRetryAttempts = 3 };
        var policy = RetryPolicies.CreateLlmRetryPolicy(config);
        int executionCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            executionCount++;
            return await Task.FromResult("Success");
        });

        // Assert
        result.Should().Be("Success");
        executionCount.Should().Be(1);
    }
}
