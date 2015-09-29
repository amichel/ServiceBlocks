public interface ITaskWorker
{
    void Start();
    void Stop(int timeout = -1);
}