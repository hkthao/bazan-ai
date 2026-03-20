using System;
using System.Linq;
using BazanAI.Identity.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BazanAI.Identity.Tests.Domain;

public class FarmerTests
{
    private const string TestFullName = "Nguyễn Văn An";
    private const string TestPhone = "+84901234567";
    private const string TestEmail = "an.nguyen@example.com";
    private const string TestIp = "127.0.0.1";
    private const string TestPolicyVersion = "1.0.0";

    [Fact]
    public void RegisterWithPhone_WithValidConsent_ShouldCreateFarmer()
    {
        // Arrange
        var consent = new ConsentRecord(true, DateTime.UtcNow, TestPolicyVersion, TestIp);

        // Act
        var farmer = Farmer.RegisterWithPhone(TestPhone, TestFullName, consent);

        // Assert
        farmer.Should().NotBeNull();
        farmer.FullName.Should().Be(TestFullName);
        farmer.PrivacyConsent.Should().NotBeNull();
        farmer.PrivacyConsent!.Accepted.Should().BeTrue();
        
        farmer.Identities.Should().HaveCount(1);
        var identity = farmer.Identities.First();
        identity.Provider.Should().Be("phone_otp");
        identity.ProviderUserId.Should().Be(TestPhone);
        
        farmer.GetPhone().Should().Be(TestPhone);
    }

    [Fact]
    public void RegisterWithPhone_WithNullConsent_ShouldThrowException()
    {
        // Act
        Action act = () => Farmer.RegisterWithPhone(TestPhone, TestFullName, null!);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Consent is required for registration.");
    }

    [Fact]
    public void RegisterWithPhone_WithUnacceptedConsent_ShouldThrowException()
    {
        // Arrange
        var consent = new ConsentRecord(false, DateTime.UtcNow, TestPolicyVersion, TestIp);

        // Act
        Action act = () => Farmer.RegisterWithPhone(TestPhone, TestFullName, consent);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Consent is required for registration.");
    }

    [Fact]
    public void RegisterWithOAuth_ShouldCreateFarmerWithEmail()
    {
        // Arrange
        var consent = new ConsentRecord(true, DateTime.UtcNow, TestPolicyVersion, TestIp);

        // Act
        var farmer = Farmer.RegisterWithOAuth("google", "google-uid-123", TestEmail, TestFullName, consent);

        // Assert
        farmer.FullName.Should().Be(TestFullName);
        farmer.PrimaryEmail.Should().Be(TestEmail);
        farmer.Identities.Should().HaveCount(1);
        
        var identity = farmer.Identities.First();
        identity.Provider.Should().Be("google");
        identity.ProviderUserId.Should().Be("google-uid-123");
        identity.Email.Should().Be(TestEmail);
    }

    [Fact]
    public void LinkIdentity_WhenNewProvider_ShouldAddIdentity()
    {
        // Arrange
        var consent = new ConsentRecord(true, DateTime.UtcNow, TestPolicyVersion, TestIp);
        var farmer = Farmer.RegisterWithPhone(TestPhone, TestFullName, consent);
        var googleIdentity = new ExternalIdentity("google", "google-uid-123", TestEmail, DateTime.UtcNow);

        // Act
        farmer.LinkIdentity(googleIdentity);

        // Assert
        farmer.Identities.Should().HaveCount(2);
        farmer.Identities.Should().Contain(i => i.Provider == "google");
    }

    [Fact]
    public void LinkIdentity_WhenExistingProvider_ShouldThrowException()
    {
        // Arrange
        var consent = new ConsentRecord(true, DateTime.UtcNow, TestPolicyVersion, TestIp);
        var farmer = Farmer.RegisterWithPhone(TestPhone, TestFullName, consent);
        var duplicatePhoneIdentity = new ExternalIdentity("phone_otp", "+84999999999", null, DateTime.UtcNow);

        // Act
        Action act = () => farmer.LinkIdentity(duplicatePhoneIdentity);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already linked*");
    }

    [Fact]
    public void UpdateProfile_ShouldChangeProperties()
    {
        // Arrange
        var consent = new ConsentRecord(true, DateTime.UtcNow, TestPolicyVersion, TestIp);
        var farmer = Farmer.RegisterWithPhone(TestPhone, TestFullName, consent);

        // Act
        farmer.UpdateProfile("Nguyễn Văn B", "advanced");

        // Assert
        farmer.FullName.Should().Be("Nguyễn Văn B");
        farmer.LiteracyLevel.Should().Be("advanced");
    }
}
