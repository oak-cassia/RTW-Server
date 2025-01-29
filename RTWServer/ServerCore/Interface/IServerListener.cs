namespace RTWServer.ServerCore.Interface;

public interface IServerListener
{
    void Start(int backlog);
    void Stop();
    Task<IClient> AcceptClientAsync(CancellationToken token);
}