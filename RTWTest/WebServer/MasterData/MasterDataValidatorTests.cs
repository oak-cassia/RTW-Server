using RTWWebServer.MasterDatas;
using RTWWebServer.MasterDatas.Models;

namespace RTWTest.WebServer.MasterData;

[TestFixture]
public class MasterDataValidatorTests
{
    [Test]
    public void Validate_WithValidData_ReturnsNoErrors()
    {
        var errors = MasterDataValidator.Validate(ValidCharacters(), ValidFurniture(), ValidRoomGrades(), ValidMissions());

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_WithEmptyCharactersArray_ReturnsFailure()
    {
        var errors = MasterDataValidator.Validate([], ValidFurniture(), ValidRoomGrades(), ValidMissions());

        Assert.That(errors, Has.Some.Contains("Characters array cannot be empty"));
    }

    [Test]
    public void Validate_WithDuplicateCharacterIds_ReturnsFailure()
    {
        CharacterMaster[] characters =
        [
            new() { Id = 1, Name = "Character1", Portfolio = 50, Development = 60, JobSearching = 70 },
            new() { Id = 1, Name = "Character2", Portfolio = 80, Development = 90, JobSearching = 85 }
        ];

        var errors = MasterDataValidator.Validate(characters, ValidFurniture(), ValidRoomGrades(), ValidMissions());

        Assert.That(errors, Has.Some.Contains("Duplicate character ID found: 1"));
    }

    [Test]
    public void Validate_WithInvalidCharacterData_ReturnsFailure()
    {
        CharacterMaster[] characters =
        [
            new() { Id = 0, Name = "", Portfolio = 0, Development = 101, JobSearching = -1 }
        ];

        var errors = MasterDataValidator.Validate(characters, ValidFurniture(), ValidRoomGrades(), ValidMissions());

        Assert.That(errors, Has.Count.GreaterThan(0));
    }

    [Test]
    public void Validate_WithEmptyFurnitureArray_ReturnsFailure()
    {
        var errors = MasterDataValidator.Validate(ValidCharacters(), [], ValidRoomGrades(), ValidMissions());

        Assert.That(errors, Has.Some.Contains("Furniture array cannot be empty"));
    }

    [Test]
    public void Validate_WithDuplicateFurnitureIds_ReturnsFailure()
    {
        FurnitureMaster[] furniture =
        [
            new() { Id = 2001, Name = "Furniture1", Category = 1, Width = 1, Height = 1 },
            new() { Id = 2001, Name = "Furniture2", Category = 2, Width = 2, Height = 2 }
        ];

        var errors = MasterDataValidator.Validate(ValidCharacters(), furniture, ValidRoomGrades(), ValidMissions());

        Assert.That(errors, Has.Some.Contains("Duplicate furniture ID found: 2001"));
    }

    [Test]
    public void Validate_WithEmptyRoomGradesArray_ReturnsFailure()
    {
        var errors = MasterDataValidator.Validate(ValidCharacters(), ValidFurniture(), [], ValidMissions());

        Assert.That(errors, Has.Some.Contains("RoomGrades array cannot be empty"));
    }

    [Test]
    public void Validate_WithDuplicateRoomGrade_ReturnsFailure()
    {
        RoomGradeMaster[] roomGrades =
        [
            new() { Grade = 1, Width = 30, Height = 30 },
            new() { Grade = 1, Width = 50, Height = 50 }
        ];

        var errors = MasterDataValidator.Validate(ValidCharacters(), ValidFurniture(), roomGrades, ValidMissions());

        Assert.That(errors, Has.Some.Contains("Duplicate room grade found: 1"));
    }

    [Test]
    public void Validate_WithoutGradeOne_ReturnsFailure()
    {
        RoomGradeMaster[] roomGrades = [new() { Grade = 2, Width = 50, Height = 50 }];

        var errors = MasterDataValidator.Validate(ValidCharacters(), ValidFurniture(), roomGrades, ValidMissions());

        Assert.That(errors, Has.Some.Contains("RoomGrades must contain grade 1"));
    }

    [Test]
    public void Validate_WithEmptyMissionsArray_ReturnsFailure()
    {
        var errors = MasterDataValidator.Validate(ValidCharacters(), ValidFurniture(), ValidRoomGrades(), []);

        Assert.That(errors, Has.Some.Contains("Missions array cannot be empty"));
    }

    [Test]
    public void Validate_WithDuplicateMissionIds_ReturnsFailure()
    {
        MissionMaster[] missions =
        [
            new() { Id = 101, Name = "Mission1", StaminaCost = 5, StartingMental = 100, RewardFame = 10, RewardGold = 20 },
            new() { Id = 101, Name = "Mission2", StaminaCost = 5, StartingMental = 100, RewardFame = 10, RewardGold = 20 }
        ];

        var errors = MasterDataValidator.Validate(ValidCharacters(), ValidFurniture(), ValidRoomGrades(), missions);

        Assert.That(errors, Has.Some.Contains("Duplicate mission ID found: 101"));
    }

    private static CharacterMaster[] ValidCharacters() =>
        [new() { Id = 1, Name = "Character", Portfolio = 50, Development = 60, JobSearching = 70 }];

    private static FurnitureMaster[] ValidFurniture() =>
        [new() { Id = 2001, Name = "Furniture", Category = 1, Width = 1, Height = 1 }];

    private static RoomGradeMaster[] ValidRoomGrades() =>
        [new() { Grade = 1, Width = 30, Height = 30 }];

    private static MissionMaster[] ValidMissions() =>
        [new() { Id = 101, Name = "Mission", StaminaCost = 5, StartingMental = 100, RewardFame = 10, RewardGold = 20 }];
}
