namespace API.Models;
public class FailedRequest
{
    public required string Url { get; set; }
    public required HttpMethod Method { get; set; }
    public required Dictionary<string, string> Headers { get; set; }
    public required string Body { get; set; }
}