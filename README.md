# Dataverse Analyzer

A Roslyn analyzer for .NET Core projects that enforces specific control flow braces rules.

## Rule CT0001: Control Flow Braces Rule

This analyzer enforces that `if` and `else` control flow statements without braces can only contain:
- `return` statements
- `throw` statements  
- `continue` statements
- `break` statements
- `yield break` statements

### Examples

✅ **Allowed** (no braces needed):
```csharp
if (condition)
    return;

if (error)
    throw new Exception();

while (true)
    break;

foreach (var item in items)
    continue;

if (done)
    yield break;
```

❌ **Not allowed** (braces required):
```csharp
// This will trigger CT0001
if (condition)
    DoSomething();
```

## Usage

The analyzer is designed to be consumed as a project reference or NuGet package in other .NET projects. When integrated, it will automatically analyze your code and report violations of rule CT0001.

### Building

```bash
dotnet build src\DataverseAnalyzer\DataverseAnalyzer.csproj --configuration Release
```

The built analyzer DLL will be available in `src\DataverseAnalyzer\bin\Release\net8.0\DataverseAnalyzer.dll`.

## Integration

To use this analyzer in your projects, reference the built DLL as an analyzer:

```xml
<ItemGroup>
  <Analyzer Include="path\to\DataverseAnalyzer.dll" />
</ItemGroup>
```

The analyzer includes an automatic code fix provider that can add braces when violations are detected.