
using MongoDB.Driver;

namespace BazanAI.Infrastructure.Common.Persistence;

public abstract class MongoDbBase
{
    protected readonly IMongoDatabase Database;

    protected MongoDbBase(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        Database = client.GetDatabase(databaseName);
    }
}
