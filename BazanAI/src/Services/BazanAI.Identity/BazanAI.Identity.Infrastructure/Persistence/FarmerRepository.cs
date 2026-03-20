using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BazanAI.Identity.Domain.Entities;
using BazanAI.Identity.Domain.Repositories;
using MongoDB.Driver;

namespace BazanAI.Identity.Infrastructure.Persistence;

public class FarmerRepository : IFarmerRepository
{
    private readonly IMongoCollection<Farmer> _collection;

    public FarmerRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Farmer>("farmers");
    }

    public async Task<Farmer?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _collection.Find(f => f.Id == id).FirstOrDefaultAsync(ct);
    }

    public async Task<Farmer?> FindByIdentityAsync(string provider, string providerUserId, CancellationToken ct = default)
    {
        return await _collection.Find(f => f.Identities.Any(i => i.Provider == provider && i.ProviderUserId == providerUserId))
                                .FirstOrDefaultAsync(ct);
    }

    public async Task CreateAsync(Farmer farmer, CancellationToken ct = default)
    {
        await _collection.InsertOneAsync(farmer, cancellationToken: ct);
    }

    public async Task UpdateAsync(Farmer farmer, CancellationToken ct = default)
    {
        await _collection.ReplaceOneAsync(f => f.Id == farmer.Id, farmer, cancellationToken: ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await _collection.DeleteOneAsync(f => f.Id == id, cancellationToken: ct);
    }
}
