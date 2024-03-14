using Domain;
using HistoryService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace Hist.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class HistoryController : Controller {
        
        private readonly CalcContext _context;
        private readonly Tracer _tracer;
        public HistoryController(CalcContext context, Tracer tracer) {
            _context = context;
            _tracer = tracer;
            RebuildDB();
        }

        [HttpGet("rebuildDB")]
        public void RebuildDB()
        {
            _context.Database.EnsureCreated();
        }
        
        [HttpGet]
        public async Task<ActionResult<List<Operation>>> GetHistory()
        {
            try
            {
                var propagatorExtract = new TraceContextPropagator();
                var parentContext = propagatorExtract.Extract(default, Request, (request, key) => {
                    return new List<string>(new[] {
                        request.Headers.ContainsKey(key) ? request.Headers[key].ToString() : String.Empty
                    });
                });
                
                Baggage.Current = parentContext.Baggage;
                
                using var consumerActivity = _tracer.StartActiveSpan("ConsumerActivity");
                using var activity = _tracer.StartActiveSpan("GetHistory");
                
                return await _context.OperationTable.ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving the history.");
            }
        }

        
        [HttpPost("AddOperation")]
        public Task AddOperation(Operation operation)
        {
            try
            {
                var propagatorExtract = new TraceContextPropagator();
                var parentContext = propagatorExtract.Extract(default, operation, (request, key) => {
                    return new List<string>(new[] {
                        request.Headers.ContainsKey(key) ? request.Headers[key].ToString() : String.Empty
                    });
                });
                Baggage.Current = parentContext.Baggage;
                using var consumerActivity = _tracer.StartActiveSpan("ConsumerActivity");
                using var activity = _tracer.StartActiveSpan("AddOperation");
                _context.OperationTable.Add(operation);
                _context.SaveChanges();
                return Task.CompletedTask;
            } catch (Exception e)
            {
                Monitoring.Monitoring.Log.Error("Error adding operation to history. Exception: {0}", e);
                return Task.CompletedTask;
            }
        }
    }
}