using System.Numerics;
using System.Text.Json;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;

namespace SubtractService.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class SubtractController : ControllerBase {
        private readonly IHttpClientFactory _clientFactory;

        public SubtractController(IHttpClientFactory httpClientFactory) {
            _clientFactory = httpClientFactory;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))]
        public async Task<IActionResult> Subtract(Problem problem) {
            var result = problem.OperandA - problem.OperandB;
            var operation = CreateOperationObject(problem, result);
            try {
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
                OperationType = "subtract"
            };

            return operation;
        }
    }
}
