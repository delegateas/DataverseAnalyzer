# Dataverse Analyzer

A Roslyn analyzer enforcing coding standards for Dataverse/.NET projects.

## Rules Overview

| ID | Rule | Category | Severity |
|----|------|----------|----------|
| CT0001 | Braceless control flow must be return/throw/continue/break/yield break | Style | Error |
| CT0002 | Enum properties should not be assigned literal values | Usage | Warning |
| CT0003 | Remove empty parentheses from object initialization | Style | Error |
| CT0004 | Remove braces from single control flow statements | Style | Error |
| CT0005 | Constructor has duplicate DI parameter types | Usage | Warning |
| CT0006 | Plugin class missing XML documentation | Documentation | Warning |
| CT0007 | Use ContainsAttributes instead of Contains on Entity | Usage | Warning |
| CT0008 | AddFilteredAttributes not allowed on Create | Usage | Error |
| CT0009 | PreImage not allowed on Create | Usage | Error |
| CT0010 | PostImage not allowed on Delete | Usage | Error |

## Style Rules

### CT0001: Braceless Control Flow

Control flow statements without braces can only contain: `return`, `throw`, `continue`, `break`, or `yield break`.

```csharp
// Allowed
if (condition)
    return;

// Not allowed - triggers CT0001
if (condition)
    DoSomething();
```

### CT0003: Object Initialization Parentheses

Remove empty parentheses when using object initializers.

```csharp
// Allowed
var account = new Account { Name = "Test" };

// Not allowed - triggers CT0003
var account = new Account() { Name = "Test" };
```

### CT0004: Braced Control Flow

Remove braces when a block contains only a single control flow statement.

```csharp
// Allowed
if (condition)
    return;

// Not allowed - triggers CT0004
if (condition)
{
    return;
}
```

## Usage Rules

### CT0002: Enum Assignment

Enum properties should use enum values, not literals.

```csharp
// Allowed
AccountCategoryCode = AccountCategoryCode.Standard;

// Not allowed - triggers CT0002
AccountCategoryCode = 1;
```

### CT0005: Duplicate DI Parameters

Constructors shouldn't have multiple parameters of the same DI type (Service, Repository, Handler, Provider, Factory, Manager, Client).

```csharp
// Not allowed - triggers CT0005
public MyClass(IUserService userService, IUserService adminService) { }
```

### CT0007: Entity Contains

Use type-safe `ContainsAttributes` instead of string-based `Contains` on Entity types.

```csharp
// Allowed
if (account.ContainsAttributes(x => x.Name))

// Not allowed - triggers CT0007
if (account.Contains("name"))
```

### CT0008-CT0010: Plugin Step Configuration

These rules prevent invalid plugin step configurations in Dataverse:

| Rule | Restriction | Reason |
|------|-------------|--------|
| CT0008 | No `AddFilteredAttributes` on Create | Filtered attributes have no effect on Create |
| CT0009 | No PreImage on Create | No previous record state exists |
| CT0010 | No PostImage on Delete | No record state exists after deletion |

```csharp
// Not allowed - triggers CT0008
RegisterPluginStep<Account>(EventOperation.Create)
    .AddFilteredAttributes(a => a.Name);

// Not allowed - triggers CT0009
RegisterPluginStep<Account>(EventOperation.Create)
    .AddImage(ImageType.PreImage);

// Not allowed - triggers CT0010
RegisterPluginStep<Account>(EventOperation.Delete)
    .AddImage(ImageType.PostImage);
```

## Documentation Rules

### CT0006: Plugin Documentation

Classes implementing `IPlugin` must have XML documentation.

```csharp
// Allowed
/// <summary>Updates account name on create.</summary>
public class UpdateAccountPlugin : IPlugin { }

// Not allowed - triggers CT0006
public class UpdateAccountPlugin : IPlugin { }
```

## Usage

### Building

```bash
dotnet build src\DataverseAnalyzer\DataverseAnalyzer.csproj --configuration Release
```

### Integration

Reference the analyzer in your project:

```xml
<ItemGroup>
  <Analyzer Include="path\to\DataverseAnalyzer.dll" />
</ItemGroup>
```
