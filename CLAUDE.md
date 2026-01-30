# Unity Docs Index Generator - Development Guidelines

## Coding Standards

### Precondition Validation

All functions with preconditions MUST:

1. **Check preconditions before execution** - Validate all required conditions at the start of the function
2. **Return a clear enum error** - If preconditions are not met, return a descriptive enum value
3. **Handle errors at call site** - Callers must handle all possible error cases appropriately

#### Example Pattern

```csharp
public enum GenerateResult
{
    Success,
    ErrorDocsPathNotFound,
    ErrorManualNotFound,
    ErrorScriptReferenceNotFound,
    ErrorOutputPathInvalid,
    ErrorFileWriteFailed
}

public static GenerateResult GenerateIndex(string docsPath, string outputPath)
{
    // 1. Check preconditions
    if (string.IsNullOrEmpty(docsPath))
        return GenerateResult.ErrorDocsPathNotFound;

    if (!Directory.Exists(docsPath))
        return GenerateResult.ErrorDocsPathNotFound;

    var manualPath = Path.Combine(docsPath, "Manual");
    if (!Directory.Exists(manualPath))
        return GenerateResult.ErrorManualNotFound;

    if (string.IsNullOrEmpty(outputPath))
        return GenerateResult.ErrorOutputPathInvalid;

    // 2. Execute main logic
    try
    {
        var index = BuildIndex(manualPath);
        File.WriteAllText(outputPath, index);
    }
    catch (IOException)
    {
        return GenerateResult.ErrorFileWriteFailed;
    }

    return GenerateResult.Success;
}
```

#### Caller Responsibility

```csharp
var result = GenerateIndex(docsPath, outputPath);

switch (result)
{
    case GenerateResult.Success:
        Debug.Log("Index generated successfully");
        break;
    case GenerateResult.ErrorDocsPathNotFound:
        ShowError(Localization.Get("errorDocsPathNotFound"));
        break;
    case GenerateResult.ErrorManualNotFound:
        ShowError(Localization.Get("errorManualNotFound"));
        break;
    case GenerateResult.ErrorOutputPathInvalid:
        ShowError(Localization.Get("errorOutputPathInvalid"));
        break;
    case GenerateResult.ErrorFileWriteFailed:
        ShowError(Localization.Get("errorFileWriteFailed"));
        break;
}
```

### Enum Naming Convention

- Prefix success case with nothing or `Success`
- Prefix error cases with `Error`
- Be specific about what failed: `ErrorManualNotFound` not `ErrorNotFound`
- Use PascalCase

### When to Use This Pattern

- Public API methods
- Methods that interact with file system
- Methods that depend on external state
- Methods called from UI handlers

### When NOT to Use This Pattern

- Private helper methods with guaranteed valid inputs
- Simple pure functions
- Methods where exceptions are more appropriate (e.g., programming errors)

### Error Messages with Actionable Suggestions

All error messages displayed to users MUST include:

1. **What went wrong** - Clear description of the error
2. **Why it happened** - Brief explanation of the cause (if known)
3. **What the user can do** - Actionable steps to resolve the issue

#### Error Message Format

```
[Error Description]: [Cause]
[Action to take]
```

#### Example Error Messages

**Bad (no action):**
```
Error: Network failure while downloading documentation
```

**Good (with action):**
```
Error: Network failure while downloading documentation
→ Check your internet connection and try again, or use a custom CDN URL
```

#### Implementation Pattern

```csharp
// Define error messages with actions in localization
{
    "errorNetworkFailure": "Error: Network failure while downloading documentation\n→ Check your internet connection and try again, or use a custom CDN URL",
    "errorSourceNotFound": "Error: Source directory not found\n→ Click 'Browse' to select a valid documentation folder",
    "errorInvalidVersion": "Error: Invalid Unity version\n→ Enter a valid version number (e.g., 6000.0)",
    "errorExtractionFailed": "Error: Failed to extract documentation archive\n→ Delete .unity-docs folder and try downloading again",
    "errorWriteFailed": "Error: Failed to write file\n→ Check file permissions and ensure the path is writable"
}
```

#### Action Types

Common actionable suggestions:

| Error Type | Suggested Actions |
|------------|-------------------|
| Network error | Check connection, use alternative CDN, retry |
| File not found | Browse to select correct path, verify path exists |
| Permission denied | Check file permissions, run as administrator |
| Invalid input | Show valid format/example, reset to default |
| Resource missing | Download/install required resource, provide link |
| Version mismatch | Update version, use compatible version |
