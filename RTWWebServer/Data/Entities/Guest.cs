namespace RTWWebServer.Data.Entities;

public class Guest(long id, Guid guid)
{
    private Guest() : this(0, Guid.Empty)
    {
    }

    public Guest(Guid guid) : this(0, guid)
    {
    }

    public long Id { get; init; } = id;
    public Guid Guid { get; init; } = guid;
}