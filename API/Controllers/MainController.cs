using System.Text.Json;
using Domain;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class MainController : ControllerBase {
        
        private readonly IHttpClientFactory _clientFactory;

        public MainController(IHttpClientFactory httpClientFactory) {
            _clientFactory = httpClientFactory;
        }

        [HttpPost("Sum")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(double))]
        public async Task<IActionResult> Sum([FromBody] Problem problem) {
            var client = _clientFactory.CreateClient();
            var sumServiceUrl = "http://sum-service:80";
            
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
    }
}
