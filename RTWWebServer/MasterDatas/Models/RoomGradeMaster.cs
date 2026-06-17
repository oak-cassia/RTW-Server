using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.MasterDatas.Models;

public sealed class RoomGradeMaster
{
    [Range(1, int.MaxValue)]
    public int Grade { get; init; }

    [Range(1, 1000)]
    public int Width { get; init; }

    [Range(1, 1000)]
    public int Height { get; init; }
}
