using System;

namespace BazanAI.Identity.Domain.Entities;

public record ExternalIdentity(
    string Provider,        // "phone_otp" | "google" | "email_password"
    string ProviderUserId,  // encrypted phone number, google uid, hoặc email
    string? Email = null,
    DateTime? LinkedAt = null
);
