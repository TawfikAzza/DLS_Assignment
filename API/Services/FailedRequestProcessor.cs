
namespace API.Services;
public class FailedRequestProcessor : BackgroundService
{
    private readonly IFailedRequestQueue _failedRequestQueue;
    private readonly IHttpClientFactory _clientFactory;

    public FailedRequestProcessor(IFailedRequestQueue failedRequestQueue, IHttpClientFactory clientFactory)
    {
        _failedRequestQueue = failedRequestQueue;
        _clientFactory = clientFactory;
    }

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        // Assume IsServiceHealthy is a method that checks the health of your service.
        if (await IsServiceHealthy("http://sum-service/health", stoppingToken))
        {
            while (_failedRequestQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
            {
                var failedRequest = _failedRequestQueue.Dequeue();
                if (failedRequest != null)
                {
                    // Logic to resend the failed request
                    // Ensure you handle potential failures of this resend attempt as well
                }
            }
        }

        // Wait for a while before the next health check
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
    }
}

private async Task<bool> IsServiceHealthy(string healthCheckUrl, CancellationToken stoppingToken)
{
    try
    {
        var client = _clientFactory.CreateClient();
        var response = await client.GetAsync(healthCheckUrl, stoppingToken);
        return response.IsSuccessStatusCode;
    }
    catch
    {
        // Log the exception or handle it as appropriate
        return false;
    }
}
}
