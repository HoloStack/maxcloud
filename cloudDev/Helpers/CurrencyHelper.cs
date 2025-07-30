using System.Globalization;

namespace cloudDev.Helpers;

public static class CurrencyHelper
{
    public static string FormatAsRand(decimal amount)
    {
        return $"R {amount.ToString("F2", CultureInfo.InvariantCulture)}";
    }
    
    public static string FormatAsRand(float amount)
    {
        return $"R{amount:F2}";
    }
    
    public static string FormatAsRand(double amount)
    {
        return $"R{amount:F2}";
    }
}
