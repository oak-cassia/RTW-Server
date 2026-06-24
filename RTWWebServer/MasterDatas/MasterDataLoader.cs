using System.Collections.Immutable;
using System.Text.Json;
using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.MasterDatas;

// MasterDatas 디렉터리의 JSON을 System.Text.Json으로 직접 역직렬화해 마스터 스냅샷을 만든다.
// (IConfiguration/Options 경로를 거치지 않는다.)
public sealed class MasterDataLoader(string masterDataDirectory) : IMasterDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MasterDataSet Load()
    {
        var characters = LoadSection<CharacterMaster>("CharacterMaster.json", "Characters");
        var furniture = LoadSection<FurnitureMaster>("FurnitureMaster.json", "Furniture");
        var roomGrades = LoadSection<RoomGradeMaster>("RoomGradeMaster.json", "RoomGrades");
        var missions = LoadSection<MissionMaster>("MissionMaster.json", "Missions");
        var ranks = LoadSection<RankMaster>("RankMaster.json", "Ranks");

        MasterDataValidator.ValidateAndThrow(characters, furniture, roomGrades, missions, ranks);

        return new MasterDataSet(
            characters.ToImmutableDictionary(c => c.Id),
            furniture.ToImmutableDictionary(f => f.Id),
            roomGrades.ToImmutableDictionary(g => g.Grade),
            missions.ToImmutableDictionary(m => m.Id),
            ranks.ToImmutableDictionary(r => r.Rank));
    }

    // 래퍼 객체({"Characters": [...]})에서 명명 배열을 꺼내 타입 배열로 역직렬화한다.
    // 파일 누락/속성 누락은 예외로 띄워 기동을 막는다(fail-fast).
    private T[] LoadSection<T>(string fileName, string propertyName)
    {
        var path = Path.Combine(masterDataDirectory, fileName);

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty(propertyName, out var arrayElement))
        {
            throw new InvalidOperationException($"Master data file '{fileName}' is missing the '{propertyName}' property.");
        }

        return arrayElement.Deserialize<T[]>(JsonOptions)
            ?? throw new InvalidOperationException($"Master data file '{fileName}' has a null '{propertyName}' array.");
    }
}
