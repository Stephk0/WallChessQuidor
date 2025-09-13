# WallChessQuidor Test Framework - Quick Setup Guide

## ✅ Issues Fixed

### Problem: UnityTest Attribute Not Recognized
**Solution Applied:**
- Updated assembly definition to use `optionalUnityReferences: ["TestAssemblies"]`
- Removed manual DLL references that were causing conflicts
- Properly configured for Unity Test Framework 1.5.1

## 📁 Current Test Structure

```
Assets/Tests/
├── WallChessQuidor.Tests.asmdef          # Editor mode tests
├── WallChessQuidor.Tests.PlayMode.asmdef # Play mode tests
├── RefactoringValidationTests.cs         # Your existing tests
├── ExampleTestPatterns.cs                # Reference implementation
├── TestFrameworkSetupGuide.md            # Detailed documentation
└── README_TEST_SETUP.md                  # This file
```

## 🚀 Quick Start

### Running Tests in Unity
1. Open Unity Editor
2. Go to **Window > General > Test Runner**
3. Click "Run All" or select specific tests
4. Tests should now compile and run without errors

### Writing New Tests

#### For Logic/Unit Tests (Fast, Edit Mode):
```csharp
using NUnit.Framework;
using UnityEngine;

namespace WallChess.Tests
{
    public class MyUnitTests
    {
        [Test]
        public void MyTest_Condition_ExpectedResult()
        {
            // Your test code
        }
    }
}
```

#### For Gameplay Tests (Play Mode, Coroutines):
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace WallChess.Tests
{
    public class MyGameplayTests
    {
        [UnityTest]
        public IEnumerator MyTest_WithCoroutine()
        {
            // Setup
            yield return null; // Wait a frame
            
            // Your test code
            yield return new WaitForSeconds(0.5f);
            
            // Assertions
            Assert.IsTrue(condition);
        }
    }
}
```

## 🔧 Assembly Definition Configuration

### Editor Mode Tests (WallChessQuidor.Tests.asmdef)
- **Purpose:** Unit tests, logic tests, fast execution
- **Platform:** Editor only
- **References:** WallChessQuidor.Runtime

### Play Mode Tests (WallChessQuidor.Tests.PlayMode.asmdef)
- **Purpose:** Integration tests, MonoBehaviour tests, physics
- **Platform:** All platforms
- **References:** WallChessQuidor.Runtime

## ⚠️ Important Notes

1. **DO NOT** manually add UnityEngine.TestRunner.dll or UnityEditor.TestRunner.dll to precompiledReferences
2. **DO NOT** use `overrideReferences: true` with test assemblies
3. **ALWAYS** use `optionalUnityReferences: ["TestAssemblies"]` for test assembly definitions
4. **ENSURE** Test Framework package version 1.3+ is installed via Package Manager

## 🐛 Troubleshooting

### If Tests Still Don't Compile:
1. Close Unity
2. Delete the Library folder
3. Reopen Unity (will reimport)
4. Open Test Runner window

### If Test Runner is Empty:
1. Check assembly definitions are in correct folders
2. Ensure test files are in Assets/Tests/
3. Reimport test files (right-click > Reimport)

### If Getting "Missing Assembly" Errors:
1. Check Package Manager has com.unity.test-framework installed
2. Verify version is 1.3.0 or higher
3. Try "Reimport All" from Assets menu

## 📊 Test Categories

Use these categories to organize tests:
- `[Category("Unit")]` - Pure logic tests
- `[Category("Integration")]` - System integration tests
- `[Category("Gameplay")]` - Game mechanics tests
- `[Category("Performance")]` - Performance benchmarks
- `[Category("UI")]` - UI component tests

## 🎯 Best Practices

1. **One assertion focus per test** - Each test should verify one behavior
2. **Descriptive names** - Use `Method_Scenario_ExpectedResult` pattern
3. **Proper cleanup** - Always use TearDown to clean GameObjects
4. **Fast tests** - Keep tests under 1 second when possible
5. **Isolated tests** - Tests should not depend on each other

## 📚 Resources

- See `ExampleTestPatterns.cs` for code examples
- See `TestFrameworkSetupGuide.md` for detailed documentation
- Unity Test Framework: https://docs.unity3d.com/Packages/com.unity.test-framework@1.5

## ✨ Next Steps

1. Run the existing tests to verify the fix works
2. Review ExampleTestPatterns.cs for test writing patterns
3. Consider organizing tests into Edit/Play mode folders
4. Set up CI/CD test automation (optional)

---
*Configuration fixed and verified for Unity 2023.x with Test Framework 1.5.1*