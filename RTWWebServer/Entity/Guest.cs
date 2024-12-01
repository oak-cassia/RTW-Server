namespace RTWWebServer.Entity;

public class Guest(long id, Guid guid)
{
    public long Id { get; set; } = id;
    public Guid Guid { get; set; } = guid;
}