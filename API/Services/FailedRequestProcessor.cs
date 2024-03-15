using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using API.Models;
using API.Services;

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
            if (await IsServiceHealthy("http://sum-service/health", stoppingToken))
            {
                while (_failedRequestQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                {
                    var failedRequest = _failedRequestQueue.Dequeue();
                    if (failedRequest != null)
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

                            // Handle response as needed
                            if (!response.IsSuccessStatusCode)
                            {
                                // Optionally handle failed resend attempts here
                            }
                        }
                        catch
                        {
                            // Optionally handle exceptions from resend attempts here
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
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Optionally handle exceptions from the health check here
            return false;
        }
    }
}
