namespace RTWWebServer.Database.Data;

public class Guest(int id, Guid guid)
{
    public int Id { get; set; } = id;
    public Guid Guid { get; set; } = guid;
}