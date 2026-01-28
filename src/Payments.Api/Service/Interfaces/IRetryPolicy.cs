namespace Payments.Api.Service.Interfaces;

/// <summary>
/// Defines a retry policy for handling transient failures.
/// Used with payment gateway calls to ensure reliability without duplicating payments.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Executes an async operation with retry logic.
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="shouldRetry">Optional predicate to determine if a result should trigger a retry</param>
    /// <returns>The result of the operation</returns>
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, Func<T, bool>? shouldRetry = null);
}
