using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.MasterDatas.Models;

public sealed class FurnitureMaster
{
    [Range(1, int.MaxValue)]
    public int Id { get; init; }

    [Required, MinLength(1)]
    public string Name { get; init; } = "";

    [Range(1, 100)]
    public int Category { get; init; }

    [Range(1, 100)]
    public int Width { get; init; }

    [Range(1, 100)]
    public int Height { get; init; }
}
