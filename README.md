# Dataverse Analyzer

A Roslyn analyzer for .NET Core projects that enforces specific coding standards and best practices.

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

## Rule CT0002: Enum Assignment Rule

This analyzer prevents assigning literal values to enum properties, requiring the use of proper enum values instead.

### Examples

✅ **Allowed**:
```csharp
AccountCategoryCode = AccountCategoryCode.Standard;
```

❌ **Not allowed**:
```csharp
// This will trigger CT0002
AccountCategoryCode = 1;
```

## Rule CT0003: Object Initialization Rule

This analyzer prevents the use of empty parentheses in object initialization when using object initializers.

### Examples

✅ **Allowed**:
```csharp
var account = new Account
{
    Name = "MoneyMan",
};

var account = new Account(accountId)
{
    Name = "MoneyMan",
};
```

❌ **Not allowed**:
```csharp
// This will trigger CT0003
var account = new Account()
{
    Name = "MoneyMan",
};
```

## Usage

The analyzer is designed to be consumed as a project reference or NuGet package in other .NET projects. When integrated, it will automatically analyze your code and report violations of the rules.

### Building

```bash
dotnet build src\DataverseAnalyzer\DataverseAnalyzer.csproj --configuration Release
```

The built analyzer DLL will be available in `src\DataverseAnalyzer\bin\Release\netstandard2.0\DataverseAnalyzer.dll`.

## Integration

To use this analyzer in your projects, reference the built DLL as an analyzer:

```xml
<ItemGroup>
  <Analyzer Include="path\to\DataverseAnalyzer.dll" />
</ItemGroup>
```

The analyzer includes an automatic code fix provider that can add braces when violations are detected.