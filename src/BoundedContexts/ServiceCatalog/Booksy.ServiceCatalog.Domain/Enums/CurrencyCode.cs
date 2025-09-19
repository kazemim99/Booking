using System.ComponentModel;

namespace Booksy.ServiceCatalog.Domain.Enums;

/// <summary>
/// ISO 4217 currency codes for multi-currency support
/// </summary>
public enum CurrencyCode
{
    /// <summary>
    /// United States Dollar
    /// </summary>
    [Description("USD")]
    USD = 840,

    /// <summary>
    /// Euro
    /// </summary>
    [Description("EUR")]
    EUR = 978,

    /// <summary>
    /// British Pound Sterling
    /// </summary>
    [Description("GBP")]
    GBP = 826,

    /// <summary>
    /// Japanese Yen
    /// </summary>
    [Description("JPY")]
    JPY = 392,

    /// <summary>
    /// Canadian Dollar
    /// </summary>
    [Description("CAD")]
    CAD = 124,

    /// <summary>
    /// Australian Dollar
    /// </summary>
    [Description("AUD")]
    AUD = 36,

    /// <summary>
    /// Swiss Franc
    /// </summary>
    [Description("CHF")]
    CHF = 756,

    /// <summary>
    /// Chinese Yuan Renminbi
    /// </summary>
    [Description("CNY")]
    CNY = 156,

    /// <summary>
    /// Swedish Krona
    /// </summary>
    [Description("SEK")]
    SEK = 752,

    /// <summary>
    /// New Zealand Dollar
    /// </summary>
    [Description("NZD")]
    NZD = 554,

    /// <summary>
    /// Mexican Peso
    /// </summary>
    [Description("MXN")]
    MXN = 484,

    /// <summary>
    /// Singapore Dollar
    /// </summary>
    [Description("SGD")]
    SGD = 702,

    /// <summary>
    /// Hong Kong Dollar
    /// </summary>
    [Description("HKD")]
    HKD = 344,

    /// <summary>
    /// Norwegian Krone
    /// </summary>
    [Description("NOK")]
    NOK = 578,

    /// <summary>
    /// South Korean Won
    /// </summary>
    [Description("KRW")]
    KRW = 410,

    /// <summary>
    /// Turkish Lira
    /// </summary>
    [Description("TRY")]
    TRY = 949,

    /// <summary>
    /// Russian Ruble
    /// </summary>
    [Description("RUB")]
    RUB = 643,

    /// <summary>
    /// Indian Rupee
    /// </summary>
    [Description("INR")]
    INR = 356,

    /// <summary>
    /// Brazilian Real
    /// </summary>
    [Description("BRL")]
    BRL = 986,

    /// <summary>
    /// South African Rand
    /// </summary>
    [Description("ZAR")]
    ZAR = 710,

    /// <summary>
    /// Polish Zloty
    /// </summary>
    [Description("PLN")]
    PLN = 985,

    /// <summary>
    /// Danish Krone
    /// </summary>
    [Description("DKK")]
    DKK = 208,

    /// <summary>
    /// Czech Koruna
    /// </summary>
    [Description("CZK")]
    CZK = 203,

    /// <summary>
    /// Hungarian Forint
    /// </summary>
    [Description("HUF")]
    HUF = 348,

    /// <summary>
    /// Israeli New Shekel
    /// </summary>
    [Description("ILS")]
    ILS = 376,

    /// <summary>
    /// Chilean Peso
    /// </summary>
    [Description("CLP")]
    CLP = 152,

    /// <summary>
    /// Philippine Peso
    /// </summary>
    [Description("PHP")]
    PHP = 608,

    /// <summary>
    /// United Arab Emirates Dirham
    /// </summary>
    [Description("AED")]
    AED = 784,

    /// <summary>
    /// Colombian Peso
    /// </summary>
    [Description("COP")]
    COP = 170,

    /// <summary>
    /// Saudi Riyal
    /// </summary>
    [Description("SAR")]
    SAR = 682,

    /// <summary>
    /// Malaysian Ringgit
    /// </summary>
    [Description("MYR")]
    MYR = 458,

