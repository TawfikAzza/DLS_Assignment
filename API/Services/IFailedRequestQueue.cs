using API.Models;
namespace API.Services
{
    public interface IFailedRequestQueue
    {
        void Enqueue(FailedRequest request);
        FailedRequest? Dequeue();
        int Count { get; }
    }
}