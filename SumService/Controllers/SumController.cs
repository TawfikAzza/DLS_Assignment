using System.Text.Json;
using Domain;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Polly.CircuitBreaker;

namespace SumService.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class SumController : ControllerBase {
        private readonly IHttpClientFactory _clientFactory;
        private readonly Random _random = new Random(99999);
        private readonly Tracer _tracer;

        public SumController(IHttpClientFactory httpClientFactory, Tracer tracer) {
            _clientFactory = httpClientFactory;
            _tracer = tracer;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))]
        public async Task<IActionResult> Sum(Problem problem) {
            var propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, problem, (request, key) => {
                return new List<string>(new[] {
                    request.Headers.ContainsKey(key) ? request.Headers[key].ToString() : String.Empty
                });
            });
            Baggage.Current = parentContext.Baggage;
            using var consumerActivity = _tracer.StartActiveSpan("ConsumerActivity");
            using var activity = _tracer.StartActiveSpan("Sum");
            var result = problem.OperandA + problem.OperandB;

            try {
                var operation = CreateOperationObject(problem, result);

                var client = _clientFactory.CreateClient("HistoryServiceClient");
                var historyService = "http://history-service:80";

                var jsonRequest = JsonSerializer.Serialize(operation);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{historyService}/History/AddOperation", content);

                if (!response.IsSuccessStatusCode) throw new HttpRequestException();

                return Ok(result);
            }
            catch (BrokenCircuitException) {
                Monitoring.Monitoring.Log.Warning("HistoryService is down, circuit breaker opened.");
                return Ok(result);
            }
            catch (HttpRequestException) {
                Monitoring.Monitoring.Log.Error("HistoryService is unavailable");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Service unavailable");
            }
        }

        private Operation CreateOperationObject(Problem problem, double result) {
            var operation = new Operation() {
                Id = 0,
                OperandA = problem.OperandA,
                OperandB = problem.OperandB,
                Result = result,
                OperationType = "sum"

            };

            return operation;
        }
    }
}
