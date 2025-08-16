using RTWWebServer.Configuration;
using RTWWebServer.Data.Entities;

namespace RTWTest.Webserver.MasterData;

[TestFixture]
public class MasterDataOptionsValidatorTests
{
    private readonly MasterDataOptionsValidator _validator = new MasterDataOptionsValidator();

    [Test]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        var options = new MasterDataOptions
        {
            Characters =
            [
                new CharacterMaster
                {
                    Id = 1,
                    Name = "ValidCharacter",
                    Portfolio = 50,
                    Development = 60,
                    JobSearching = 70
                }
            ]
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Failures, Is.Null.Or.Empty);
    }

    [Test]
    public void Validate_WithEmptyCharactersArray_ReturnsFailure()
    {
        var options = new MasterDataOptions
        {
            Characters = []
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failures, Has.Some.Contains("Characters array cannot be empty"));
    }

    [Test]
    public void Validate_WithDuplicateIds_ReturnsFailure()
    {
        var options = new MasterDataOptions
        {
            Characters =
            [
                new CharacterMaster { Id = 1, Name = "Character1", Portfolio = 50, Development = 60, JobSearching = 70 },
                new CharacterMaster { Id = 1, Name = "Character2", Portfolio = 80, Development = 90, JobSearching = 85 }
            ]
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failures?.ToList(), Has.Some.Contains("Duplicate character ID found: 1"));
    }

    [Test]
    public void Validate_WithInvalidCharacterData_ReturnsFailure()
    {
        var options = new MasterDataOptions
        {
            Characters =
            [
                new CharacterMaster
                {
                    Id = 0,
                    Name = "",
                    Portfolio = 0,
                    Development = 101,
                    JobSearching = -1
                }
            ]
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failures?.ToList(), Has.Count.GreaterThan(0));
    }

    [Test]
    public void Validate_WithLargeNumberOfCharacters_ValidatesAll()
    {
        var characters = new List<CharacterMaster>();
        for (int i = 1; i <= 100; i++)
        {
            characters.Add(new CharacterMaster
            {
                Id = i,
                Name = $"Character{i}",
                Portfolio = 50,
                Development = 60,
                JobSearching = 70
            });
        }

        var options = new MasterDataOptions { Characters = characters.ToArray() };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.True);
    }
}