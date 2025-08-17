using System.ComponentModel.DataAnnotations;
using RTWWebServer.MasterDatas.Models;

namespace RTWTest.Webserver.MasterData;

[TestFixture]
public class CharacterMasterTests
{
    [Test]
    public void Validation_WithValidData_ReturnsNoErrors()
    {
        var character = new CharacterMaster
        {
            Id = 1,
            Name = "ValidCharacter",
            Portfolio = 50,
            Development = 75,
            JobSearching = 30
        };

        var validationResults = ValidateCharacter(character);
        
        Assert.That(validationResults, Is.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Validation_WithInvalidId_ReturnsValidationError(int invalidId)
    {
        var character = new CharacterMaster
        {
            Id = invalidId,
            Name = "TestCharacter",
            Portfolio = 50,
            Development = 75,
            JobSearching = 30
        };

        var validationResults = ValidateCharacter(character);
        
        Assert.That(validationResults, Is.Not.Empty);
    }

    [TestCase("")]
    [TestCase(null)]
    public void Validation_WithInvalidName_ReturnsValidationError(string? invalidName)
    {
        var character = new CharacterMaster
        {
            Id = 1,
            Name = invalidName!,
            Portfolio = 50,
            Development = 75,
            JobSearching = 30
        };

        var validationResults = ValidateCharacter(character);
        
        Assert.That(validationResults, Is.Not.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(101)]
    public void Validation_WithInvalidRange_ReturnsValidationError(int invalidValue)
    {
        var character = new CharacterMaster
        {
            Id = 1,
            Name = "TestCharacter",
            Portfolio = invalidValue,
            Development = 50,
            JobSearching = 50
        };

        var validationResults = ValidateCharacter(character);
        
        Assert.That(validationResults, Is.Not.Empty);
    }

    [TestCase(1, 1, 1)]
    [TestCase(100, 100, 100)]
    public void Validation_WithBoundaryValues_IsValid(int portfolio, int development, int jobSearching)
    {
        var character = new CharacterMaster
        {
            Id = 1,
            Name = "BoundaryTest",
            Portfolio = portfolio,
            Development = development,
            JobSearching = jobSearching
        };

        var validationResults = ValidateCharacter(character);
        
        Assert.That(validationResults, Is.Empty);
    }

    private static List<ValidationResult> ValidateCharacter(CharacterMaster character)
    {
        var context = new ValidationContext(character);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(character, context, results, true);
        return results;
    }
}