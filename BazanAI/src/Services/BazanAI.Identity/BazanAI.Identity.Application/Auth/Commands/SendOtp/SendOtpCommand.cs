using System;
using System.Threading;
using System.Threading.Tasks;
using BazanAI.Identity.Application.Auth.Common;
using BazanAI.SharedKernel.Models;
using FluentValidation;
using MediatR;

namespace BazanAI.Identity.Application.Auth.Commands.SendOtp;

public record SendOtpCommand(string PhoneNumber) : IRequest<Result<SendOtpResponse>>;

public record SendOtpResponse(DateTime OtpExpiresAt, string Message = "OTP đã được gửi");

public class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Số điện thoại không được để trống")
            .Matches(@"^\+84[0-9]{9}$").WithMessage("Số điện thoại không hợp lệ (phải bắt đầu bằng +84 và có 9 chữ số tiếp theo)");
    }
}

public class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, Result<SendOtpResponse>>
{
    private readonly IOtpService _otpService;

    public SendOtpCommandHandler(IOtpService otpService)
    {
        _otpService = otpService;
    }

    public async Task<Result<SendOtpResponse>> Handle(SendOtpCommand request, CancellationToken ct)
    {
        var result = await _otpService.SendOtpAsync(request.PhoneNumber, ct);
        
        if (!result.IsSuccess)
            return Result<SendOtpResponse>.Failure(result.ErrorCode, result.Message);

        // Round to seconds to avoid leaking sub-second info
        var expiresAt = new DateTime(
            result.Value.Year, result.Value.Month, result.Value.Day, 
            result.Value.Hour, result.Value.Minute, result.Value.Second, 
            DateTimeKind.Utc);

        return Result<SendOtpResponse>.Success(new SendOtpResponse(expiresAt));
    }
}
