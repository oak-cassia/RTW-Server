namespace RTWServer.ServerCore.Interface;

public interface IClient : IDisposable
{
    Stream Stream { get; }
    bool IsConnected { get; }

    ValueTask<int> ReceiveAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken = default);

    void Close();
}