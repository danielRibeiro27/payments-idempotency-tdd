using Payments.Api.Service.Implementations;

namespace Payments.UnitTests;

/// <summary>
/// Unit tests for RetryService covering retry logic, backoff, and error handling.
/// </summary>
public class RetryServiceTests
{
    private readonly RetryService _retryService;

    public RetryServiceTests()
    {
        _retryService = new RetryService();
    }

    #region ExecuteWithRetryAsync<T> Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenOperationSucceeds_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "success";
        
        // Act
        var result = await _retryService.ExecuteWithRetryAsync(
            () => Task.FromResult(expectedResult));

        // Assert: should return the successful result
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenTransientFailureThenSuccess_ShouldRetryAndSucceed()
    {
        // Arrange
        int attemptCount = 0;
        
        // Act
        var result = await _retryService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TimeoutException("Transient failure");
            }
            return "success";
        }, maxRetries: 3);

        // Assert: should succeed after retry
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenAllRetriesExhausted_ShouldThrowLastException()
    {
        // Arrange
        int attemptCount = 0;
        
        // Act & Assert: should throw after all retries exhausted
        var exception = await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await _retryService.ExecuteWithRetryAsync(async () =>
            {
                attemptCount++;
                throw new TimeoutException($"Attempt {attemptCount}");
            }, maxRetries: 2);
        });

        Assert.Equal(3, attemptCount); // Initial + 2 retries
        Assert.Contains("Attempt 3", exception.Message);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WhenNonTransientFailure_ShouldNotRetry()
    {
        // Arrange
        int attemptCount = 0;
        
        // Act & Assert: non-transient exceptions should not be retried
        // Note: Using "Permanent error" to avoid containing "transient" substring
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _retryService.ExecuteWithRetryAsync(async () =>
            {
                attemptCount++;
                throw new InvalidOperationException("Permanent error - do not retry");
            }, maxRetries: 3);
        });

        Assert.Equal(1, attemptCount); // Should only try once
        Assert.Equal("Permanent error - do not retry", exception.Message);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithZeroRetries_ShouldNotRetry()
    {
        // Arrange
        int attemptCount = 0;
        
        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await _retryService.ExecuteWithRetryAsync(async () =>
            {
                attemptCount++;
                throw new TimeoutException("Timeout");
            }, maxRetries: 0);
        });

        Assert.Equal(1, attemptCount); // Only one attempt, no retries
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _retryService.ExecuteWithRetryAsync<string>(null!);
        });
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNegativeRetries_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await _retryService.ExecuteWithRetryAsync(
                () => Task.FromResult("test"), maxRetries: -1);
        });
    }

    #endregion

    #region ExecuteWithRetryAsync (void) Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_WhenSucceeds_ShouldComplete()
    {
        // Arrange
        bool operationExecuted = false;
        
        // Act
        await _retryService.ExecuteWithRetryAsync(async () =>
        {
            operationExecuted = true;
        });

        // Assert
        Assert.True(operationExecuted);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_WhenTransientFailureThenSuccess_ShouldRetry()
    {
        // Arrange
        int attemptCount = 0;
        
        // Act
        await _retryService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException("Network error");
            }
        }, maxRetries: 3);

        // Assert
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _retryService.ExecuteWithRetryAsync(null!);
        });
    }

    #endregion

    #region Transient Exception Detection Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_TimeoutException_ShouldBeRetried()
    {
        // Arrange
        int attemptCount = 0;
        
        // Act
        var result = await _retryService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TimeoutException();
            }
            return "done";
        });

        // Assert
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_TaskCanceledException_ShouldBeRetried()
    {
        // Arrange
        int attemptCount = 0;
        
        // Act
        var result = await _retryService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TaskCanceledException();
            }
            return "done";
        });

        // Assert
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_HttpRequestException_ShouldBeRetried()
    {
        // Arrange
        int attemptCount = 0;
        
        // Act
        var result = await _retryService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new HttpRequestException();
            }
            return "done";
        });

        // Assert
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_TransientInvalidOperationException_ShouldBeRetried()
    {
        // Arrange
        int attemptCount = 0;
        
        // Act
        var result = await _retryService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new InvalidOperationException("A transient error occurred");
            }
            return "done";
        });

        // Assert
        Assert.Equal(2, attemptCount);
    }

    #endregion
}