    /// <summary>
    /// Romanian Leu
    /// </summary>
    [Description("RON")]
    RON = 946,

    /// <summary>
    /// Croatian Kuna
    /// </summary>
    [Description("HRK")]
    HRK = 191,

    /// <summary>
    /// Bulgarian Lev
    /// </summary>
    [Description("BGN")]
    BGN = 975,

    /// <summary>
    /// Thai Baht
    /// </summary>
    [Description("THB")]
    THB = 764,

    /// <summary>
    /// Indonesian Rupiah
    /// </summary>
    [Description("IDR")]
    IDR = 360,

    /// <summary>
    /// Ukrainian Hryvnia
    /// </summary>
    [Description("UAH")]
    UAH = 980,

    /// <summary>
    /// Argentine Peso
    /// </summary>
    [Description("ARS")]
    ARS = 32,

    /// <summary>
    /// Egyptian Pound
    /// </summary>
    [Description("EGP")]
    EGP = 818,

    /// <summary>
    /// Kenyan Shilling
    /// </summary>
    [Description("KES")]
    KES = 404,

    /// <summary>
    /// Nigerian Naira
    /// </summary>
    [Description("NGN")]
    NGN = 566,

    /// <summary>
    /// Moroccan Dirham
    /// </summary>
    [Description("MAD")]
    MAD = 504,

    /// <summary>
    /// Peruvian Sol
    /// </summary>
    [Description("PEN")]
    PEN = 604,

    /// <summary>
    /// Vietnamese Dong
    /// </summary>
    [Description("VND")]
    VND = 704,

    /// <summary>
    /// Bangladeshi Taka
    /// </summary>
    [Description("BDT")]
    BDT = 50,

    /// <summary>
    /// Pakistani Rupee
    /// </summary>
    [Description("PKR")]
    PKR = 586,

    /// <summary>
    /// Sri Lankan Rupee
    /// </summary>
    [Description("LKR")]
    LKR = 144,

    /// <summary>
    /// Nepalese Rupee
    /// </summary>
    [Description("NPR")]
    NPR = 524,

    /// <summary>
    /// Myanmar Kyat
    /// </summary>
    [Description("MMK")]
    MMK = 104,

    /// <summary>
    /// Cambodian Riel
    /// </summary>
    [Description("KHR")]
    KHR = 116,

    /// <summary>
    /// Laotian Kip
    /// </summary>
    [Description("LAK")]
    LAK = 418,

    /// <summary>
    /// Mongolian Tugrik
    /// </summary>
    [Description("MNT")]
    MNT = 496,

    /// <summary>
    /// Kazakhstani Tenge
    /// </summary>
    [Description("KZT")]
    KZT = 398,

    /// <summary>
    /// Uzbekistani Som
    /// </summary>
    [Description("UZS")]
    UZS = 860,

    /// <summary>
    /// Georgian Lari
    /// </summary>
    [Description("GEL")]
    GEL = 981,

    /// <summary>
    /// Armenian Dram
    /// </summary>
    [Description("AMD")]
    AMD = 51,

    /// <summary>
    /// Azerbaijani Manat
    /// </summary>
    [Description("AZN")]
    AZN = 944,


    /// <summary>
    /// Iran Rial
    /// </summary>
    [Description("IRR")]

    IRR = 987
}

