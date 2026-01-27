namespace Payments.Api.Service.Interfaces;

/// <summary>
/// Service for executing operations with retry logic for transient failures.
/// </summary>
public interface IRetryService
{
    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <returns>The result of the operation.</returns>
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3);

    /// <summary>
    /// Executes an operation with retry logic (no return value).
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    Task ExecuteWithRetryAsync(Func<Task> operation, int maxRetries = 3);
}
