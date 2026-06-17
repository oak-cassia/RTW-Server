using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.Configuration;

public sealed class MasterDataOptions
{
    public CharacterMaster[] Characters { get; init; } = [];
    public FurnitureMaster[] Furniture { get; init; } = [];
    public RoomGradeMaster[] RoomGrades { get; init; } = [];
}

public sealed class MasterDataOptionsValidator : IValidateOptions<MasterDataOptions>
{
    public ValidateOptionsResult Validate(string? name, MasterDataOptions options)
    {
        var results = new List<string>();

        ValidateCharacters(options.Characters, results);
        ValidateFurniture(options.Furniture, results);
        ValidateRoomGrades(options.RoomGrades, results);

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

    private static void ValidateRoomGrades(RoomGradeMaster[] roomGrades, List<string> results)
    {
        if (roomGrades.Length == 0)
        {
            results.Add("RoomGrades array cannot be empty");
        }

        var duplicateGrades = roomGrades.GroupBy(g => g.Grade).Where(g => g.Count() > 1);
        foreach (var duplicate in duplicateGrades)
        {
            results.Add($"Duplicate room grade found: {duplicate.Key}");
        }

        // 행이 없는 유저는 1등급(기본)으로 간주하므로 1등급 마스터는 반드시 존재해야 한다.
        if (roomGrades.Length > 0 && roomGrades.All(g => g.Grade != 1))
        {
            results.Add("RoomGrades must contain grade 1 (default room size)");
        }

        foreach (var roomGrade in roomGrades)
        {
            var context = new ValidationContext(roomGrade);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(roomGrade, context, validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    results.Add($"RoomGrade {roomGrade.Grade}: {validationResult.ErrorMessage}");
                }
            }
        }
    }
}