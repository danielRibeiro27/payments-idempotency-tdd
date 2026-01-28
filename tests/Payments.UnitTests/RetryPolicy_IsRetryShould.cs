using Payments.Api.Service.Implementations;
using Payments.Api.Service.Interfaces;

namespace Payments.UnitTests;

/// <summary>
/// Unit tests for retry policy behavior.
/// Tests verify correct retry semantics for transient failure handling.
/// </summary>
public class RetryPolicy_IsRetryShould
{
    [Fact]
    public async Task ExecuteAsync_ShouldSucceedOnFirstAttempt()
    {
        var policy = new ExponentialBackoffRetryPolicy(new RetryOptions { MaxRetries = 3 });
        var callCount = 0;

        var result = await policy.ExecuteAsync(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            return "success";
        });

        Assert.Equal("success", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryOnTransientException()
    {
        var policy = new ExponentialBackoffRetryPolicy(new RetryOptions 
        { 
            MaxRetries = 3, 
            InitialDelayMs = 1, 
            UseJitter = false 
        });
        var callCount = 0;

        var result = await policy.ExecuteAsync<string>(async () =>
        {
            callCount++;
            if (callCount < 3)
                throw new HttpRequestException("Network error");
            await Task.CompletedTask;
            return "success after retry";
        });

        Assert.Equal("success after retry", result);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectMaxRetries()
    {
        var policy = new ExponentialBackoffRetryPolicy(new RetryOptions 
        { 
            MaxRetries = 2, 
            InitialDelayMs = 1, 
            UseJitter = false 
        });
        var callCount = 0;

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync<string>(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                throw new HttpRequestException("Persistent network error");
            });
        });

        // Initial attempt + 2 retries = 3 total attempts
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotRetryOnNonTransientException()
    {
        var policy = new ExponentialBackoffRetryPolicy(new RetryOptions { MaxRetries = 3 });
        var callCount = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await policy.ExecuteAsync<string>(async () =>
            {
                callCount++;
                await Task.CompletedTask;
                throw new InvalidOperationException("Business logic error");
            });
        });

        // Should not retry - only 1 attempt
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryBasedOnResultPredicate()
    {
        var policy = new ExponentialBackoffRetryPolicy(new RetryOptions 
        { 
            MaxRetries = 3, 
            InitialDelayMs = 1, 
            UseJitter = false 
        });
        var callCount = 0;

        var result = await policy.ExecuteAsync(
            async () =>
            {
                callCount++;
                await Task.CompletedTask;
                return callCount < 3 ? false : true; // Return failure until third attempt
            },
            shouldRetry: r => !r // Retry if result is false
        );

        Assert.True(result);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryOnTimeout()
    {
        var policy = new ExponentialBackoffRetryPolicy(new RetryOptions 
        { 
            MaxRetries = 2, 
            InitialDelayMs = 1, 
            UseJitter = false 
        });
        var callCount = 0;

        var result = await policy.ExecuteAsync<string>(async () =>
        {
            callCount++;
            if (callCount < 2)
                throw new TaskCanceledException("Timeout");
            await Task.CompletedTask;
            return "success";
        });

        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPerformMultipleRetriesWithBackoff()
    {
        // This test verifies that retries happen with increasing delays
        // Due to timing sensitivity, we only verify the retry count and success
        var policy = new ExponentialBackoffRetryPolicy(new RetryOptions 
        { 
            MaxRetries = 3, 
            InitialDelayMs = 1, // Use minimal delays for fast test execution
            BackoffMultiplier = 2.0,
            UseJitter = false 
        });
        var callCount = 0;

        var result = await policy.ExecuteAsync<string>(async () =>
        {
            callCount++;
            if (callCount < 4)
                throw new HttpRequestException("Network error");
            await Task.CompletedTask;
            return "success";
        });

        // Verify all retries were attempted (1 initial + 3 retries = 4 total)
        Assert.Equal(4, callCount);
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowOnNullOperation()
    {
        var policy = new ExponentialBackoffRetryPolicy();

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await policy.ExecuteAsync<string>(null!);
        });
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() => new ExponentialBackoffRetryPolicy(null!));
    }
}
