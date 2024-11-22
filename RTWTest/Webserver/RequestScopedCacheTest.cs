using RTWWebServer.Database.Cache;

namespace RTWTest.Webserver;

public class RequestScopedCacheTest
{
    private readonly IRequestScopedCache _cache = new RequestScopedCache();

    [Test]
    public void Should_Get_Same_Value_When_Set()
    {
        var key = "key";
        var value = "value";

        _cache.Set(key, value);
        var result = _cache.Get<string>(key);

        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void Should_Get_Default_When_Key_Not_Exists()
    {
        var key = "key";

        var result = _cache.Get<string>(key);

        Assert.That(result, Is.EqualTo(default(string)));
    }

    [Test]
    public void Should_Remove_Key()
    {
        var key = "key";
        var value = "value";

        _cache.Set(key, value);
        _cache.Remove(key);
        var result = _cache.Get<string>(key);

        Assert.That(result, Is.EqualTo(default(string)));
    }

    [Test]
    public void Should_Handle_Multiple_key()
    {
        var key1 = "key1";
        var key2 = "key2";

        var value1 = "value1";
        var value2 = "value2";

        _cache.Set(key1, value1);
        _cache.Set(key2, value2);

        var result1 = _cache.Get<string>(key1);
        var result2 = _cache.Get<string>(key2);

        Assert.That(result1, Is.EqualTo(value1));
        Assert.That(result2, Is.EqualTo(value2));
    }

    [Test]
    public void Should_Overwrite_Value()
    {
        var key = "key";
        var value1 = "value1";
        var value2 = "value2";

        _cache.Set(key, value1);
        _cache.Set(key, value2);

        var result = _cache.Get<string>(key);

        Assert.That(result, Is.EqualTo(value2));
    }
}