/// <summary>
/// Extension methods for CurrencyCode enum
/// </summary>
public static class CurrencyCodeExtensions
{
    private static readonly Dictionary<CurrencyCode, CurrencyInfo> CurrencyData = new()
    {
        { CurrencyCode.USD, new("$", "United States Dollar", 2) },
        { CurrencyCode.EUR, new("€", "Euro", 2) },
        { CurrencyCode.GBP, new("£", "British Pound Sterling", 2) },
        { CurrencyCode.JPY, new("¥", "Japanese Yen", 0) },
        { CurrencyCode.CAD, new("C$", "Canadian Dollar", 2) },
        { CurrencyCode.AUD, new("A$", "Australian Dollar", 2) },
        { CurrencyCode.CHF, new("CHF", "Swiss Franc", 2) },
        { CurrencyCode.CNY, new("¥", "Chinese Yuan", 2) },
        { CurrencyCode.SEK, new("kr", "Swedish Krona", 2) },
        { CurrencyCode.NZD, new("NZ$", "New Zealand Dollar", 2) },
        { CurrencyCode.MXN, new("$", "Mexican Peso", 2) },
        { CurrencyCode.SGD, new("S$", "Singapore Dollar", 2) },
        { CurrencyCode.HKD, new("HK$", "Hong Kong Dollar", 2) },
        { CurrencyCode.NOK, new("kr", "Norwegian Krone", 2) },
        { CurrencyCode.KRW, new("₩", "South Korean Won", 0) },
        { CurrencyCode.TRY, new("₺", "Turkish Lira", 2) },
        { CurrencyCode.RUB, new("₽", "Russian Ruble", 2) },
        { CurrencyCode.INR, new("₹", "Indian Rupee", 2) },
        { CurrencyCode.BRL, new("R$", "Brazilian Real", 2) },
        { CurrencyCode.ZAR, new("R", "South African Rand", 2) },
        { CurrencyCode.PLN, new("zł", "Polish Zloty", 2) },
        { CurrencyCode.DKK, new("kr", "Danish Krone", 2) },
        { CurrencyCode.CZK, new("Kč", "Czech Koruna", 2) },
        { CurrencyCode.HUF, new("Ft", "Hungarian Forint", 2) },
        { CurrencyCode.ILS, new("₪", "Israeli New Shekel", 2) },
        { CurrencyCode.CLP, new("$", "Chilean Peso", 0) },
        { CurrencyCode.PHP, new("₱", "Philippine Peso", 2) },
        { CurrencyCode.AED, new("د.إ", "UAE Dirham", 2) },
        { CurrencyCode.COP, new("$", "Colombian Peso", 2) },
        { CurrencyCode.SAR, new("﷼", "Saudi Riyal", 2) },
        { CurrencyCode.MYR, new("RM", "Malaysian Ringgit", 2) },
        { CurrencyCode.RON, new("lei", "Romanian Leu", 2) },
        { CurrencyCode.HRK, new("kn", "Croatian Kuna", 2) },
        { CurrencyCode.BGN, new("лв", "Bulgarian Lev", 2) },
        { CurrencyCode.THB, new("฿", "Thai Baht", 2) },
        { CurrencyCode.IDR, new("Rp", "Indonesian Rupiah", 2) },
        { CurrencyCode.UAH, new("₴", "Ukrainian Hryvnia", 2) },
        { CurrencyCode.ARS, new("$", "Argentine Peso", 2) },
        { CurrencyCode.EGP, new("£", "Egyptian Pound", 2) },
        { CurrencyCode.KES, new("KSh", "Kenyan Shilling", 2) },
        { CurrencyCode.NGN, new("₦", "Nigerian Naira", 2) },
        { CurrencyCode.MAD, new("د.م.", "Moroccan Dirham", 2) },
        { CurrencyCode.PEN, new("S/", "Peruvian Sol", 2) },
        { CurrencyCode.VND, new("₫", "Vietnamese Dong", 0) },
        { CurrencyCode.BDT, new("৳", "Bangladeshi Taka", 2) },
        { CurrencyCode.PKR, new("₨", "Pakistani Rupee", 2) },
        { CurrencyCode.LKR, new("₨", "Sri Lankan Rupee", 2) },
        { CurrencyCode.NPR, new("₨", "Nepalese Rupee", 2) },
        { CurrencyCode.MMK, new("K", "Myanmar Kyat", 2) },
        { CurrencyCode.KHR, new("៛", "Cambodian Riel", 2) },
        { CurrencyCode.LAK, new("₭", "Laotian Kip", 2) },
        { CurrencyCode.MNT, new("₮", "Mongolian Tugrik", 2) },
        { CurrencyCode.KZT, new("₸", "Kazakhstani Tenge", 2) },
        { CurrencyCode.UZS, new("so'm", "Uzbekistani Som", 2) },
        { CurrencyCode.GEL, new("₾", "Georgian Lari", 2) },
        { CurrencyCode.AMD, new("֏", "Armenian Dram", 2) },
        { CurrencyCode.AZN, new("₼", "Azerbaijani Manat", 2) }
    };

