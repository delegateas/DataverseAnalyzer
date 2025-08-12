---
description: Main entry point for AI-based development and developer reference
globs: 
alwaysApply: true
---

# 🚨 CRITICAL: Read AI Rules First

**BEFORE making ANY code changes, you MUST read and follow the rules in the `.ai_rules` folder.**

## Essential Rule Files

**⚠️ MANDATORY READING**: These files contain critical development patterns and standards

## Build & Test Commands

```bash
# Build solution (REQUIRED before submitting)
dotnet build --configuration Release

# Run tests (REQUIRED after any changes)
dotnet test --configuration Release
```

## Critical Reminders

- **Rule violations** = code gets reverted
- **Missing build/test** = changes get rejected
- Never skip the build/test validation

**⚠️ WARNING**: Code that doesn't follow these rules gets reverted. The rules contain critical architectural decisions, naming conventions, and patterns that ensure code quality and consistency across the entire solution.

**Read the rules. Follow the rules. Test your changes.**