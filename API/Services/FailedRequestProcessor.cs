using System.Net.Http;
using System.Text;
using API.Models;
using API.Services;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

public class FailedRequestProcessor : BackgroundService
{
    private readonly IFailedRequestQueue _failedRequestQueue;
    private readonly IHttpClientFactory _clientFactory;
    private const int MaxRetries = 3; // Maximum number of retries for a failed request

    public FailedRequestProcessor(IFailedRequestQueue failedRequestQueue, IHttpClientFactory clientFactory)
    {
        _failedRequestQueue = failedRequestQueue;
        _clientFactory = clientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await IsServiceHealthy("http://sum-service/health", stoppingToken))
            {
                while (_failedRequestQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                {
                    var failedRequest = _failedRequestQueue.Dequeue();
                    if (failedRequest != null)
                    {
                        int retryCount = 0;
                        bool requestProcessed = false;

                        while (retryCount < MaxRetries && !requestProcessed && !stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                var client = _clientFactory.CreateClient();
                                var httpRequestMessage = new HttpRequestMessage(failedRequest.Method, failedRequest.Url)
                                {
                                    Content = new StringContent(failedRequest.Body, Encoding.UTF8, "application/json")
                                };

                                foreach (var header in failedRequest.Headers)
                                {
                                    httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                                }

                                var response = await client.SendAsync(httpRequestMessage, stoppingToken);

                                if (response.IsSuccessStatusCode)
                                {
                                    requestProcessed = true;
                                }
                                else
                                {
                                    Monitoring.Monitoring.Log.Warning($"Failed to process request to {failedRequest.Url}. Status Code: {response.StatusCode}. Attempt {retryCount + 1} of {MaxRetries}.");
                                    retryCount++;
                                    await Task.Delay(TimeSpan.FromSeconds(2 ^ retryCount), stoppingToken); // Exponential back-off
                                }
                            }
                            catch (Exception ex)
                            {
                                Monitoring.Monitoring.Log.Error($"Error processing failed request to {failedRequest.Url}. Attempt {retryCount + 1} of {MaxRetries}. Exception: {ex.Message}");
                                retryCount++;
                                await Task.Delay(TimeSpan.FromSeconds(2 ^ retryCount), stoppingToken); // Exponential back-off
                            }
                        }

                        if (!requestProcessed)
                        {
                            Monitoring.Monitoring.Log.Error($"Failed to process failed request to {failedRequest.Url} after {MaxRetries} attempts.");
                        }
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task<bool> IsServiceHealthy(string healthCheckUrl, CancellationToken stoppingToken)
    {
        try
        {
            var client = _clientFactory.CreateClient();
            var response = await client.GetAsync(healthCheckUrl, stoppingToken);
            if (response.IsSuccessStatusCode)
            {
                // Optionally parse response
                return true;
            }

            Monitoring.Monitoring.Log.Warning($"Health check for {healthCheckUrl} failed with status code: {response.StatusCode}.");
            return false;
        }
        catch (Exception ex)
        {
            Monitoring.Monitoring.Log.Error($"Exception during health check for {healthCheckUrl}. Exception: {ex.Message}");
            return false;
        }
    }
}
