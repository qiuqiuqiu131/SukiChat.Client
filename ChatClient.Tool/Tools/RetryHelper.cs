namespace ChatClient.Tool.Tools;

/// <summary>
/// 提供异步操作重试功能的工具类
/// </summary>
public static class RetryHelper
{
    /// <summary>
    /// 使用重试策略执行异步操作
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="maxRetries">最大重试次数，默认为3</param>
    /// <param name="initialDelay">初始重试延迟（毫秒），默认为1000ms</param>
    /// <param name="logger">可选的日志记录器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步操作的结果</returns>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3,
        int initialDelay = 1000,
        CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        var delay = TimeSpan.FromMilliseconds(initialDelay);

        while (true)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，不再重试
                throw;
            }
            catch (Exception ex)
            {
                retryCount++;

                if (retryCount > maxRetries)
                {
                    //logger?.LogError(ex, $"操作失败，已达到最大重试次数({maxRetries})");
                    throw;
                }

                //logger?.LogWarning(ex, $"操作失败，正在尝试重试 ({retryCount}/{maxRetries})");

                await Task.Delay(delay, cancellationToken);
                // 实现指数退避策略，每次重试后延迟时间翻倍
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
    }

    /// <summary>
    /// 使用重试策略执行无返回值的异步操作
    /// </summary>
    /// <param name="operation">要执行的异步操作</param>
    /// <param name="maxRetries">最大重试次数，默认为3</param>
    /// <param name="initialDelay">初始重试延迟（毫秒），默认为1000ms</param>
    /// <param name="logger">可选的日志记录器</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        int maxRetries = 3,
        int initialDelay = 1000,
        CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        var delay = TimeSpan.FromMilliseconds(initialDelay);

        while (true)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await operation();
                return;
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，不再重试
                throw;
            }
            catch (Exception ex)
            {
                retryCount++;

                if (retryCount > maxRetries)
                {
                    //logger?.LogError(ex, $"操作失败，已达到最大重试次数({maxRetries})");
                    throw;
                }

                //logger?.LogWarning(ex, $"操作失败，正在尝试重试 ({retryCount}/{maxRetries})");

                await Task.Delay(delay, cancellationToken);
                // 实现指数退避策略，每次重试后延迟时间翻倍
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
    }
}