using System;
using System.Threading;
using System.Threading.Tasks;
using BazanAI.SharedKernel.Models;

namespace BazanAI.Identity.Application.Auth.Common;

/// <summary>
/// Service for generating, storing, and sending OTP for authentication.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a 6-digit OTP, stores its hash in Redis, and sends it to the user.
    /// </summary>
    /// <param name="phoneNumber">User's phone number in format +84...</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Expiry time of the generated OTP</returns>
    Task<Result<DateTime>> SendOtpAsync(string phoneNumber, CancellationToken ct = default);

    /// <summary>
    /// Verifies the provided OTP against the stored hash in Redis.
    /// </summary>
    /// <param name="phoneNumber">User's phone number</param>
    /// <param name="otp">Plaintext 6-digit OTP</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if OTP is valid and matches</returns>
    Task<Result<bool>> VerifyOtpAsync(string phoneNumber, string otp, CancellationToken ct = default);
}
