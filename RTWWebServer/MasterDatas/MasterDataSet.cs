using System.Collections.Immutable;
using RTWWebServer.MasterDatas.Models;

namespace RTWWebServer.MasterDatas;

// 로더가 만들어 내는 불변 마스터 데이터 스냅샷. provider가 이걸 받아 그대로 들고 조회만 한다.
public sealed class MasterDataSet(
    ImmutableDictionary<int, CharacterMaster> characters,
    ImmutableDictionary<int, FurnitureMaster> furniture,
    ImmutableDictionary<int, RoomGradeMaster> roomGrades)
{
    public ImmutableDictionary<int, CharacterMaster> Characters { get; } = characters;
    public ImmutableDictionary<int, FurnitureMaster> Furniture { get; } = furniture;
    public ImmutableDictionary<int, RoomGradeMaster> RoomGrades { get; } = roomGrades;
}
