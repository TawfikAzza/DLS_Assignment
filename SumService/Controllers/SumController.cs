using System.Text.Json;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace SumService.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class SumController : ControllerBase {

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))] 
        public IActionResult Sum(Problem problem)
        {
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
                Id = _random.Next(9999999),
                OperandA = problem.OperandA,
                OperandB = problem.OperandB,
                Result = result,
                OperationType = "sum"
            };

            return operation;
        }
    }
}
