using Domain;
using Microsoft.AspNetCore.Mvc;

namespace Hist.Controllers {
    public class SumController : Controller {
        
        [HttpPost]
        public async Task<ActionResult<List<Operation>>> GetHistory()
        {
            return null;
        }
    }
}