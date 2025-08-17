using System.ComponentModel.DataAnnotations;

namespace RTWWebServer.MasterDatas.Models;

public sealed class CharacterMaster
{
    [Range(1, int.MaxValue)]
    public int Id { get; init; }
    
    [Required, MinLength(1)]
    public string Name { get; init; } = "";
    
    [Range(1, 100)]
    public int Portfolio { get; init; }
    
    [Range(1, 100)]
    public int Development { get; init; }
    
    [Range(1, 100)]
    public int JobSearching { get; init; }
}