namespace Play.Common.Settings;

public class MongoDbSettings
{
    private string connectionString;

    public string Host { get; init; }
    public int Port { get; init; }

    public string ConnectionString
    {
        get
        {
            return string.IsNullOrWhiteSpace(connectionString)
                ? $"mongodb://{Host}:{Port}" //expression body definition
                : connectionString;
        }
        init { connectionString = value; }
    }

}