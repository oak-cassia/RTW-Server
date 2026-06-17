using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.Configuration;

public sealed class MasterDataOptions
{
    public CharacterMaster[] Characters { get; init; } = [];
    public FurnitureMaster[] Furniture { get; init; } = [];
}

public sealed class MasterDataOptionsValidator : IValidateOptions<MasterDataOptions>
{
    public ValidateOptionsResult Validate(string? name, MasterDataOptions options)
    {
        var results = new List<string>();

        ValidateCharacters(options.Characters, results);
        ValidateFurniture(options.Furniture, results);

        return results.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(results);
    }

    private static void ValidateCharacters(CharacterMaster[] characters, List<string> results)
    {
        if (characters.Length == 0)
        {
            results.Add("Characters array cannot be empty");
        }

        var duplicateIds = characters.GroupBy(c => c.Id).Where(g => g.Count() > 1);
        foreach (var duplicate in duplicateIds)
        {
            results.Add($"Duplicate character ID found: {duplicate.Key}");
        }

        foreach (var character in characters)
        {
            var context = new ValidationContext(character);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(character, context, validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    results.Add($"Character {character.Id}: {validationResult.ErrorMessage}");
                }
            }
        }
    }

    private static void ValidateFurniture(FurnitureMaster[] furnitureItems, List<string> results)
    {
        if (furnitureItems.Length == 0)
        {
            results.Add("Furniture array cannot be empty");
        }

        var duplicateIds = furnitureItems.GroupBy(f => f.Id).Where(g => g.Count() > 1);
        foreach (var duplicate in duplicateIds)
        {
            results.Add($"Duplicate furniture ID found: {duplicate.Key}");
        }

        foreach (var furniture in furnitureItems)
        {
            var context = new ValidationContext(furniture);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(furniture, context, validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    results.Add($"Furniture {furniture.Id}: {validationResult.ErrorMessage}");
                }
            }
        }
    }
}