using Domain;
using Microsoft.AspNetCore.Mvc;

namespace SumService.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class SumController : ControllerBase {

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Result))]
        public IActionResult Sum(Problem problem) {

            var result = new Result() {
                Value = problem.OperandA + problem.OperandB
            };

            return Ok(result);
        }
    }
}
