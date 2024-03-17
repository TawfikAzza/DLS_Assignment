namespace API.Services;
public class FailedRequestQueueFactory : IFailedRequestQueueFactory
{
    private readonly Dictionary<string, IFailedRequestQueue> _queues = new Dictionary<string, IFailedRequestQueue>();

    public IFailedRequestQueue GetQueue(string serviceName)
    {
        if (!_queues.TryGetValue(serviceName, out IFailedRequestQueue? value))
        {
            value = new InMemoryFailedRequestQueue();
            _queues[serviceName] = value; // Assumes a default constructor is available
        }

        return value;
    }
}