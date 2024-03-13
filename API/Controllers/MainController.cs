using System.Diagnostics;
using System.Text.Json;
using Domain; // Domain models, potentially for request/response objects
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry; // For tracing and telemetry
using OpenTelemetry.Context.Propagation; // For context propagation in distributed tracing
using OpenTelemetry.Trace; // For tracing
using Polly.CircuitBreaker; // For handling circuit breaker pattern
using API.Models; // Where your FailedRequest model is defined
using API.Services; // Where your IFailedRequestQueue service is defined

namespace API.Controllers {
    // Attribute to mark this class as an API controller with route and API behavior
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase {
        // Dependency injection fields
        private readonly IFailedRequestQueue _failedRequestQueue; // Queue for failed requests
        private readonly IHttpClientFactory _clientFactory; // Factory for creating HTTP clients
        private readonly Tracer _tracer; // Tracer for OpenTelemetry

        // Constructor with dependency injection
        public MainController(IHttpClientFactory httpClientFactory, Tracer tracer, IFailedRequestQueue failedRequestQueue) {
            _clientFactory = httpClientFactory;
            _tracer = tracer;
            _failedRequestQueue = failedRequestQueue;
        }

        // API endpoint for sum operation
        [HttpPost("Sum")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))]
        public async Task<IActionResult> Sum([FromBody] Problem problem) {
            // Start a new tracing span
            using var activity = _tracer.StartActiveSpan("Sum");
            var jsonRequest = JsonSerializer.Serialize(problem); // Serialize request body to JSON
            var sumServiceUrl = "http://sum-service:80"; // URL of the sum service

            try {
                var client = _clientFactory.CreateClient("SumServiceClient");
                // Inject tracing context into the outbound HTTP request
                var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
                var propagationContext = new PropagationContext(activityContext, Baggage.Current);
                var propagator = new TraceContextPropagator();
                propagator.Inject(propagationContext, problem, (msg, key, value) => { msg.Headers.Add(key, value); });

                // Create HTTP content and make a POST request
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{sumServiceUrl}/Sum", content);

                // Handle unsuccessful response
                if (!response.IsSuccessStatusCode) throw new HttpRequestException();

                // Parse response and return result
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<double>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(result);
            }
            catch (BrokenCircuitException) {
                // Log and enqueue request if circuit breaker is open
                Monitoring.Monitoring.Log.Warning("SumService is down, circuit breaker opened.");
                _failedRequestQueue.Enqueue(new FailedRequest {
                    Url = $"{sumServiceUrl}/Sum",
                    Method = HttpMethod.Post,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } },
                    Body = jsonRequest
                });
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "SumService is down, circuit breaker opened.");
            }
            catch (HttpRequestException) {
                // Log and return service unavailable error
                Monitoring.Monitoring.Log.Error("SumService is unavailable");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Service unavailable");
            }
        }

        // Similar structure for "Subtract" and "History" endpoints...
        // Each endpoint attempts an operation, logs and queues failed requests, and handles exceptions accordingly.
    }
}
