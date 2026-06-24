using System.ComponentModel.DataAnnotations;
using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.MasterDatas;

// 마스터 데이터 정합성 검증. System.Text.Json은 DataAnnotation을 자동 실행하지 않으므로
// 항목 검증은 여기서 Validator.TryValidateObject로 직접 수행한다.
public static class MasterDataValidator
{
    public static IReadOnlyList<string> Validate(
        IReadOnlyList<CharacterMaster> characters,
        IReadOnlyList<FurnitureMaster> furniture,
        IReadOnlyList<RoomGradeMaster> roomGrades,
        IReadOnlyList<MissionMaster> missions,
        IReadOnlyList<RankMaster> ranks)
    {
        var results = new List<string>();

        ValidateCharacters(characters, results);
        ValidateFurniture(furniture, results);
        ValidateRoomGrades(roomGrades, results);
        ValidateMissions(missions, results);
        ValidateRanks(ranks, results);

        return results;
    }

    // 검증 실패 시 모든 메시지를 묶어 예외를 던진다(시작 단계 fail-fast).
    public static void ValidateAndThrow(
        IReadOnlyList<CharacterMaster> characters,
        IReadOnlyList<FurnitureMaster> furniture,
        IReadOnlyList<RoomGradeMaster> roomGrades,
        IReadOnlyList<MissionMaster> missions,
        IReadOnlyList<RankMaster> ranks)
    {
        var errors = Validate(characters, furniture, roomGrades, missions, ranks);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Master data validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
    }

    private static void ValidateCharacters(IReadOnlyList<CharacterMaster> characters, List<string> results)
    {
        if (characters.Count == 0)
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
            ValidateItem(character, $"Character {character.Id}", results);
        }
    }

    private static void ValidateFurniture(IReadOnlyList<FurnitureMaster> furnitureItems, List<string> results)
    {
        if (furnitureItems.Count == 0)
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
            ValidateItem(furniture, $"Furniture {furniture.Id}", results);
        }
    }

    private static void ValidateRoomGrades(IReadOnlyList<RoomGradeMaster> roomGrades, List<string> results)
    {
        if (roomGrades.Count == 0)
        {
            results.Add("RoomGrades array cannot be empty");
        }

        var duplicateGrades = roomGrades.GroupBy(g => g.Grade).Where(g => g.Count() > 1);
        foreach (var duplicate in duplicateGrades)
        {
            results.Add($"Duplicate room grade found: {duplicate.Key}");
        }

        // 행이 없는 유저는 1등급(기본)으로 간주하므로 1등급 마스터는 반드시 존재해야 한다.
        if (roomGrades.Count > 0 && roomGrades.All(g => g.Grade != 1))
        {
            results.Add("RoomGrades must contain grade 1 (default room size)");
        }

        foreach (var roomGrade in roomGrades)
        {
            ValidateItem(roomGrade, $"RoomGrade {roomGrade.Grade}", results);
        }
    }

    private static void ValidateMissions(IReadOnlyList<MissionMaster> missions, List<string> results)
    {
        if (missions.Count == 0)
        {
            results.Add("Missions array cannot be empty");
        }

        var duplicateIds = missions.GroupBy(m => m.Id).Where(g => g.Count() > 1);
        foreach (var duplicate in duplicateIds)
        {
            results.Add($"Duplicate mission ID found: {duplicate.Key}");
        }

        foreach (var mission in missions)
        {
            ValidateItem(mission, $"Mission {mission.Id}", results);
        }
    }

    private static void ValidateRanks(IReadOnlyList<RankMaster> ranks, List<string> results)
    {
        if (ranks.Count == 0)
        {
            results.Add("Ranks array cannot be empty");
        }

        var duplicateRanks = ranks.GroupBy(r => r.Rank).Where(g => g.Count() > 1);
        foreach (var duplicate in duplicateRanks)
        {
            results.Add($"Duplicate rank found: {duplicate.Key}");
        }

        // 명성 0(가입 시점)에서도 항상 하나의 랭크로 떨어지도록 baseline(RequiredFame 0)이 있어야 한다.
        if (ranks.Count > 0 && ranks.All(r => r.RequiredFame != 0))
        {
            results.Add("Ranks must contain a baseline rank with RequiredFame 0");
        }

        // 랭크가 오를수록 누적 명성 임계값도 엄격히 증가해야 한다. 파생(GetRankByFame)의 단조성 전제이자,
        // 임계값이 같은 두 랭크가 있으면 어느 랭크로 떨어질지 모호해지는 것을 막는다.
        var orderedByRank = ranks.OrderBy(r => r.Rank).ToList();
        for (int i = 1; i < orderedByRank.Count; i++)
        {
            if (orderedByRank[i].RequiredFame <= orderedByRank[i - 1].RequiredFame)
            {
                results.Add(
                    $"Rank {orderedByRank[i].Rank} RequiredFame must be greater than rank {orderedByRank[i - 1].Rank}");
            }
        }

        foreach (var rank in ranks)
        {
            ValidateItem(rank, $"Rank {rank.Rank}", results);
        }
    }

    private static void ValidateItem(object item, string label, List<string> results)
    {
        var context = new ValidationContext(item);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(item, context, validationResults, true))
        {
            foreach (var validationResult in validationResults)
            {
                results.Add($"{label}: {validationResult.ErrorMessage}");
            }
        }
    }
}
