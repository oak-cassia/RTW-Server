using RTWWebServer.Configuration;
using RTWWebServer.MasterDatas.Models;

namespace RTWTest.WebServer.MasterData;

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
            ],
            Furniture =
            [
                new FurnitureMaster { Id = 2001, Name = "ValidFurniture", Category = 1, Width = 1, Height = 1 }
            ],
            RoomGrades =
            [
                new RoomGradeMaster { Grade = 1, Width = 30, Height = 30 }
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

        var options = new MasterDataOptions
        {
            Characters = characters.ToArray(),
            Furniture = [new FurnitureMaster { Id = 2001, Name = "Furniture", Category = 1, Width = 1, Height = 1 }],
            RoomGrades = [new RoomGradeMaster { Grade = 1, Width = 30, Height = 30 }]
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void Validate_WithEmptyFurnitureArray_ReturnsFailure()
    {
        var options = new MasterDataOptions
        {
            Characters = [new CharacterMaster { Id = 1, Name = "Character", Portfolio = 50, Development = 60, JobSearching = 70 }],
            Furniture = []
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failures, Has.Some.Contains("Furniture array cannot be empty"));
    }

    [Test]
    public void Validate_WithDuplicateFurnitureIds_ReturnsFailure()
    {
        var options = new MasterDataOptions
        {
            Characters = [new CharacterMaster { Id = 1, Name = "Character", Portfolio = 50, Development = 60, JobSearching = 70 }],
            Furniture =
            [
                new FurnitureMaster { Id = 2001, Name = "Furniture1", Category = 1, Width = 1, Height = 1 },
                new FurnitureMaster { Id = 2001, Name = "Furniture2", Category = 2, Width = 2, Height = 2 }
            ]
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failures?.ToList(), Has.Some.Contains("Duplicate furniture ID found: 2001"));
    }

    [Test]
    public void Validate_WithEmptyRoomGradesArray_ReturnsFailure()
    {
        var options = new MasterDataOptions
        {
            Characters = [new CharacterMaster { Id = 1, Name = "Character", Portfolio = 50, Development = 60, JobSearching = 70 }],
            Furniture = [new FurnitureMaster { Id = 2001, Name = "Furniture", Category = 1, Width = 1, Height = 1 }],
            RoomGrades = []
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failures, Has.Some.Contains("RoomGrades array cannot be empty"));
    }

    [Test]
    public void Validate_WithDuplicateRoomGrade_ReturnsFailure()
    {
        var options = new MasterDataOptions
        {
            Characters = [new CharacterMaster { Id = 1, Name = "Character", Portfolio = 50, Development = 60, JobSearching = 70 }],
            Furniture = [new FurnitureMaster { Id = 2001, Name = "Furniture", Category = 1, Width = 1, Height = 1 }],
            RoomGrades =
            [
                new RoomGradeMaster { Grade = 1, Width = 30, Height = 30 },
                new RoomGradeMaster { Grade = 1, Width = 50, Height = 50 }
            ]
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failures?.ToList(), Has.Some.Contains("Duplicate room grade found: 1"));
    }

    [Test]
    public void Validate_WithoutGradeOne_ReturnsFailure()
    {
        var options = new MasterDataOptions
        {
            Characters = [new CharacterMaster { Id = 1, Name = "Character", Portfolio = 50, Development = 60, JobSearching = 70 }],
            Furniture = [new FurnitureMaster { Id = 2001, Name = "Furniture", Category = 1, Width = 1, Height = 1 }],
            RoomGrades = [new RoomGradeMaster { Grade = 2, Width = 50, Height = 50 }]
        };

        var result = _validator.Validate(null, options);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failures, Has.Some.Contains("RoomGrades must contain grade 1"));
    }
}