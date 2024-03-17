using System.Diagnostics;
using System.Text.Json;
using Domain;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Polly.CircuitBreaker;
using API.Models;
using API.Services;

namespace API.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase {

        private readonly IHttpClientFactory _clientFactory;
        private readonly IFailedRequestQueue _failedRequestQueue;

        /*** START OF IMPORTANT CONFIGURATION ***/
        private readonly Tracer _tracer;

        public MainController(IHttpClientFactory httpClientFactory, Tracer tracer, IFailedRequestQueue failedRequestQueue) {
            _clientFactory = httpClientFactory;
            _tracer = tracer;
            _failedRequestQueue = failedRequestQueue; // Initialize the queue.
        }

        /*** END OF IMPORTANT CONFIGURATION ***/


        [HttpPost("Sum")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))]
        public async Task<IActionResult> Sum([FromBody] Problem problem) {
            using var activity = _tracer.StartActiveSpan("Sum");
            try {
                var client = _clientFactory.CreateClient("SumServiceClient");
                var sumServiceUrl = "http://sum-service:80";

                var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
                var propagationContext = new PropagationContext(activityContext, Baggage.Current);
                var propagator = new TraceContextPropagator();
                propagator.Inject(propagationContext, problem, (msg, key, value) => { msg.Headers.Add(key, value); });

                var jsonRequest = JsonSerializer.Serialize(problem);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{sumServiceUrl}/Sum", content);
                if (!response.IsSuccessStatusCode) throw new HttpRequestException();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<double>(jsonResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(result);
            }
            catch (BrokenCircuitException) {
                var sumServiceUrl = "http://sum-service:80";
                // Log the issue.
                Monitoring.Monitoring.Log.Warning("SumService is down, circuit breaker opened.");

                // Enqueue the failed request for later retry.
                _failedRequestQueue.Enqueue(new FailedRequest {
                    Url = sumServiceUrl,
                    Method = HttpMethod.Post,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                    Body = JsonSerializer.Serialize(problem)
                });

                return Ok(problem.OperandA + problem.OperandB);
            }
            catch (HttpRequestException) {
                Monitoring.Monitoring.Log.Error("SumService is unavailable");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Service unavailable");
            }
        }


        [HttpPost("Subtract")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))]
        public async Task<IActionResult> Subtract([FromBody] Problem problem) {
            try {
                var client = _clientFactory.CreateClient("SubtractServiceClient");
                var subtractServiceUrl = "http://subtract-service:80";

                var jsonRequest = JsonSerializer.Serialize(problem);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{subtractServiceUrl}/Subtract", content);

                if (!response.IsSuccessStatusCode) throw new HttpRequestException();
                
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<double>(jsonResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(result);
            }
            catch (BrokenCircuitException) {
                var subtractServiceUrl = "http://subtract-service:80";
                Monitoring.Monitoring.Log.Warning("SubtractService is down, circuit breaker opened.");
                
                // Enqueue the failed request for later retry.
                _failedRequestQueue.Enqueue(new FailedRequest {
                    Url = subtractServiceUrl,
                    Method = HttpMethod.Post,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                    Body = JsonSerializer.Serialize(problem)
                });

                return Ok(problem.OperandA - problem.OperandB);
            }
            catch (HttpRequestException) {
                Monitoring.Monitoring.Log.Error("SubtractService is unavailable");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Service unavailable");
            }
        }

        [HttpGet("History")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Operation>))]
        public async Task<IActionResult> History() {
            var client = _clientFactory.CreateClient();
            var historyServiceUrl = "http://history-service:80";

            var response = await client.GetAsync($"{historyServiceUrl}/History");

            if (response.IsSuccessStatusCode) {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<Operation>>(jsonResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(result);
            }
            Monitoring.Monitoring.Log.Error("History service unavailable. Reason: " + response.ReasonPhrase);
            return StatusCode((int)response.StatusCode, response.ReasonPhrase);

        }
    }
}
