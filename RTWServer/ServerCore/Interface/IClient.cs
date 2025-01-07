using RTWServer.Enum;

namespace RTWServer.ServerCore.Interface;

public interface IClient
{
    public string Id { get; }
    public bool IsConnected { get; }
    
    Task SendAsync(byte[] buffer);
    
    Task<int> ReadAsync(byte[] buffer, int offset, int length);
    
    void Close();
}