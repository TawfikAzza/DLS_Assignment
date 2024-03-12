using System.Numerics;
using System.Text.Json;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace SubtractService.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class SubtractController : ControllerBase {
        
        private readonly IHttpClientFactory _clientFactory;
        private readonly Random _random = new Random(99999);

        public SubtractController(IHttpClientFactory httpClientFactory) {
            _clientFactory = httpClientFactory;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))] 
        public async Task<IActionResult> Subtract(Problem problem)
        {
            var result = problem.OperandA - problem.OperandB;

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
                Id = _random.Next(9999999),
                OperandA = problem.OperandA,
                OperandB = problem.OperandB,
                Result = result,
                OperationType = "subtract"
            };

            return operation;
        }
    }
}
