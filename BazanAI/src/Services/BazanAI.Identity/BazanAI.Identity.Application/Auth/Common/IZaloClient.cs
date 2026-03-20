using System.Threading;
using System.Threading.Tasks;
using BazanAI.SharedKernel.Models;

namespace BazanAI.Identity.Application.Auth.Common;

public interface IZaloClient
{
    Task<Result<bool>> SendOtpAsync(string phoneNumber, string otp, CancellationToken ct = default);
}