    /// <summary>
    /// Get the ISO 4217 three-letter currency code
    /// </summary>
    public static string GetCode(this CurrencyCode currency)
    {
        return currency.ToString();
    }

    /// <summary>
    /// Get the currency symbol
    /// </summary>
    public static string GetSymbol(this CurrencyCode currency)
    {
        return CurrencyData.TryGetValue(currency, out var info) ? info.Symbol : currency.ToString();
    }

    /// <summary>
    /// Get the full currency name
    /// </summary>
    public static string GetName(this CurrencyCode currency)
    {
        return CurrencyData.TryGetValue(currency, out var info) ? info.Name : currency.ToString();
    }

    /// <summary>
    /// Get the number of decimal places typically used for this currency
    /// </summary>
    public static int GetDecimalPlaces(this CurrencyCode currency)
    {
        return CurrencyData.TryGetValue(currency, out var info) ? info.DecimalPlaces : 2;
    }

    /// <summary>
    /// Get the ISO 4217 numeric code
    /// </summary>
    public static int GetNumericCode(this CurrencyCode currency)
    {
        return (int)currency;
    }

    /// <summary>
    /// Format an amount with the appropriate currency symbol and decimal places
    /// </summary>
    public static string FormatAmount(this CurrencyCode currency, decimal amount)
    {
        var symbol = currency.GetSymbol();
        var decimalPlaces = currency.GetDecimalPlaces();
        var formattedAmount = amount.ToString($"F{decimalPlaces}");

        return currency switch
        {
            // Currencies where symbol comes after the amount
            CurrencyCode.SEK or CurrencyCode.NOK or CurrencyCode.DKK or
            CurrencyCode.PLN or CurrencyCode.CZK or CurrencyCode.HUF or
            CurrencyCode.HRK or CurrencyCode.BGN or CurrencyCode.RON or
            CurrencyCode.UZS or CurrencyCode.AZN
                => $"{formattedAmount} {symbol}",

            // Currencies with symbol at the end with space
            CurrencyCode.AED or CurrencyCode.MAD
                => $"{formattedAmount} {symbol}",

            // Default: symbol before amount
            _ => $"{symbol}{formattedAmount}"
        };
    }

    /// <summary>
    /// Check if currency supports fractional units (cents, pence, etc.)
    /// </summary>
    public static bool SupportsFractionalUnits(this CurrencyCode currency)
    {
        return currency.GetDecimalPlaces() > 0;
    }

    /// <summary>
    /// Get currencies commonly used in a specific region
    /// </summary>
    public static IEnumerable<CurrencyCode> GetRegionalCurrencies(CurrencyRegion region)
    {
        return region switch
        {
            CurrencyRegion.NorthAmerica => new[] { CurrencyCode.USD, CurrencyCode.CAD, CurrencyCode.MXN },
            CurrencyRegion.Europe => new[] { CurrencyCode.EUR, CurrencyCode.GBP, CurrencyCode.CHF, CurrencyCode.SEK, CurrencyCode.NOK, CurrencyCode.DKK, CurrencyCode.PLN, CurrencyCode.CZK, CurrencyCode.HUF, CurrencyCode.RON, CurrencyCode.HRK, CurrencyCode.BGN },
            CurrencyRegion.Asia => new[] { CurrencyCode.JPY, CurrencyCode.CNY, CurrencyCode.KRW, CurrencyCode.INR, CurrencyCode.SGD, CurrencyCode.HKD, CurrencyCode.THB, CurrencyCode.MYR, CurrencyCode.IDR, CurrencyCode.PHP, CurrencyCode.VND, CurrencyCode.BDT, CurrencyCode.PKR, CurrencyCode.LKR, CurrencyCode.NPR, CurrencyCode.MMK, CurrencyCode.KHR, CurrencyCode.LAK, CurrencyCode.MNT },
            CurrencyRegion.Oceania => new[] { CurrencyCode.AUD, CurrencyCode.NZD },
            CurrencyRegion.MiddleEast => new[] { CurrencyCode.AED, CurrencyCode.SAR, CurrencyCode.ILS, CurrencyCode.TRY },
            CurrencyRegion.SouthAmerica => new[] { CurrencyCode.BRL, CurrencyCode.CLP, CurrencyCode.COP, CurrencyCode.ARS, CurrencyCode.PEN },
            CurrencyRegion.Africa => new[] { CurrencyCode.ZAR, CurrencyCode.EGP, CurrencyCode.KES, CurrencyCode.NGN, CurrencyCode.MAD },
            CurrencyRegion.CentralAsia => new[] { CurrencyCode.KZT, CurrencyCode.UZS, CurrencyCode.MNT },
            CurrencyRegion.Caucasus => new[] { CurrencyCode.GEL, CurrencyCode.AMD, CurrencyCode.AZN },
            _ => Array.Empty<CurrencyCode>()
        };
    }

