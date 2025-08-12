using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Analyzer project does not require XML documentation")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "Not required for analyzer project")]
[assembly: SuppressMessage("Performance", "S1172:Unused method parameters should be removed", Justification = "CancellationToken required by method signature")]
[assembly: SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not applicable for new analyzer project")]
[assembly: SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2000:Add analyzer diagnostic to analyzer release", Justification = "Not applicable for new analyzer project")]