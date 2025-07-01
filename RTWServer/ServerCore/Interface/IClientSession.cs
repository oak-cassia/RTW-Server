using NetworkDefinition.ErrorCode;

namespace RTWServer.ServerCore.Interface;

public interface IClientSession
{
    string Id { get; } // Session ID, will also serve as Player ID after authentication
    string? AuthToken { get; }
    bool IsAuthenticated { get; }

    Task StartSessionAsync(CancellationToken token);

    Task SendAsync(IPacket packet);

    // PlayerId in the return tuple now refers to the authenticated Session ID (or a mapping if needed)
    Task<(RTWErrorCode ErrorCode, int PlayerId)> ValidateAuthTokenAsync(string authToken);

    /// <summary>
    ///     Requests the session to start its shutdown procedure.
    /// </summary>
    /// <param name="reason">The reason for the shutdown request.</param>
    Task RequestShutdownAsync(string reason);
}