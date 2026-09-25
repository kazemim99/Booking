namespace AsanRezerve.ServiceCatalog.Domain.Enums;

/// <summary>Where a push token came from. Decides which gateway payload shape is used.</summary>
public enum DevicePlatform
{
    Unknown = 0,
    Android = 1,
    Ios = 2,
    Web = 3,
}
