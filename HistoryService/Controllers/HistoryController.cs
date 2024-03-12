using Domain;
using HistoryService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hist.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class HistoryController : Controller {
        
        private readonly CalcContext _context;
        
        public HistoryController(CalcContext context) {
            _context = context;
            RebuildDB();
        }

        [HttpGet("rebuildDB")]
        public void RebuildDB()
        {
            _context.Database.EnsureCreated();
        }
        [HttpGet("GetHistory")]
        public async Task<ActionResult<List<Operation>>> GetHistory()
        {
            try
            {
                return await _context.OperationTable.ToListAsync();
            }
            catch (Exception ex)
            {
                // Log the exception and return an appropriate error response
                return StatusCode(500, "An error occurred while retrieving the history.");
            }
        }
        
        [HttpPost("AddOperation")]
        public void AddOperation(Operation operation)
        {
            _context.OperationTable.Add(operation);
            _context.SaveChanges();
        }
    }
}