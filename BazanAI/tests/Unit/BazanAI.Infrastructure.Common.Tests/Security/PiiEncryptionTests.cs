using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BazanAI.Infrastructure.Common.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace BazanAI.Infrastructure.Common.Tests.Security;

public class PiiEncryptionTests
{
    private readonly AesGcmPiiEncryptionService _sut;
    private const string TestKeyHex = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"; // 32 bytes

    public PiiEncryptionTests()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["PII_ENCRYPTION_KEY"].Returns(TestKeyHex);
        _sut = new AesGcmPiiEncryptionService(configuration);
    }

    [Fact]
    public void Encrypt_WhenValidPlaintext_ReturnsBase64String()
    {
        // Arrange
        var plaintext = "Hello Bazan AI";

        // Act
        var ciphertext = _sut.Encrypt(plaintext);

        // Assert
        ciphertext.Should().NotBeNullOrEmpty();
        Action act = () => Convert.FromBase64String(ciphertext);
        act.Should().NotThrow();
    }

    [Fact]
    public void Decrypt_WhenValidCiphertext_ReturnsOriginalPlaintext()
    {
        // Arrange
        var original = "Sieu bao bao ve nong dan";
        var encrypted = _sut.Encrypt(original);

        // Act
        var decrypted = _sut.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(original);
    }

    [Fact]
    public void Encrypt_ReturnsDifferentCiphertextForSamePlaintext()
    {
        // Arrange
        var plaintext = "Same Text";

        // Act
        var cipher1 = _sut.Encrypt(plaintext);
        var cipher2 = _sut.Encrypt(plaintext);

        // Assert
        cipher1.Should().NotBe(cipher2); // Due to random Nonce/IV
    }

    [Fact]
    public void Decrypt_WhenCiphertextIsTampered_ThrowsCryptographicException()
    {
        // Arrange
        var plaintext = "Secret Data";
        var ciphertextBase64 = _sut.Encrypt(plaintext);
        byte[] cipherBytes = Convert.FromBase64String(ciphertextBase64);
        
        // Flip a bit in the ciphertext part (after 12 bytes of nonce)
        cipherBytes[15] ^= 0xFF; 
        var tamperedCiphertext = Convert.ToBase64String(cipherBytes);

        // Act
        Action act = () => _sut.Decrypt(tamperedCiphertext);

        // Assert
        act.Should().Throw<CryptographicException>()
           .WithMessage("Decryption failed*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Encrypt_WhenNullOrEmpty_ReturnsSame(string? input)
    {
        // Act
        var result = _sut.Encrypt(input!);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void Constructor_WhenKeyIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        configuration["PII_ENCRYPTION_KEY"].Returns((string?)null);

        // Act
        Action act = () => new AesGcmPiiEncryptionService(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*not configured*");
    }

    [Fact]
    public void Constructor_WhenKeyIsInvalidLength_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = Substitute.For<IConfiguration>();
        configuration["PII_ENCRYPTION_KEY"].Returns("1234"); // Too short

        // Act
        Action act = () => new AesGcmPiiEncryptionService(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*32-byte hex string*");
    }
}
