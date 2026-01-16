using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace FailsafeAutoBackup.Service.Resilience;

public static class ResiliencePolicies
{
    public static AsyncRetryPolicy CreateRetryPolicy(ILogger logger, int maxRetries = 3)
    {
        return Policy
            .Handle<Exception>(ex => !(ex is OperationCanceledException))
            .WaitAndRetryAsync(
                maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "Retry {RetryCount} of {MaxRetries} after {Delay}s due to: {Message}",
                        retryCount,
                        maxRetries,
                        timeSpan.TotalSeconds,
                        exception.Message);
                });
    }

    public static AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(ILogger logger)
    {
        return Policy
            .Handle<Exception>(ex => !(ex is OperationCanceledException))
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) =>
                {
                    logger.LogError(
                        exception,
                        "Circuit breaker opened for {Duration}s due to: {Message}",
                        duration.TotalSeconds,
                        exception.Message);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset - operations resumed");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker half-open - testing operations");
                });
    }

    public static IAsyncPolicy CreateCompositePolicy(ILogger logger)
    {
        var retry = CreateRetryPolicy(logger);
        var circuitBreaker = CreateCircuitBreakerPolicy(logger);
        
        return Policy.WrapAsync(retry, circuitBreaker);
    }
}
