namespace RTWWebServer.Configuration;

public class GameEventLogConfiguration
{
    public bool Enabled { get; set; } = false;
    public string ClickHouse { get; set; } = "";
    public string Database { get; set; } = "rtw_game_log";
    public int BatchSize { get; set; } = 500;
    public int FlushIntervalSeconds { get; set; } = 5;
    public int QueueCapacity { get; set; } = 10000;
}
