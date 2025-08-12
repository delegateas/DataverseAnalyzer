using System.Globalization;
using System.Resources;

namespace DataverseAnalyzer;

internal static class Resources
{
    private static readonly ResourceManager ResourceManager = new("DataverseAnalyzer.Resources", typeof(Resources).Assembly);

    internal static string CT0001_Title => GetString(nameof(CT0001_Title));

    internal static string CT0001_MessageFormat => GetString(nameof(CT0001_MessageFormat));

    internal static string CT0001_Description => GetString(nameof(CT0001_Description));

    internal static string CT0001_CodeFix_Title => GetString(nameof(CT0001_CodeFix_Title));

    private static string GetString(string name) => ResourceManager.GetString(name, CultureInfo.InvariantCulture) ?? name;
}