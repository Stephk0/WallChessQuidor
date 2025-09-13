# Unity Test Framework Setup Guide for WallChessQuidor

## Fixed Issues
1. **UnityTest Attribute Recognition**: Fixed by using `optionalUnityReferences: ["TestAssemblies"]` in assembly definitions
2. **Assembly References**: Removed manual test runner references, using Unity's built-in test assembly system
3. **Namespace Issues**: Properly configured rootNamespace for test organization

## Assembly Definition Structure

### Editor Mode Tests (WallChessQuidor.Tests.asmdef)
- For tests that run in Edit Mode
- Fast execution, no scene loading
- Best for logic testing, utility functions, data structures

### Play Mode Tests (WallChessQuidor.Tests.PlayMode.asmdef)
- For tests that require Unity runtime
- Can test MonoBehaviours, coroutines, physics
- Slower but more comprehensive

## Test Organization Best Practices

### 1. Directory Structure
```
Assets/
├── Tests/
│   ├── Editor/           # Edit mode tests
│   │   ├── Unit/        # Unit tests
│   │   └── Integration/ # Integration tests
│   ├── PlayMode/        # Play mode tests
│   │   ├── Gameplay/    # Gameplay tests
│   │   └── Performance/ # Performance tests
│   └── Shared/          # Shared test utilities
```

### 2. Naming Conventions
- Test classes: `[SystemName]Tests.cs`
- Test methods: `[MethodName]_[Scenario]_[ExpectedResult]()`
- Example: `MovePawn_ValidMove_ReturnsSuccess()`

### 3. Test Categories
Use NUnit categories to organize tests:
```csharp
[Category("Unit")]
[Category("Gameplay")]
[Category("Performance")]
```

### 4. Common Attributes

#### For Edit Mode Tests:
```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class MyEditModeTests
{
    [Test]
    public void SimpleTest() { }
    
    [UnityTest]
    public IEnumerator CoroutineTest() 
    {
        yield return null;
    }
}
```

#### For Play Mode Tests:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MyPlayModeTests
{
    [UnityTest]
    public IEnumerator GameplayTest()
    {
        yield return new WaitForSeconds(0.1f);
    }
}
```

## Unity Version Compatibility

### Unity 2021.3+ (LTS)
- Test Framework 1.3+: Use `optionalUnityReferences`
- No need for manual DLL references
- Built-in test runner integration

### Unity 2022.3+ (LTS)
- Test Framework 1.4+: Enhanced async support
- Better performance profiling
- Improved test isolation

### Unity 2023.1+
- Test Framework 1.5+: Current version
- Domain reload optimization
- Better test fixtures

## Troubleshooting

### Issue: Tests Not Appearing in Test Runner
1. Ensure assembly definitions are correctly configured
2. Refresh the Test Runner window (Window > General > Test Runner)
3. Reimport test files if needed

### Issue: Compilation Errors
1. Check assembly references match your project structure
2. Ensure Test Framework package is installed (Package Manager)
3. Verify namespace consistency

### Issue: Tests Failing Randomly
1. Ensure proper setup/teardown
2. Check for static state pollution
3. Use test isolation attributes

## Running Tests

### In Unity Editor
1. Open Test Runner: Window > General > Test Runner
2. Select Edit Mode or Play Mode tab
3. Run all tests or select specific ones

### Command Line (CI/CD)
```bash
# Run all tests
Unity.exe -runTests -batchmode -projectPath "path/to/project" -testResults results.xml

# Run specific category
Unity.exe -runTests -batchmode -projectPath "path/to/project" -testResults results.xml -testFilter "Category=Unit"
```

## Performance Testing Best Practices

1. **Use Profiler Integration**:
```csharp
[UnityTest]
[Performance]
public IEnumerator MeasureFrameTime()
{
    using (Measure.Frames().Scope())
    {
        // Your test code
        yield return null;
    }
}
```

2. **Measure Allocations**:
```csharp
[Test]
public void MeasureGCAllocs()
{
    using (Measure.GarbageCollector().Scope())
    {
        // Your test code
    }
}
```

## Team Collaboration

### Git Configuration
Add to `.gitignore`:
```
# Test Results
TestResults/
test-results/
*.xml
```

### Continuous Integration
1. Set up test automation in CI pipeline
2. Run tests on pull requests
3. Generate test reports
4. Monitor test coverage

## Resources
- [Unity Test Framework Documentation](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NUnit Documentation](https://docs.nunit.org/)
- [Unity Performance Testing](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@latest)