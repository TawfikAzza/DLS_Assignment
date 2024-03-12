using System.Text.Json;

using System.Diagnostics;
using System.Text.Json;

using Domain;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace SumService.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class SumController : ControllerBase {
        private readonly IHttpClientFactory _clientFactory;
        private readonly Random _random = new Random(99999);

        public SumController(IHttpClientFactory httpClientFactory) {
            _clientFactory = httpClientFactory;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))] 
        public async Task<IActionResult> Sum(Problem problem)
        {
            var propagator = new TraceContextPropagator();
            var parentContext = propagator.Extract(default, problem, (msg, key) =>
            {
                return new List<string>(new[]{msg.Headers.ContainsKey(key)?msg.Headers[key].ToString():String.Empty});
            });
            Baggage.Current = parentContext.Baggage;
            using var consumerActivity = Monitoring.Monitoring.ActivitySource.StartActivity("ConsumerActivity", ActivityKind.Consumer, parentContext.ActivityContext);
            using var activity = Monitoring.Monitoring.ActivitySource.StartActivity();
           
            var result = problem.OperandA + problem.OperandB;
            var operation = CreateOperationObject(problem, result);
            
            var client = _clientFactory.CreateClient();
            var historyService = "http://history-service:80";
            
            var jsonRequest = JsonSerializer.Serialize(operation);
            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync($"{historyService}/History/AddOperation", content);
            
            if (response.IsSuccessStatusCode) {
                return Ok(result);
            }
            return StatusCode((int)response.StatusCode, response.ReasonPhrase);
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
