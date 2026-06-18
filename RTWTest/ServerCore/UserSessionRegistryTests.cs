using Moq;
using RTWServer.ServerCore.implementation;
using RTWServer.ServerCore.Interface;

namespace RTWTest.ServerCore;

// userId 단일 세션 강제(last-wins)와 제거 보호의 동시성 로직을 격리해 검증한다.
[TestFixture]
public class UserSessionRegistryTests
{
    private static IClientSession Session(long userId, string id)
    {
        var mock = new Mock<IClientSession>();
        mock.SetupGet(s => s.UserId).Returns(userId);
        mock.SetupGet(s => s.Id).Returns(id);
        return mock.Object;
    }

    [Test]
    public void Register_FirstSession_NoDisplacementAndRetrievable()
    {
        var registry = new UserSessionRegistry();
        var a = Session(42, "a");

        var displaced = registry.Register(a);

        Assert.That(displaced, Is.Null);
        Assert.That(registry.Get(42), Is.SameAs(a));
    }

    [Test]
    public void Register_SameUserTwice_DisplacesPreviousAndKeepsLatest()
    {
        var registry = new UserSessionRegistry();
        var a = Session(42, "a");
        var b = Session(42, "b");

        registry.Register(a);
        var displaced = registry.Register(b);

        Assert.That(displaced, Is.SameAs(a));
        Assert.That(registry.Get(42), Is.SameAs(b));
    }

    [Test]
    public void Register_SameSessionTwice_IsIdempotent()
    {
        var registry = new UserSessionRegistry();
        var a = Session(42, "a");

        registry.Register(a);
        var displaced = registry.Register(a);

        Assert.That(displaced, Is.Null);
        Assert.That(registry.Get(42), Is.SameAs(a));
    }

    [Test]
    public void Unregister_AfterDisplacement_DoesNotRemoveReplacement()
    {
        // last-wins로 밀려난 옛 세션의 정리가 새 세션을 지우면 안 된다(제거 보호).
        var registry = new UserSessionRegistry();
        var a = Session(42, "a");
        var b = Session(42, "b");

        registry.Register(a);
        registry.Register(b); // a 밀려남
        registry.Unregister(a); // 밀려난 a의 뒤늦은 정리

        Assert.That(registry.Get(42), Is.SameAs(b));
    }

    [Test]
    public void Unregister_CurrentSession_RemovesIt()
    {
        var registry = new UserSessionRegistry();
        var a = Session(42, "a");

        registry.Register(a);
        registry.Unregister(a);

        Assert.That(registry.Get(42), Is.Null);
    }

    [Test]
    public void Get_DifferentUsers_AreIndependent()
    {
        var registry = new UserSessionRegistry();
        var a = Session(1, "a");
        var b = Session(2, "b");

        registry.Register(a);
        registry.Register(b);

        Assert.That(registry.Get(1), Is.SameAs(a));
        Assert.That(registry.Get(2), Is.SameAs(b));
    }
}
