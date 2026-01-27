using Payments.Api.Service.Interfaces;

namespace Payments.Api.Service.Implementations;

/// <summary>
/// Retry service implementation with exponential backoff.
/// Retries transient failures while being safe for idempotent operations.
/// </summary>
public class RetryService : IRetryService
{
    // Base delay for exponential backoff (100ms)
    private const int BaseDelayMs = 100;
    
    // Maximum delay cap (5 seconds)
    private const int MaxDelayMs = 5000;

    /// <summary>
    /// Determines if an exception is transient and safe to retry.
    /// </summary>
    private static bool IsTransientException(Exception ex)
    {
        // Transient exceptions that are safe to retry:
        // - TimeoutException: Network/operation timeout
        // - TaskCanceledException: Can occur on timeouts
        // - HttpRequestException: Network-related errors
        // - Certain database exceptions indicating transient issues
        return ex is TimeoutException
            || ex is TaskCanceledException
            || ex is HttpRequestException
            || (ex is InvalidOperationException && ex.Message.Contains("transient", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
    {
        ArgumentNullException.ThrowIfNull(operation);
        
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be non-negative.");
        }

        int attempt = 0;
        
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientException(ex))
            {
                attempt++;
                int delay = CalculateDelay(attempt);
                await Task.Delay(delay);
            }
        }
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = 3)
    {
        ArgumentNullException.ThrowIfNull(operation);
        
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries must be non-negative.");
        }

        int attempt = 0;
        
        while (true)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientException(ex))
            {
                attempt++;
                int delay = CalculateDelay(attempt);
                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Calculates delay with exponential backoff, capped at MaxDelayMs.
    /// </summary>
    private static int CalculateDelay(int attempt)
    {
        // Exponential backoff: 100ms, 200ms, 400ms, etc.
        int delay = BaseDelayMs * (int)Math.Pow(2, attempt - 1);
        return Math.Min(delay, MaxDelayMs);
    }
}
