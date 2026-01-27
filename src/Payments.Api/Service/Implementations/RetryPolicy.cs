using Payments.Api.Service.Interfaces;

namespace Payments.Api.Service.Implementations;

/// <summary>
/// Options for configuring retry behavior.
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts (not counting the initial attempt).
    /// Default: 3 retries (4 total attempts).
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay before the first retry in milliseconds.
    /// Default: 100ms.
    /// </summary>
    public int InitialDelayMs { get; set; } = 100;

    /// <summary>
    /// Multiplier for exponential backoff.
    /// Delay increases by this factor after each retry.
    /// Default: 2.0 (delay doubles each retry).
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Maximum delay between retries in milliseconds.
    /// Default: 5000ms (5 seconds).
    /// </summary>
    public int MaxDelayMs { get; set; } = 5000;

    /// <summary>
    /// Adds jitter to prevent thundering herd.
    /// Default: true.
    /// </summary>
    public bool UseJitter { get; set; } = true;
}

/// <summary>
/// Implements exponential backoff retry policy for handling transient failures.
/// Safe for use with idempotent operations - retrying will not cause duplicate side effects.
/// </summary>
public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly RetryOptions _options;
    private readonly Random _random;

    public ExponentialBackoffRetryPolicy() : this(new RetryOptions())
    {
    }

    public ExponentialBackoffRetryPolicy(RetryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _random = new Random();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, Func<T, bool>? shouldRetry = null)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var attempts = 0;
        var delay = _options.InitialDelayMs;

        while (true)
        {
            attempts++;

            try
            {
                var result = await operation();

                // Check if result indicates we should retry
                if (shouldRetry != null && shouldRetry(result) && attempts <= _options.MaxRetries)
                {
                    await ApplyDelay(delay);
                    delay = CalculateNextDelay(delay);
                    continue;
                }

                return result;
            }
            catch (Exception ex) when (IsTransientException(ex) && attempts <= _options.MaxRetries)
            {
                await ApplyDelay(delay);
                delay = CalculateNextDelay(delay);
            }
        }
    }

    private async Task ApplyDelay(int delayMs)
    {
        var actualDelay = delayMs;

        if (_options.UseJitter)
        {
            // Add jitter: +/- 25% of delay to prevent thundering herd
            var jitter = (int)(delayMs * 0.25);
            actualDelay = delayMs + _random.Next(-jitter, jitter + 1);
            actualDelay = Math.Max(1, actualDelay); // Ensure positive delay
        }

        await Task.Delay(actualDelay);
    }

    private int CalculateNextDelay(int currentDelay)
    {
        var nextDelay = (int)(currentDelay * _options.BackoffMultiplier);
        return Math.Min(nextDelay, _options.MaxDelayMs);
    }

    /// <summary>
    /// Determines if an exception is transient and should be retried.
    /// In payment context, we only retry network/timeout issues, NOT business logic failures.
    /// </summary>
    private static bool IsTransientException(Exception ex)
    {
        // Transient exceptions that are safe to retry:
        // - HttpRequestException (network issues)
        // - TaskCanceledException (timeouts)
        // - OperationCanceledException (timeouts)
        // - Specific gateway timeout exceptions
        
        return ex is HttpRequestException
            || ex is TaskCanceledException
            || ex is OperationCanceledException
            || (ex is TimeoutException);
    }
}
