using RTWServer.Enum;

namespace RTWServer.ServerCore;

public interface IClient
{
    public string Id { get; }
    public bool IsConnected { get; }
    
    Task SendAsync(PacketId packetId, byte[] payload);
    
    Task<int> ReadAsync(byte[] buffer, int offset, int length);
    
    void Close();
}