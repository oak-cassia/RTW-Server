namespace RTWServer.ServerCore.Interface;

public interface IClient
{
    public Stream Stream { get; }
    public bool IsConnected { get; }

    Task SendAsync(byte[] buffer);

    Task<int> ReceiveAsync(byte[] buffer, int offset, int length);

    void Close();
}