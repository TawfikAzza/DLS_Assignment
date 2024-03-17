namespace API.Services;
public interface IFailedRequestQueueFactory
{
    IFailedRequestQueue GetQueue(string serviceName);
}