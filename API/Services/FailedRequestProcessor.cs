using System.Net.Http;
using System.Text;
using API.Models;
using API.Services;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

public class FailedRequestProcessor : BackgroundService
{
    private readonly IFailedRequestQueueFactory _queueFactory;
    private readonly IFailedRequestQueue _failedRequestQueue;
    private readonly IHttpClientFactory _clientFactory;
    private const int MaxRetries = 3; // Maximum number of retries for a failed request

    // Constructor: Injects the necessary services and factories.
    public FailedRequestProcessor(IFailedRequestQueueFactory queueFactory, IFailedRequestQueue failedRequestQueue, IHttpClientFactory clientFactory)
    {
        _failedRequestQueue = failedRequestQueue;
        _clientFactory = clientFactory;
        _queueFactory = queueFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Loop continuously until the service is stopped or cancelled
        while (!stoppingToken.IsCancellationRequested)
        {
            // GetQueue returns the queue for each service
            var sumServiceQueue = _queueFactory.GetQueue("SumServiceQueue");
            var subtractServiceQueue = _queueFactory.GetQueue("SubtractServiceQueue");
            var historyServiceQueue = _queueFactory.GetQueue("HistoryServiceQueue");

            // Process each queue if the corresponding service is healthy
            await ProcessQueueIfServiceHealthy(sumServiceQueue, "http://sum-service/health", stoppingToken);
            await ProcessQueueIfServiceHealthy(subtractServiceQueue, "http://subtract-service/health", stoppingToken);
            await ProcessQueueIfServiceHealthy(historyServiceQueue, "http://history-service/health", stoppingToken);

            // Wait a bit before checking again
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    // Checks if a specific service is healthy by sending a GET request to its health check URL.
    private async Task<bool> IsServiceHealthy(string healthCheckUrl, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(healthCheckUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Monitoring.Monitoring.Log.Warning(e, $"Health check failed for {healthCheckUrl}");
            return false;
        }
    }

    // Processes the failed requests in the queue for a given service if the service is healthy.
    private async Task ProcessQueueIfServiceHealthy(IFailedRequestQueue queue, string healthCheckUrl, CancellationToken cancellationToken)
    {
        if (await IsServiceHealthy(healthCheckUrl, cancellationToken))
        {
            while (queue.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                var failedRequest = queue.Dequeue();
                if (failedRequest != null)
                {
                    await RetryFailedRequest(failedRequest, cancellationToken);
                }
            }
        }
    }

    // Attempts to resend a failed request, retrying up to MaxRetries times with exponential backoff.
    private async Task RetryFailedRequest(FailedRequest failedRequest, CancellationToken cancellationToken)
    {
        int retryCount = 0;
        bool success = false;
        while (retryCount < MaxRetries && !success && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = _clientFactory.CreateClient();
                var requestMessage = new HttpRequestMessage(failedRequest.Method, failedRequest.Url)
                {
                    Content = new StringContent(failedRequest.Body, Encoding.UTF8, "application/json")
                };

                foreach (var header in failedRequest.Headers)
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                var response = await client.SendAsync(requestMessage, cancellationToken);
                success = response.IsSuccessStatusCode;
                if (!success)
                {
                    Monitoring.Monitoring.Log.Warning($"Retry {retryCount + 1} for {failedRequest.Url} failed with status {response.StatusCode}.");
                    retryCount++;
                    await Task.Delay(GetBackoffDelay(retryCount), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Monitoring.Monitoring.Log.Warning(ex, $"Exception while retrying request to {failedRequest.Url}");
                retryCount++;
                await Task.Delay(GetBackoffDelay(retryCount), cancellationToken);
            }
        }

        if (!success)
        {
            Monitoring.Monitoring.Log.Warning($"Failed to process {failedRequest.Url} after {MaxRetries} attempts.");
        }
    }

    // Calculates the delay for the next retry attempt using exponential backoff.
    private static TimeSpan GetBackoffDelay(int retryCount)
    {
        // Exponential back-off logic
        return TimeSpan.FromSeconds(Math.Pow(2, retryCount));
    }
}
/*
    private async Task<bool> IsServiceHealthy(string healthCheckUrl, CancellationToken stoppingToken)
    {
        try
        {
            // Create a new HttpClient and send a GET request to the health check URL
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(healthCheckUrl, stoppingToken);
            if (response.IsSuccessStatusCode)
            {
                // Return true if the service responds with a success status code, indicating it's healthy
                return true;
            }

            // Log a warning if the health check fails
            Monitoring.Monitoring.Log.Warning($"Health check for {healthCheckUrl} failed with status code: {response.StatusCode}.");
            return false;
        }
        catch (Exception ex)
        {
            // Log any exceptions encountered during the health check
            Monitoring.Monitoring.Log.Error($"Exception during health check for {healthCheckUrl}. Exception: {ex.Message}");
            return false;
        }
    }

    // Existing methods like IsServiceHealthy remain unchanged
}

        // Continuously running background task
        while (!stoppingToken.IsCancellationRequested)
        {   
            var healthCheckUrls = new Dictionary<string, string>
            {
                { "SumServiceQueue", "http://sum-service/health" },
                { "SubtractServiceQueue", "http://subtract-service/health" },
                { "HistoryServiceQueue", "http://history-service/health" }
            };
            // Use this mapping to fetch the correct URL for health checks
            
            // Check if the SumService is healthy before attempting to process failed requests
            if (await IsServiceHealthy("http://sum-service/health", stoppingToken))
            {
                // Process each failed request in the queue
                while (_failedRequestQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                {
                    var failedRequest = _failedRequestQueue.Dequeue();
                    if (failedRequest != null)
                    {
                        int retryCount = 0;
                        bool requestProcessed = false;

                        // Retry the failed request up to a maximum number of retries
                        while (retryCount < MaxRetries && !requestProcessed && !stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                var client = _clientFactory.CreateClient();
                                var httpRequestMessage = new HttpRequestMessage(failedRequest.Method, failedRequest.Url)
                                {
                                    Content = new StringContent(failedRequest.Body, Encoding.UTF8, "application/json")
                                };

                                // Add headers to the request
                                foreach (var header in failedRequest.Headers)
                                {
                                    httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                                }

                                var response = await client.SendAsync(httpRequestMessage, stoppingToken);

                                if (response.IsSuccessStatusCode)
                                {
                                    requestProcessed = true; // Mark as processed if the response is successful
                                }
                                else
                                {
                                    // Log a warning if the request failed, then retry
                                    Monitoring.Monitoring.Log.Warning($"Failed to process request to {failedRequest.Url}. Status Code: {response.StatusCode}. Attempt {retryCount + 1} of {MaxRetries}.");
                                    retryCount++;
                                    // Implement exponential back-off in retry delay
                                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), stoppingToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log any exceptions encountered during processing
                                Monitoring.Monitoring.Log.Error($"Error processing failed request to {failedRequest.Url}. Attempt {retryCount + 1} of {MaxRetries}. Exception: {ex.Message}");
                                retryCount++;
                                // Implement exponential back-off in retry delay
                                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), stoppingToken);
                            }
                        }

                        if (!requestProcessed)
                        {
                            // Log an error if the request couldn't be processed after all retries
                            Monitoring.Monitoring.Log.Error($"Failed to process failed request to {failedRequest.Url} after {MaxRetries} attempts.");
                        }
                    }
                }
            }
            // Wait for a minute before the next health check and attempt to process the queue
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }*/