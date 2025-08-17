using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RTWWebServer.Configuration;
using RTWWebServer.MasterDatas.Models;
using RTWWebServer.Providers.MasterData;

namespace RTWTest.Webserver.MasterData;

[TestFixture]
public class MasterDataProviderTests
{
    private Mock<ILogger<MasterDataProvider>> _mockLogger = null!;
    private MasterDataOptions _testOptions = null!;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<MasterDataProvider>>();

        _testOptions = new MasterDataOptions
        {
            Characters =
            [
                new CharacterMaster
                {
                    Id = 1,
                    Name = "Character1",
                    Portfolio = 50,
                    Development = 60,
                    JobSearching = 70
                },
                new CharacterMaster
                {
                    Id = 2,
                    Name = "Character2",
                    Portfolio = 80,
                    Development = 90,
                    JobSearching = 75
                }
            ]
        };
    }

    [Test]
    public void TryGetCharacter_ReturnsCorrectResults()
    {
        var mockOptionsMonitor = CreateMockOptionsMonitor(_testOptions);
        using var service = new MasterDataProvider(mockOptionsMonitor.Object, _mockLogger.Object);

        Assert.That(service.TryGetCharacter(1, out var char1), Is.True);
        Assert.That(char1?.Name, Is.EqualTo("Character1"));

        Assert.That(service.TryGetCharacter(2, out var char2), Is.True);
        Assert.That(char2?.Name, Is.EqualTo("Character2"));

        Assert.That(service.TryGetCharacter(999, out _), Is.False);
    }

    [Test]
    public void GetAllCharacters_ReturnsAllCharacters()
    {
        var mockOptionsMonitor = CreateMockOptionsMonitor(_testOptions);
        using var service = new MasterDataProvider(mockOptionsMonitor.Object, _mockLogger.Object);

        var characters = service.GetAllCharacters();

        Assert.That(characters, Has.Count.EqualTo(2));
        Assert.That(characters, Is.AssignableTo<IReadOnlyCollection<CharacterMaster>>());
    }

    [Test]
    public void Service_WithEmptyCharacters_HandlesGracefully()
    {
        var emptyOptions = new MasterDataOptions { Characters = [] };
        var mockOptionsMonitor = CreateMockOptionsMonitor(emptyOptions);
        using var emptyService = new MasterDataProvider(mockOptionsMonitor.Object, _mockLogger.Object);

        var characters = emptyService.GetAllCharacters();
        Assert.That(characters, Is.Empty);
        Assert.That(emptyService.TryGetCharacter(1, out _), Is.False);
    }

    private static Mock<IOptionsMonitor<MasterDataOptions>> CreateMockOptionsMonitor(MasterDataOptions options)
    {
        var mockOptionsMonitor = new Mock<IOptionsMonitor<MasterDataOptions>>();
        mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        return mockOptionsMonitor;
    }
}