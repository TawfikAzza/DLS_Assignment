using System.Diagnostics;
using System.Text.Json;
using Domain;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace API.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase {
        
        private readonly IHttpClientFactory _clientFactory;
        
        /*** START OF IMPORTANT CONFIGURATION ***/
        private readonly Tracer _tracer;

        public MainController(IHttpClientFactory httpClientFactory, Tracer tracer) {
            _clientFactory = httpClientFactory;
            _tracer = tracer;
        }
        
        /*** END OF IMPORTANT CONFIGURATION ***/

        [HttpPost("Sum")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))]
        public async Task<IActionResult> Sum([FromBody] Problem problem)
        {
            /*** HOW TO START TRACING ***/
            using var activity = _tracer.StartActiveSpan("Sum");

            var client = _clientFactory.CreateClient();
            var sumServiceUrl = "http://sum-service:80";
            
            var activityContext = activity?.Context ?? Activity.Current?.Context ?? default;
            var propagationContext = new PropagationContext(activityContext, Baggage.Current);
            var propagator = new TraceContextPropagator();
            propagator.Inject(propagationContext, problem, (msg, key, value) =>
            {
                msg.Headers.Add(key, value);
            });
            
            var jsonRequest = JsonSerializer.Serialize(problem);
            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync($"{sumServiceUrl}/Sum", content);
            
            if (response.IsSuccessStatusCode) {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<double>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(result);
            } 
            
            return StatusCode((int)response.StatusCode, response.ReasonPhrase);
        }
        
        [HttpPost("Subtract")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))]
        public async Task<IActionResult> Subtract([FromBody] Problem problem) {
            var client = _clientFactory.CreateClient();
            var sumServiceUrl = "http://subtract-service:80";
            
            var jsonRequest = JsonSerializer.Serialize(problem);
            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync($"{sumServiceUrl}/Subtract", content);
            
            if (response.IsSuccessStatusCode) {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<double>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(result);
            } 
            
            return StatusCode((int)response.StatusCode, response.ReasonPhrase);
        }
        
        [HttpGet("History")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Operation>))]
        public async Task<IActionResult> History() {
            var client = _clientFactory.CreateClient();
            var historyServiceUrl = "http://history-service:80";

            var response = await client.GetAsync($"{historyServiceUrl}/History");
            
            if (response.IsSuccessStatusCode) {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<Operation>>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Ok(result);
            } 
            
            return StatusCode((int)response.StatusCode, response.ReasonPhrase);
        }
    }
}
