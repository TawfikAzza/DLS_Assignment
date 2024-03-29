﻿using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Domain;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Polly.CircuitBreaker;

namespace SubtractService.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class SubtractController : ControllerBase {
        private readonly IHttpClientFactory _clientFactory;
        private readonly Tracer _tracer;
        public SubtractController(IHttpClientFactory httpClientFactory, Tracer tracer) {
            _clientFactory = httpClientFactory;
            _tracer = tracer;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))]
        public async Task<IActionResult> Subtract(Problem problem) {
            var propagatorExtract = new TraceContextPropagator();
            var parentContext = propagatorExtract.Extract(default, problem, (request, key) => {
                return new List<string>(new[] {
                    request.Headers.ContainsKey(key) ? request.Headers[key].ToString() : String.Empty
                });
            });
            Baggage.Current = parentContext.Baggage;
            using var consumerActivity = _tracer.StartActiveSpan("ConsumerActivity");
            using var activity = _tracer.StartActiveSpan("Subtract");
            var result = problem.OperandA - problem.OperandB;
            var operation = CreateOperationObject(problem, result);
            try {
                var client = _clientFactory.CreateClient("HistoryServiceClient");
                var historyService = "http://history-service:80";
                var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
                var propagationContext = new PropagationContext(activityContext, Baggage.Current);
                var propagator = new TraceContextPropagator();
                propagator.Inject(propagationContext, operation, (msg, key, value) => { msg.Headers.Add(key, value); });
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
                OperationType = "subtract"
            };

            return operation;
        }
    }
}
