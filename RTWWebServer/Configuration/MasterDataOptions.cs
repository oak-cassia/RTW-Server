using Microsoft.Extensions.Options;
using RTWWebServer.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.Configuration;

public sealed class MasterDataOptions
{
    public CharacterMaster[] Characters { get; init; } = [];
}

public sealed class MasterDataOptionsValidator : IValidateOptions<MasterDataOptions>
{
    public ValidateOptionsResult Validate(string? name, MasterDataOptions options)
    {
        var results = new List<string>();

        if (options.Characters.Length == 0)
        {
            results.Add("Characters array cannot be empty");
        }

        var duplicateIds = options.Characters.GroupBy(c => c.Id).Where(g => g.Count() > 1);
        foreach (var duplicate in duplicateIds)
        {
            results.Add($"Duplicate character ID found: {duplicate.Key}");
        }

        foreach (var character in options.Characters)
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

        return results.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(results);
    }
}