namespace RTWServer.ServerCore.Interface;

public interface IClientSession
{
    string Id { get; }
    
    Task StartSessionAsync(CancellationToken token);
    
    Task SendAsync(IPacket packet);
    
    void Disconnect();
}