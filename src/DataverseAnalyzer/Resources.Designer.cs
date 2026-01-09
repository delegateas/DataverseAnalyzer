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

    internal static string CT0002_Title => GetString(nameof(CT0002_Title));

    internal static string CT0002_MessageFormat => GetString(nameof(CT0002_MessageFormat));

    internal static string CT0002_Description => GetString(nameof(CT0002_Description));

    internal static string CT0003_Title => GetString(nameof(CT0003_Title));

    internal static string CT0003_MessageFormat => GetString(nameof(CT0003_MessageFormat));

    internal static string CT0003_Description => GetString(nameof(CT0003_Description));

    internal static string CT0004_Title => GetString(nameof(CT0004_Title));

    internal static string CT0004_MessageFormat => GetString(nameof(CT0004_MessageFormat));

    internal static string CT0004_Description => GetString(nameof(CT0004_Description));

    internal static string CT0004_CodeFix_Title => GetString(nameof(CT0004_CodeFix_Title));

    internal static string CT0005_Title => GetString(nameof(CT0005_Title));

    internal static string CT0005_MessageFormat => GetString(nameof(CT0005_MessageFormat));

    internal static string CT0005_Description => GetString(nameof(CT0005_Description));

    internal static string CT0006_Title => GetString(nameof(CT0006_Title));

    internal static string CT0006_MessageFormat => GetString(nameof(CT0006_MessageFormat));

    internal static string CT0006_Description => GetString(nameof(CT0006_Description));

    internal static string CT0007_Title => GetString(nameof(CT0007_Title));

    internal static string CT0007_MessageFormat => GetString(nameof(CT0007_MessageFormat));

    internal static string CT0007_Description => GetString(nameof(CT0007_Description));

    private static string GetString(string name) => ResourceManager.GetString(name, CultureInfo.InvariantCulture) ?? name;
}