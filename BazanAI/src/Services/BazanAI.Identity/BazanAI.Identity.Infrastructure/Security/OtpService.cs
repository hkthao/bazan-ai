using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BazanAI.Identity.Application.Auth.Common;
using BazanAI.SharedKernel.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BazanAI.Identity.Infrastructure.Security;

public class OtpService : IOtpService
{
    private readonly IDistributedCache _cache;
    private readonly IZaloClient _zaloClient;
    private readonly string _pepper;
    private readonly ILogger<OtpService> _logger;
    private const int OtpExpiryMinutes = 5;

    public OtpService(
        IDistributedCache cache, 
        IZaloClient zaloClient,
        IConfiguration configuration, 
        ILogger<OtpService> logger)
    {
        _cache = cache;
        _zaloClient = zaloClient;
        _logger = logger;
        _pepper = configuration["OTP_PEPPER"] ?? throw new InvalidOperationException("OTP_PEPPER is not configured.");
    }

    public async Task<Result<DateTime>> SendOtpAsync(string phoneNumber, CancellationToken ct = default)
    {
        try
        {
            // 1. Generate 6-digit OTP
            var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // 2. Hash OTP before storing
            var hashedOtp = HashOtp(otp, phoneNumber);

            // 3. Store in Redis
            var expiry = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpiryMinutes)
            };

            await _cache.SetStringAsync($"otp:{phoneNumber}", hashedOtp, cacheOptions, ct);

            // 4. Send via Zalo
            var zaloResult = await _zaloClient.SendOtpAsync(phoneNumber, otp, ct);
            if (!zaloResult.IsSuccess)
            {
                _logger.LogWarning("Zalo send failed for {PhoneNumber}, returning failure.", phoneNumber);
                return Result<DateTime>.Failure(zaloResult.ErrorCode, "Không thể gửi mã OTP qua Zalo.");
            }

            _logger.LogInformation("OTP sent successfully to {PhoneNumber}", phoneNumber);
            return Result<DateTime>.Success(expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP to {PhoneNumber}", phoneNumber);
            return Result<DateTime>.Failure("otp_send_failed", "Không thể gửi mã OTP. Vui lòng thử lại sau.");
        }
    }

    public async Task<Result<bool>> VerifyOtpAsync(string phoneNumber, string otp, CancellationToken ct = default)
    {
        var storedHash = await _cache.GetStringAsync($"otp:{phoneNumber}", ct);
        if (string.IsNullOrEmpty(storedHash))
        {
            return Result<bool>.Failure("otp_expired", "Mã OTP đã hết hạn hoặc không tồn tại.");
        }

        var currentHash = HashOtp(otp, phoneNumber);
        if (storedHash != currentHash)
        {
            return Result<bool>.Failure("otp_invalid", "Mã OTP không chính xác.");
        }

        // Delete OTP after successful verification
        await _cache.RemoveAsync($"otp:{phoneNumber}", ct);

        return Result<bool>.Success(true);
    }

    private string HashOtp(string otp, string phoneNumber)
    {
        // SHA256(otp + phoneNumber + pepper) to prevent global rainbow tables
        var input = $"{otp}{phoneNumber}{_pepper}";
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
