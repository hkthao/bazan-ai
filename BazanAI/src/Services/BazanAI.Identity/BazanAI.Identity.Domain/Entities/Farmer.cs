
using System;
using System.Collections.Generic;
using BazanAI.SharedKernel.Domain;

namespace BazanAI.Identity.Domain.Entities;

public class Farmer : AggregateRoot
{
    public string PhoneNumber { get; private set; }
    public string FullName { get; private set; }
    public string? ZaloId { get; private set; }
    public string PreferredLanguage { get; private set; } = "vi-VN";

    private readonly List<string> _farmIds = new();
    public IReadOnlyCollection<string> FarmIds => _farmIds.AsReadOnly();

    public Farmer(string phoneNumber, string fullName)
    {
        PhoneNumber = phoneNumber;
        FullName = fullName;
    }

    public void AddFarm(string farmId)
    {
        if (!_farmIds.Contains(farmId))
        {
            _farmIds.Add(farmId);
        }
    }
}
