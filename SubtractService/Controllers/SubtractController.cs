using Domain;
using Microsoft.AspNetCore.Mvc;

namespace SubtractService.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class SubtractController : ControllerBase {

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))] 
        public IActionResult Sum(Problem problem)
        {
            var result = problem.OperandA - problem.OperandB;
            return Ok(result);
        }
    }
}
