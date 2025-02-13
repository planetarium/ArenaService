using System;
using System.Threading.Tasks;

namespace ArenaService.Utils;

public static class RetryUtility
{
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts,
        int delayMilliseconds,
        Func<T, bool> successCondition,
        Action<int>? onRetry = null
    )
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var result = await operation();

                if (successCondition(result))
                {
                    return result;
                }

                onRetry?.Invoke(attempt);
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    throw new Exception($"Operation failed after {maxAttempts} attempts.", ex);
                }

                onRetry?.Invoke(attempt);
            }
            await Task.Delay(delayMilliseconds);
        }

        throw new TimeoutException($"Operation did not succeed after {maxAttempts} attempts.");
    }
}
