using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using RTWServer.ServerCore.implementation;
using RTWServer.ServerCore.Interface;

namespace RTWTest.ServerCore;

// 동시 세션 상한(어드미션 컨트롤)이 초과 연결을 세션 생성 없이 닫는지 검증한다.
[TestFixture]
public class ClientSessionManagerTests
{
    // 취소될 때까지 수신을 막아 세션을 살아있게 유지하는 테스트용 클라이언트.
    private sealed class BlockingClient : IClient
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Stream Stream { get; } = new MemoryStream();
        public bool IsConnected => true;
        public int DisposeCount;

        public async ValueTask<int> ReceiveAsync(byte[] buffer, int offset, int length, CancellationToken cancellationToken = default)
        {
            using (cancellationToken.Register(() => _gate.TrySetResult()))
            {
                await _gate.Task;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return 0; // 취소가 아닌 경우 EOF로 간주
        }

        public void Close() => Dispose();
        public void Dispose() => Interlocked.Increment(ref DisposeCount);
    }

    private static ClientSessionManager CreateManager(int maxConcurrentSessions)
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var packetHandler = new Mock<IPacketHandler>().Object;

        // 헤더 크기를 0이 아닌 값으로 둬야 읽기 루프가 실제로 ReceiveAsync를 호출해
        // BlockingClient에서 멈춘다(0이면 0바이트 읽기로 간주돼 세션이 즉시 끝난다).
        var serializerMock = new Mock<IPacketSerializer>();
        serializerMock.Setup(s => s.GetHeaderSize()).Returns(8);

        var validator = new Mock<ISessionValidator>().Object;
        return new ClientSessionManager(loggerFactory, packetHandler, serializerMock.Object, validator, maxConcurrentSessions);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.Elapsed > timeout)
            {
                throw new TimeoutException("Condition was not met within the timeout");
            }

            await Task.Delay(10);
        }
    }

    [Test]
    public async Task HandleNewClientAsync_OverConcurrencyLimit_RejectsWithoutCreatingSession()
    {
        var manager = CreateManager(maxConcurrentSessions: 2);
        using var cts = new CancellationTokenSource();

        var client1 = new BlockingClient();
        var client2 = new BlockingClient();
        var client3 = new BlockingClient();

        // 1, 2는 상한 내라 세션을 열고 수신에서 블로킹된다(완료되지 않음).
        Task t1 = manager.HandleNewClientAsync(client1, cts.Token);
        Task t2 = manager.HandleNewClientAsync(client2, cts.Token);

        await WaitUntilAsync(() => manager.GetAllClientSessions().Count() == 2, TimeSpan.FromSeconds(2));

        // 3번째는 상한 초과 → 세션 미생성 + 클라 정리 후 즉시 완료.
        await manager.HandleNewClientAsync(client3, cts.Token);

        Assert.That(client3.DisposeCount, Is.GreaterThan(0));
        Assert.That(manager.GetAllClientSessions().Count(), Is.EqualTo(2));

        // 정리: 취소해 1, 2 세션을 종료시킨다.
        cts.Cancel();
        await Task.WhenAll(t1, t2);
    }

    [Test]
    public async Task HandleNewClientAsync_AfterSessionEnds_AdmitsNewConnection()
    {
        // 상한이 1일 때, 한 세션이 끝나면 슬롯이 회수되어 다음 연결을 받아들인다.
        var manager = CreateManager(maxConcurrentSessions: 1);
        using var cts = new CancellationTokenSource();

        var client1 = new BlockingClient();
        Task t1 = manager.HandleNewClientAsync(client1, cts.Token);
        await WaitUntilAsync(() => manager.GetAllClientSessions().Count() == 1, TimeSpan.FromSeconds(2));

        // 1번 세션 종료 → 슬롯 회수.
        cts.Cancel();
        await t1;
        await WaitUntilAsync(() => !manager.GetAllClientSessions().Any(), TimeSpan.FromSeconds(2));

        // 새 토큰으로 두 번째 연결을 열면 정상 수용된다.
        using var cts2 = new CancellationTokenSource();
        var client2 = new BlockingClient();
        Task t2 = manager.HandleNewClientAsync(client2, cts2.Token);
        await WaitUntilAsync(() => manager.GetAllClientSessions().Count() == 1, TimeSpan.FromSeconds(2));

        Assert.That(client2.DisposeCount, Is.EqualTo(0));

        cts2.Cancel();
        await t2;
    }
}
