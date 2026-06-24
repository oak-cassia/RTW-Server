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

    [Test]
    public void GetRankByFame_ReturnsHighestReachedRank()
    {
        var provider = CreateProviderWithRanks(
            new RankMaster { Rank = 1, RequiredFame = 0 },
            new RankMaster { Rank = 2, RequiredFame = 300 },
            new RankMaster { Rank = 3, RequiredFame = 1000 });

        // baseline / 임계값 직전 / 정확히 임계값 / 사이 / 최상위 초과
        Assert.That(provider.GetRankByFame(0), Is.EqualTo(1));
        Assert.That(provider.GetRankByFame(299), Is.EqualTo(1));
        Assert.That(provider.GetRankByFame(300), Is.EqualTo(2));
        Assert.That(provider.GetRankByFame(999), Is.EqualTo(2));
        Assert.That(provider.GetRankByFame(1000), Is.EqualTo(3));
        Assert.That(provider.GetRankByFame(50000), Is.EqualTo(3));
    }

    [Test]
    public void GetRankByFame_WithNoBaseline_ReturnsZeroBelowLowestThreshold()
    {
        // baseline(RequiredFame 0)이 없으면 가장 낮은 임계값 미만에선 도달한 랭크가 없다.
        var provider = CreateProviderWithRanks(new RankMaster { Rank = 1, RequiredFame = 100 });

        Assert.That(provider.GetRankByFame(0), Is.EqualTo(0));
        Assert.That(provider.GetRankByFame(99), Is.EqualTo(0));
        Assert.That(provider.GetRankByFame(100), Is.EqualTo(1));
    }

    private MasterDataProvider CreateProvider(params CharacterMaster[] characters)
        => CreateProvider(characters, []);

    private MasterDataProvider CreateProviderWithRanks(params RankMaster[] ranks)
        => CreateProvider([], ranks);

    private MasterDataProvider CreateProvider(CharacterMaster[] characters, RankMaster[] ranks)
    {
        var set = new MasterDataSet(
            characters.ToImmutableDictionary(c => c.Id),
            ImmutableDictionary<int, FurnitureMaster>.Empty,
            ImmutableDictionary<int, RoomGradeMaster>.Empty,
            ImmutableDictionary<int, MissionMaster>.Empty,
            ranks.ToImmutableDictionary(r => r.Rank));

        var mockLoader = new Mock<IMasterDataLoader>();
        mockLoader.Setup(l => l.Load()).Returns(set);

        return new MasterDataProvider(mockLoader.Object, _mockLogger.Object);
    }
}
