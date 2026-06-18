using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Moq;
using RTWWebServer.MasterDatas;
using RTWWebServer.MasterDatas.Models;
using RTWWebServer.Providers.MasterData;

namespace RTWTest.WebServer.MasterData;

[TestFixture]
public class MasterDataProviderTests
{
    private Mock<ILogger<MasterDataProvider>> _mockLogger = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<MasterDataProvider>>();
    }

    [Test]
    public void TryGetCharacter_ReturnsCorrectResults()
    {
        var provider = CreateProvider(
            new CharacterMaster { Id = 1, Name = "Character1", Portfolio = 50, Development = 60, JobSearching = 70 },
            new CharacterMaster { Id = 2, Name = "Character2", Portfolio = 80, Development = 90, JobSearching = 75 });

        Assert.That(provider.TryGetCharacter(1, out var char1), Is.True);
        Assert.That(char1?.Name, Is.EqualTo("Character1"));

        Assert.That(provider.TryGetCharacter(2, out var char2), Is.True);
        Assert.That(char2?.Name, Is.EqualTo("Character2"));

        Assert.That(provider.TryGetCharacter(999, out _), Is.False);
    }

    [Test]
    public void GetAllCharacters_ReturnsAllCharacters()
    {
        var provider = CreateProvider(
            new CharacterMaster { Id = 1, Name = "Character1", Portfolio = 50, Development = 60, JobSearching = 70 },
            new CharacterMaster { Id = 2, Name = "Character2", Portfolio = 80, Development = 90, JobSearching = 75 });

        var characters = provider.GetAllCharacters();

        Assert.That(characters, Has.Count.EqualTo(2));
        Assert.That(characters, Is.AssignableTo<ImmutableDictionary<int, CharacterMaster>>());
        Assert.That(characters.ContainsKey(1), Is.True);
        Assert.That(characters.ContainsKey(2), Is.True);
        Assert.That(characters[1].Name, Is.EqualTo("Character1"));
        Assert.That(characters[2].Name, Is.EqualTo("Character2"));
    }

    [Test]
    public void Provider_WithEmptyCharacters_HandlesGracefully()
    {
        var provider = CreateProvider(/* no characters */);

        var characters = provider.GetAllCharacters();
        Assert.That(characters, Is.Empty);
        Assert.That(provider.TryGetCharacter(1, out _), Is.False);
    }

    private MasterDataProvider CreateProvider(params CharacterMaster[] characters)
    {
        var set = new MasterDataSet(
            characters.ToImmutableDictionary(c => c.Id),
            ImmutableDictionary<int, FurnitureMaster>.Empty,
            ImmutableDictionary<int, RoomGradeMaster>.Empty);

        var mockLoader = new Mock<IMasterDataLoader>();
        mockLoader.Setup(l => l.Load()).Returns(set);

        return new MasterDataProvider(mockLoader.Object, _mockLogger.Object);
    }
}