    /// <summary>
    /// Check if this is a major trading currency
    /// </summary>
    public static bool IsMajorCurrency(this CurrencyCode currency)
    {
        var majorCurrencies = new[]
        {
            CurrencyCode.USD, CurrencyCode.EUR, CurrencyCode.JPY, CurrencyCode.GBP,
            CurrencyCode.CHF, CurrencyCode.CAD, CurrencyCode.AUD, CurrencyCode.CNY
        };

        return majorCurrencies.Contains(currency);
    }

    /// <summary>
    /// Check if currency is commonly used in international trade
    /// </summary>
    public static bool IsInternationalTradeCurrency(this CurrencyCode currency)
    {
        var tradeCurrencies = new[]
        {
            CurrencyCode.USD, CurrencyCode.EUR, CurrencyCode.GBP, CurrencyCode.JPY,
            CurrencyCode.CHF, CurrencyCode.CAD, CurrencyCode.AUD, CurrencyCode.CNY,
            CurrencyCode.HKD, CurrencyCode.SGD, CurrencyCode.SEK, CurrencyCode.NOK
        };

        return tradeCurrencies.Contains(currency);
    }

    /// <summary>
    /// Parse currency code from string
    /// </summary>
    public static CurrencyCode? ParseFromString(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return null;

        if (Enum.TryParse<CurrencyCode>(currencyCode.ToUpperInvariant(), out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Get typical exchange rate volatility level
    /// </summary>
    public static VolatilityLevel GetVolatilityLevel(this CurrencyCode currency)
    {
        return currency switch
        {
            CurrencyCode.USD or CurrencyCode.EUR or CurrencyCode.JPY or CurrencyCode.CHF => VolatilityLevel.Low,
            CurrencyCode.GBP or CurrencyCode.CAD or CurrencyCode.AUD or CurrencyCode.SEK or CurrencyCode.NOK => VolatilityLevel.Medium,
            CurrencyCode.TRY or CurrencyCode.ARS or CurrencyCode.RUB or CurrencyCode.UAH => VolatilityLevel.High,
            _ => VolatilityLevel.Medium
        };
    }

    /// <summary>
    /// Check if currency requires special handling for payments
    /// </summary>
    public static bool RequiresSpecialHandling(this CurrencyCode currency)
    {
        var specialHandlingCurrencies = new[]
        {
            CurrencyCode.RUB, CurrencyCode.UAH, CurrencyCode.TRY, // Political/economic restrictions
            CurrencyCode.IRR, // If Iranian Rial were added
            CurrencyCode.VND, CurrencyCode.IDR, // Large denomination numbers
            CurrencyCode.JPY, CurrencyCode.KRW  // No decimal places
        };

        return specialHandlingCurrencies.Contains(currency);
    }

    private record CurrencyInfo(string Symbol, string Name, int DecimalPlaces);
}

/// <summary>
/// Geographic regions for currency grouping
/// </summary>
public enum CurrencyRegion
{
    NorthAmerica,
    SouthAmerica,
    Europe,
    Asia,
    Oceania,
    MiddleEast,
    Africa,
    CentralAsia,
    Caucasus
}

/// <summary>
/// Currency volatility levels
/// </summary>
public enum VolatilityLevel
{
    Low,
    Medium,
    High
}