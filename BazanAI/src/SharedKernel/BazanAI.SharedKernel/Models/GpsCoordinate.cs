
using System.Collections.Generic;
using BazanAI.SharedKernel.Domain;

namespace BazanAI.SharedKernel.Models;

public class GpsCoordinate : ValueObject
{
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public GpsCoordinate(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90) throw new ArgumentException("Invalid latitude");
        if (longitude < -180 || longitude > 180) throw new ArgumentException("Invalid longitude");

        Latitude = latitude;
        Longitude = longitude;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }
}
