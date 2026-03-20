using System;

namespace BazanAI.Identity.Domain.Entities;

public record ConsentRecord(
    bool Accepted,
    DateTime AcceptedAt,
    string PolicyVersion,
    string AcceptedFromIp
);
