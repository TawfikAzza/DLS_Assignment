using System.Diagnostics;
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
            Monitoring.Monitoring.Log.Debug("Received problem: {0}", problem);
            
            var propagatorExtract = new TraceContextPropagator();
            var parentContext = propagatorExtract.Extract(default, problem, (request, key) => {
                return new List<string>(new[] {
                    request.Headers.ContainsKey(key) ? request.Headers[key].ToString() : String.Empty
                });
            });
            Baggage.Current = parentContext.Baggage;
            using var consumerActivity = _tracer.StartActiveSpan("ConsumerActivity");
            Monitoring.Monitoring.Log.Debug("Extracted and consumed parent activity.");
            
            
            using var activity = _tracer.StartActiveSpan("Sum");
            var result = problem.OperandA + problem.OperandB;
            Monitoring.Monitoring.Log.Debug("Calculated sum, the result is: {0}", result);
            
            try {
                var operation = CreateOperationObject(problem, result);
                Monitoring.Monitoring.Log.Debug("Operation object created.");

                var client = _clientFactory.CreateClient("HistoryServiceClient");
                Monitoring.Monitoring.Log.Debug("Client created.");
                var historyService = "http://history-service:80";
                var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
                Monitoring.Monitoring.Log.Debug("Activity context created: {0}", activityContext);
                var propagationContext = new PropagationContext(activityContext, Baggage.Current);
                Monitoring.Monitoring.Log.Debug("Propagation context created: {0}", propagationContext);
                var propagatorInject = new TraceContextPropagator();
                Monitoring.Monitoring.Log.Debug("Propagator created: {0}", propagatorInject);
                
                propagatorInject.Inject(propagationContext, operation, (msg, key, value) =>
                {
                    msg.Headers.Add(key, value); 
                    
                });
                Monitoring.Monitoring.Log.Debug("Propagated context to operation object.");

                
                var jsonRequest = JsonSerializer.Serialize(operation);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
                Monitoring.Monitoring.Log.Debug("Serialized and created content object.");

                Monitoring.Monitoring.Log.Debug("Sending operation object to HistoryService.");
                var response = await client.PostAsync($"{historyService}/History/AddOperation", content);
                Monitoring.Monitoring.Log.Debug("Received response from HistoryService.");

                if (!response.IsSuccessStatusCode) throw new HttpRequestException();
                Monitoring.Monitoring.Log.Debug("Operation object successfully sent to HistoryService.");

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
            using var activity = _tracer.StartActiveSpan("CreateOperationObject");
            Monitoring.Monitoring.Log.Debug("Creating operation object.");
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
