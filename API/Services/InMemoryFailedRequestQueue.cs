using System.Collections.Generic;
using API.Models; // Adjust based on where your FailedRequest model is located

namespace API.Services
{
public class InMemoryFailedRequestQueue : IFailedRequestQueue
{
    private readonly Queue<FailedRequest> _queue = new Queue<FailedRequest>();

    public void Enqueue(FailedRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        _queue.Enqueue(request);
    }

    public FailedRequest? Dequeue()
    {
        return _queue.Count > 0 ? _queue.Dequeue() : null;
    }

    public int Count => _queue.Count;
}
}
