using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BazanAI.SharedKernel.Domain;

namespace BazanAI.Identity.Domain.Entities;

public class Farmer : AggregateRoot
{
    public required string FullName { get; set; }
    public string? PrimaryEmail { get; private set; }
    public string? ZaloId { get; private set; }
    public string PreferredLanguage { get; private set; } = "vi-VN";
    public string? LiteracyLevel { get; private set; } // "basic" | "medium" | "advanced"
    public ConsentRecord? PrivacyConsent { get; private set; }

    private readonly List<ExternalIdentity> _identities = new();
    public IReadOnlyCollection<ExternalIdentity> Identities => _identities.AsReadOnly();

    private readonly List<string> _farmIds = new();
    public IReadOnlyCollection<string> FarmIds => _farmIds.AsReadOnly();

    // EF Core / MongoDB required constructor
    public Farmer() { }

    [SetsRequiredMembers]
    public Farmer(string fullName, IEnumerable<ExternalIdentity> identities, ConsentRecord? consent = null)
    {
        FullName = fullName;
        _identities.AddRange(identities);
        PrivacyConsent = consent;
    }

    // Factory method cho phone_otp (dùng bởi flow hiện tại)
    public static Farmer RegisterWithPhone(
        string encryptedPhone,
        string fullName,
        ConsentRecord consent)
    {
        if (consent == null || !consent.Accepted)
            throw new InvalidOperationException("Consent is required for registration.");

        var identity = new ExternalIdentity(
            Provider: "phone_otp",
            ProviderUserId: encryptedPhone,
            LinkedAt: DateTime.UtcNow
        );

        return new Farmer(fullName, new[] { identity }, consent);
    }

    // Factory method cho OAuth
    public static Farmer RegisterWithOAuth(
        string provider,
        string providerUserId,
        string email,
        string fullName,
        ConsentRecord consent)
    {
        if (consent == null || !consent.Accepted)
            throw new InvalidOperationException("Consent is required for registration.");

        var identity = new ExternalIdentity(
            Provider: provider,
            ProviderUserId: providerUserId,
            Email: email,
            LinkedAt: DateTime.UtcNow
        );

        var farmer = new Farmer(fullName, new[] { identity }, consent)
        {
            PrimaryEmail = email
        };
        return farmer;
    }

    public void LinkIdentity(ExternalIdentity newIdentity)
    {
        if (_identities.Any(i => i.Provider == newIdentity.Provider))
            throw new InvalidOperationException($"Identity for provider {newIdentity.Provider} is already linked.");

        _identities.Add(newIdentity);
    }

    public string? GetPhone()
        => _identities.FirstOrDefault(i => i.Provider == "phone_otp")?.ProviderUserId;

    public void AddFarm(string farmId)
    {
        if (!_farmIds.Contains(farmId))
        {
            _farmIds.Add(farmId);
        }
    }

    public void UpdateProfile(string fullName, string? literacyLevel)
    {
        FullName = fullName;
        LiteracyLevel = literacyLevel;
    }
}
