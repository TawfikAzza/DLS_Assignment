using Domain;
using Microsoft.AspNetCore.Mvc;

namespace SumService.Controllers {
    public class SumController : Controller {
        
        [HttpPost]
        public async IActionResult<Result> Sum(Operation operation) {
            
        }
    }
}
