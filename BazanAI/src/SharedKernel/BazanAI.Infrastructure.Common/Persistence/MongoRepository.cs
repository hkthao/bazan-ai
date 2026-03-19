
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using BazanAI.SharedKernel.Domain;
using BazanAI.SharedKernel.Domain.Persistence;
using MongoDB.Driver;

namespace BazanAI.Infrastructure.Common.Persistence;

public abstract class MongoRepository<T> : MongoDbBase, IMongoRepository<T> where T : Entity
{
    protected readonly IMongoCollection<T> Collection;

    protected MongoRepository(string connectionString, string databaseName, string collectionName) 
        : base(connectionString, databaseName)
    {
        Collection = Database.GetCollection<T>(collectionName);
    }

    protected MongoRepository(IMongoDatabase database, string collectionName) 
        : base(database)
    {
        Collection = Database.GetCollection<T>(collectionName);
    }

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Collection.Find(_ => true).ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await Collection.Find(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        entity.MarkUpdated();
        await Collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
    }

    public virtual async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await Collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
    }
}
