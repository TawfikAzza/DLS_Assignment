using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Domain; // Assuming this is where Problem and Operation classes are defined
using API.Models; // Assuming this is where FailedRequest is defined
using API.Services; // Assuming this contains interfaces like IHttpClientFactory, IFailedRequestQueue
using OpenTelemetry.Trace;
using OpenTelemetry.Context.Propagation;

namespace API.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IFailedRequestQueue _failedRequestQueue;
        private readonly Tracer _tracer;

        public MainController(IHttpClientFactory httpClientFactory, Tracer tracer, IFailedRequestQueue failedRequestQueue) {
            _clientFactory = httpClientFactory;
            _tracer = tracer;
            _failedRequestQueue = failedRequestQueue;
        }

        [HttpPost("Sum")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Sum([FromBody] Problem problem) {
            using var activity = _tracer.StartActiveSpan("Sum");
            try {
                var client = _clientFactory.CreateClient("SumServiceClient");
                var sumServiceUrl = "http://sum-service:80";
                var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
                var propagationContext = new PropagationContext(activityContext, Baggage.Current);
                var propagator = new TraceContextPropagator();

                HttpRequestMessage requestMessage = new HttpRequestMessage();
                propagator.Inject(propagationContext, requestMessage, (msg, key, value) => msg.Headers.Add(key, value));

                var jsonRequest = JsonSerializer.Serialize(problem);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{sumServiceUrl}/Sum", content);
                if (!response.IsSuccessStatusCode) throw new HttpRequestException();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<double>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(result);
            } catch (BrokenCircuitException e) {
                var sumServiceUrl = "http://sum-service:80";
                // Log the issue and enqueue the failed request for later retry.
                // Assuming Monitoring.Monitoring.Log exists and works as shown.
                Monitoring.Monitoring.Log.Warning($"SumService is down, circuit breaker opened. Exception: {e}");
                _failedRequestQueue.Enqueue(new FailedRequest {
                    Url = sumServiceUrl,
                    Method = HttpMethod.Post,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                    Body = JsonSerializer.Serialize(problem)
                });
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "SumService is down, circuit breaker opened.");
            } catch (HttpRequestException) {
                Monitoring.Monitoring.Log.Error("SumService is unavailable");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Service unavailable");
            }
        }

        // The Subtract method is essentially similar to the Sum method in structure, just with different endpoints.
        // Replace the Sum method logic with the appropriate Subtract logic where necessary.

        [HttpGet("History")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> History() {
            using var activity = _tracer.StartActiveSpan("History");
            try {
                var client = _clientFactory.CreateClient("HistoryServiceClient");
                var historyServiceUrl = "http://history-service:80";
                var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
                var propagationContext = new PropagationContext(activityContext, Baggage.Current);
                var request = new HttpRequestMessage(HttpMethod.Get, $"{historyServiceUrl}/History");
                var propagator = new TraceContextPropagator();
                propagator.Inject(propagationContext, request, (msg, key, value) => { msg.Headers.Add(key, value); });

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode) {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<List<Operation>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return Ok(result);
                }
                Monitoring.Monitoring.Log.Error("History service unavailable. Reason: " + response.ReasonPhrase);
                return StatusCode((int)response.StatusCode, response.ReasonPhrase);
            } catch (HttpRequestException) {
                Monitoring.Monitoring.Log.Error("Error while accessing History service.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Service unavailable");
            }
        }
    }
}
