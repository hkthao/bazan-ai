using System.Threading;
using System.Threading.Tasks;
using BazanAI.Identity.Domain.Entities;

namespace BazanAI.Identity.Domain.Repositories;

public interface IFarmerRepository
{
    Task<Farmer?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Farmer?> FindByIdentityAsync(string provider, string providerUserId, CancellationToken ct = default);
    Task CreateAsync(Farmer farmer, CancellationToken ct = default);
    Task UpdateAsync(Farmer farmer, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
