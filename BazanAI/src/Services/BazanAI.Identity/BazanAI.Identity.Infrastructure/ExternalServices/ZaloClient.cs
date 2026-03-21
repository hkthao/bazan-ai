using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BazanAI.Identity.Application.Auth.Common;
using BazanAI.SharedKernel.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BazanAI.Identity.Infrastructure.ExternalServices;

public class ZaloClient : IZaloClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accessToken;
    private readonly string _templateId;
    private readonly ILogger<ZaloClient> _logger;

    public ZaloClient(HttpClient httpClient, IConfiguration configuration, ILogger<ZaloClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _accessToken = configuration["ZALO_OA_ACCESS_TOKEN"] ?? throw new InvalidOperationException("ZALO_OA_ACCESS_TOKEN is not configured.");
        _templateId = configuration["ZALO_OA_OTP_TEMPLATE"] ?? throw new InvalidOperationException("ZALO_OA_OTP_TEMPLATE is not configured.");
        
        _httpClient.BaseAddress = new Uri("https://openapi.zalo.me/");
    }

    public async Task<Result<bool>> SendOtpAsync(string phoneNumber, string otp, CancellationToken ct = default)
    {
        try
        {
            var requestBody = new
            {
                recipient = new { user_id = phoneNumber }, // Note: Real ZNS uses user_id or phone
                message = new
                {
                    attachment = new
                    {
                        type = "template",
                        payload = new
                        {
                            template_type = "zns",
                            template_id = _templateId,
                            template_data = new { otp = otp }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("access_token", _accessToken);

            // POST v3.0/oa/message/cs
            var response = await _httpClient.PostAsync("v3.0/oa/message/cs", content, ct);
            
            if (response.IsSuccessStatusCode)
            {
                return Result<bool>.Success(true);
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Zalo API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
            return Result<bool>.Failure("zalo_api_error", $"Zalo API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Zalo message to {PhoneNumber}", phoneNumber);
            return Result<bool>.Failure("zalo_exception", "Internal error calling Zalo API");
        }
    }
}
