namespace HouseKeeper.Data;

public sealed class MongoDbOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "housekeeper";
}
