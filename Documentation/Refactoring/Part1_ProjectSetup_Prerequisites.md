# Part 1: Project Setup & Prerequisites
## WallChessQuidor Refactoring Implementation Guide

---

## Table of Contents
1. [Environment Setup & Prerequisites](#1-environment-setup--prerequisites)
2. [Project Structure Analysis](#2-project-structure-analysis)
3. [Backup & Safety Procedures](#3-backup--safety-procedures)
4. [Development Workflow Setup](#4-development-workflow-setup)
5. [Pre-Refactor Validation](#5-pre-refactor-validation)
6. [Tool Setup](#6-tool-setup)

---

## 1. Environment Setup & Prerequisites

### 1.1 Unity Version Requirements

**Required Unity Version:** Unity 2022.3.x LTS (Long Term Support)
- Minimum: 2022.3.10f1
- Recommended: 2022.3.20f1 or latest LTS patch

**Installation Steps:**
1. Open Unity Hub
2. Navigate to Installs → Install Editor
3. Select Unity 2022.3.x LTS
4. Include modules:
   - Build Support for target platforms (Windows, Mac, Linux)
   - Visual Studio or Rider integration
   - Documentation
   - Unity Profiler

### 1.2 Required Packages and Dependencies

**Package Manager Configuration:**

1. Open Unity → Window → Package Manager
2. Enable "Show preview packages" in settings
3. Install/Update the following packages:

```json
{
  "dependencies": {
    "com.unity.inputsystem": "1.7.0",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.ugui": "1.0.0",
    "com.unity.addressables": "1.21.19",
    "com.unity.burst": "1.8.11",
    "com.unity.collections": "2.1.4",
    "com.unity.mathematics": "1.2.6",
    "com.unity.jobs": "0.70.0-preview.7",
    "com.unity.test-framework": "1.3.9",
    "com.unity.ide.visualstudio": "2.0.22",
    "com.unity.performance.profile-analyzer": "1.2.2"
  }
}
```

**Third-Party Dependencies:**
- DOTween Pro (if animation system uses it)
- Odin Inspector (optional but recommended for editor tools)
- Unity Async/Await utilities

### 1.3 Development Tools and Plugins

**Essential Tools:**
1. **IDE Setup:**
   - Visual Studio 2022 Community or Professional
   - OR JetBrains Rider 2023.3+
   - Install Unity integration plugins

2. **Version Control:**
   - Git 2.40+ with LFS support
   - SourceTree or Fork GUI client
   - Unity YAML Merge Tool configured

3. **Additional Unity Assets:**
   - Console Pro or Editor Console Pro (enhanced debugging)
   - Build Report Inspector
   - Asset Usage Detector

### 1.4 Git Setup and Branching Strategy

**Repository Configuration:**

`.gitignore` essentials:
```gitignore
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
[Mm]emoryCaptures/
[Rr]ecordings/

# Asset meta data
!/[Aa]ssets/**/*.meta

# Uncomment if using Plastic SCM
# .plastic/

# Jetbrains Rider
.idea/
*.iml

# Visual Studio cache
.vs/
*.user
*.userprefs
```

**Branching Strategy:**
```
main
├── develop
│   ├── refactor/phase-1-architecture
│   ├── refactor/phase-2-systems
│   ├── refactor/phase-3-optimization
│   └── refactor/phase-4-polish
└── hotfix/
```

**Git LFS Configuration:**
```bash
git lfs track "*.psd"
git lfs track "*.fbx"
git lfs track "*.wav"
git lfs track "*.mp3"
git lfs track "*.png"
git lfs track "*.jpg"
```

---

## 2. Project Structure Analysis

### 2.1 Current Folder Structure Assessment

**Existing Structure Issues:**
```
Assets/
├── AmplifyShaderEditor/     # Third-party, should be isolated
├── Scenes/                  # Mixed scene types
├── Scripts/                 # Flat structure, no organization
├── Prefabs/                 # No categorization
├── Materials/               # No naming convention
└── Textures/                # Mixed purposes
```

### 2.2 Proposed New Folder Organization

**Target Architecture:**
```
Assets/
├── _Project/
│   ├── Architecture/
│   │   ├── GameSystems/
│   │   │   ├── Board/
│   │   │   ├── Pieces/
│   │   │   ├── Movement/
│   │   │   ├── Rules/
│   │   │   └── AI/
│   │   ├── UISystem/
│   │   │   ├── Menus/
│   │   │   ├── HUD/
│   │   │   └── Dialogs/
│   │   └── Infrastructure/
│   │       ├── SaveLoad/
│   │       ├── Audio/
│   │       └── Input/
│   ├── Art/
│   │   ├── Models/
│   │   │   ├── Board/
│   │   │   ├── Pieces/
│   │   │   └── Environment/
│   │   ├── Materials/
│   │   │   ├── Board/
│   │   │   ├── Pieces/
│   │   │   └── UI/
│   │   ├── Textures/
│   │   │   ├── Board/
│   │   │   ├── Pieces/
│   │   │   └── UI/
│   │   └── Shaders/
│   ├── Prefabs/
│   │   ├── Core/
│   │   ├── Gameplay/
│   │   ├── UI/
│   │   └── Environment/
│   ├── Resources/
│   │   ├── GameConfigs/
│   │   ├── Addressables/
│   │   └── StreamingAssets/
│   ├── Scenes/
│   │   ├── Core/
│   │   │   ├── _Preload.unity
│   │   │   └── Main.unity
│   │   ├── Gameplay/
│   │   └── UI/
│   └── Settings/
│       ├── Input/
│       ├── Rendering/
│       └── Audio/
├── ThirdParty/
│   ├── AmplifyShaderEditor/
│   └── [Other plugins]/
└── Editor/
    ├── Tools/
    ├── Windows/
    └── Inspectors/
```

### 2.3 Migration Path for Existing Files

**Phase 1: Preparation**
```csharp
// EditorScript: ProjectStructureMigrator.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public class ProjectStructureMigrator : EditorWindow
{
    [MenuItem("WallChessQuidor/Refactor Tools/Structure Migrator")]
    static void Init()
    {
        GetWindow<ProjectStructureMigrator>("Structure Migrator");
    }
    
    void OnGUI()
    {
        if (GUILayout.Button("1. Create New Structure"))
        {
            CreateFolderStructure();
        }
        
        if (GUILayout.Button("2. Analyze Dependencies"))
        {
            AnalyzeAssetDependencies();
        }
        
        if (GUILayout.Button("3. Generate Migration Report"))
        {
            GenerateMigrationReport();
        }
    }
    
    void CreateFolderStructure()
    {
        // Implementation for creating folders
        string[] folders = {
            "Assets/_Project",
            "Assets/_Project/Architecture",
            "Assets/_Project/Architecture/GameSystems",
            // ... etc
        };
        
        foreach(var folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }
        AssetDatabase.Refresh();
    }
}
```

### 2.4 Naming Conventions and Standards

**Asset Naming Convention:**
```
[Prefix]_[AssetName]_[Variant]_[Number]

Prefixes:
- PRE_ : Prefabs
- MAT_ : Materials
- TEX_ : Textures
- MDL_ : Models
- SND_ : Sounds
- MUS_ : Music
- UI_  : UI Elements
- SCR_ : ScriptableObjects
- ANI_ : Animations
```

**Script Naming Standards:**
```csharp
// Systems: [Feature]System.cs
BoardSystem.cs
MovementSystem.cs

// Controllers: [Target]Controller.cs
PieceController.cs
PlayerController.cs

// Managers: [Domain]Manager.cs
GameManager.cs
UIManager.cs

// Data: [Type]Data.cs or [Type]Config.cs
PieceData.cs
BoardConfig.cs

// Interfaces: I[Capability].cs
IMoveable.cs
ISelectable.cs
```

---

## 3. Backup & Safety Procedures

### 3.1 Complete Project Backup Strategy

**Automated Backup Script:**
```csharp
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ProjectBackupTool : EditorWindow
{
    private string backupPath = "D:/WallChessQuidor_Backups";
    
    [MenuItem("WallChessQuidor/Backup/Create Full Backup")]
    static void CreateBackup()
    {
        var window = GetWindow<ProjectBackupTool>();
        window.ExecuteBackup();
    }
    
    void ExecuteBackup()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string backupFolder = Path.Combine(backupPath, $"Backup_{timestamp}");
        
        // Create backup folder
        Directory.CreateDirectory(backupFolder);
        
        // Copy project files
        CopyDirectory("Assets", Path.Combine(backupFolder, "Assets"));
        CopyDirectory("ProjectSettings", Path.Combine(backupFolder, "ProjectSettings"));
        CopyDirectory("Packages", Path.Combine(backupFolder, "Packages"));
        
        // Log backup info
        File.WriteAllText(
            Path.Combine(backupFolder, "backup_info.txt"),
            $"Backup Created: {timestamp}\n" +
            $"Unity Version: {Application.unityVersion}\n" +
            $"Platform: {Application.platform}"
        );
        
        Debug.Log($"Backup created at: {backupFolder}");
    }
    
    static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        
        foreach (string file in Directory.GetFiles(source))
        {
            string dest = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }
        
        foreach (string dir in Directory.GetDirectories(source))
        {
            string dest = Path.Combine(destination, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }
}
```

### 3.2 Git Branching for Safe Refactoring

**Branch Creation Commands:**
```bash
# Create main refactor branch
git checkout -b refactor/main-architecture

# Create phase branches
git checkout -b refactor/phase-1-architecture
git checkout -b refactor/phase-2-systems
git checkout -b refactor/phase-3-optimization
git checkout -b refactor/phase-4-polish

# Tag current state before refactoring
git tag -a pre-refactor-v1.0 -m "State before major refactoring"
git push origin --tags
```

**Commit Strategy:**
```bash
# Atomic commits for each change
git add -A
git commit -m "refactor: extract BoardSystem from monolithic GameManager"

# Regular pushes to remote
git push origin refactor/phase-1-architecture

# Create restore points
git tag -a checkpoint-phase1-complete -m "Phase 1 architecture complete"
```

### 3.3 Rollback Procedures

**Emergency Rollback Script:**
```csharp
public class EmergencyRollback : EditorWindow
{
    [MenuItem("WallChessQuidor/Emergency/Rollback to Checkpoint")]
    static void ShowWindow()
    {
        GetWindow<EmergencyRollback>("Emergency Rollback");
    }
    
    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "WARNING: This will reset to last checkpoint!", 
            MessageType.Warning
        );
        
        if (GUILayout.Button("Rollback to Last Checkpoint"))
        {
            if (EditorUtility.DisplayDialog(
                "Confirm Rollback",
                "This will lose all changes since checkpoint. Continue?",
                "Yes, Rollback",
                "Cancel"))
            {
                ExecuteRollback();
            }
        }
    }
    
    void ExecuteRollback()
    {
        // Git reset to last tag
        System.Diagnostics.Process.Start("git", "reset --hard pre-refactor-v1.0");
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Rollback Complete", 
            "Project rolled back to checkpoint", "OK");
    }
}
```

### 3.4 Testing Environment Setup

**Test Scene Configuration:**
1. Create `Assets/_Project/Scenes/Testing/RefactorTestScene.unity`
2. Set up isolated test environment
3. Include debug visualization tools

**Test Harness Setup:**
```csharp
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

public class RefactorValidationTests
{
    [Test]
    public void ValidateGameSystemsInitialization()
    {
        // Test that all systems initialize correctly
        var gameManager = new GameObject().AddComponent<GameManager>();
        Assert.IsNotNull(gameManager.BoardSystem);
        Assert.IsNotNull(gameManager.MovementSystem);
        Assert.IsNotNull(gameManager.RuleSystem);
    }
    
    [UnityTest]
    public IEnumerator ValidatePieceMovement()
    {
        // Set up test board
        yield return new WaitForSeconds(0.1f);
        
        // Test piece movement
        var piece = GameObject.Find("TestPiece");
        var originalPos = piece.transform.position;
        
        // Trigger movement
        // ... movement logic
        
        yield return new WaitForSeconds(1f);
        
        Assert.AreNotEqual(originalPos, piece.transform.position);
    }
}
```

---

## 4. Development Workflow Setup

### 4.1 Editor Settings and Preferences

**Unity Editor Configuration:**

1. **Edit → Preferences → General:**
   - Auto Refresh: Disabled (manual control during refactoring)
   - Script Changes While Playing: Stop and Recompile

2. **Edit → Preferences → External Tools:**
   - External Script Editor: Visual Studio 2022 / Rider
   - Generate all .csproj files: Enabled

3. **Edit → Project Settings → Editor:**
   ```
   Version Control Mode: Visible Meta Files
   Asset Serialization: Force Text
   Default Behavior Mode: 3D
   Sprite Packer: Disabled (use Addressables instead)
   ```

4. **Edit → Project Settings → Player:**
   ```
   Company Name: WindmillHill
   Product Name: WallChessQuidor
   Default Icon: [Set project icon]
   Configuration:
     Scripting Backend: IL2CPP
     Api Compatibility: .NET Standard 2.1
     Active Input Handling: Input System Package (New)
   ```

### 4.2 Code Style and Formatting Rules

**EditorConfig File (.editorconfig):**
```ini
root = true

[*.cs]
# Indentation
indent_style = space
indent_size = 4
tab_width = 4

# New line preferences
end_of_line = crlf
insert_final_newline = true

# C# specific
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_indent_case_contents = true
csharp_indent_switch_labels = true

# Code style rules
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning

# Naming conventions
dotnet_naming_rule.interface_should_be_begins_with_i.severity = warning
dotnet_naming_rule.interface_should_be_begins_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_begins_with_i.style = begins_with_i

dotnet_naming_rule.private_or_internal_field_should_be_camelCase.severity = warning
dotnet_naming_rule.private_or_internal_field_should_be_camelCase.symbols = private_or_internal_field
dotnet_naming_rule.private_or_internal_field_should_be_camelCase.style = camel_case
```

**Code Analysis Ruleset:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<RuleSet Name="WallChessQuidor Rules" ToolsVersion="16.0">
  <Rules AnalyzerId="Microsoft.CodeQuality.Analyzers">
    <Rule Id="CA1001" Action="Warning" />
    <Rule Id="CA1009" Action="Warning" />
    <Rule Id="CA1016" Action="Warning" />
    <Rule Id="CA1033" Action="Warning" />
    <Rule Id="CA1049" Action="Warning" />
    <Rule Id="CA1060" Action="Warning" />
    <Rule Id="CA1061" Action="Warning" />
    <Rule Id="CA1063" Action="Warning" />
    <Rule Id="CA1065" Action="Warning" />
    <Rule Id="CA1301" Action="Warning" />
    <Rule Id="CA1400" Action="Warning" />
  </Rules>
</RuleSet>
```

### 4.3 Performance Profiling Setup

**Profiler Markers Implementation:**
```csharp
using Unity.Profiling;
using UnityEngine;

public static class ProfilerMarkers
{
    public static readonly ProfilerMarker BoardUpdate = 
        new ProfilerMarker("WallChessQuidor.Board.Update");
    
    public static readonly ProfilerMarker PieceMovement = 
        new ProfilerMarker("WallChessQuidor.Piece.Movement");
    
    public static readonly ProfilerMarker AIThinking = 
        new ProfilerMarker("WallChessQuidor.AI.Think");
    
    public static readonly ProfilerMarker UIUpdate = 
        new ProfilerMarker("WallChessQuidor.UI.Update");
    
    public static readonly ProfilerMarker PathfindingCalculation = 
        new ProfilerMarker("WallChessQuidor.Pathfinding.Calculate");
}

// Usage example:
public class BoardSystem : MonoBehaviour
{
    void Update()
    {
        using (ProfilerMarkers.BoardUpdate.Auto())
        {
            // Board update logic
            UpdateBoardState();
            ValidateMoves();
        }
    }
}
```

**Performance Baseline Configuration:**
```csharp
[System.Serializable]
public class PerformanceTargets
{
    public float TargetFPS = 60f;
    public float MaxFrameTime = 16.67f; // ms
    public int MaxDrawCalls = 100;
    public int MaxSetPassCalls = 50;
    public float MaxMemoryUsage = 512f; // MB
    public float MaxGCAllocPerFrame = 0f; // KB
}
```

### 4.4 Debugging Configuration

**Debug Manager Setup:**
```csharp
using UnityEngine;
using System.Collections.Generic;

public class DebugManager : MonoBehaviour
{
    private static DebugManager instance;
    public static DebugManager Instance => instance;
    
    [Header("Debug Settings")]
    public bool EnableDebugVisualization = true;
    public bool ShowPerformanceStats = true;
    public bool LogDetailedErrors = true;
    public bool EnableSlowMotion = false;
    
    [Header("Debug Overlays")]
    public bool ShowBoardGrid = true;
    public bool ShowPieceInfo = true;
    public bool ShowMovementPaths = true;
    public bool ShowAIDecisions = false;
    
    private Dictionary<string, object> debugValues = new Dictionary<string, object>();
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public static void Log(string category, string message, LogType logType = LogType.Log)
    {
        if (Instance == null || !Instance.LogDetailedErrors) return;
        
        string formattedMessage = $"[{category}] {message}";
        
        switch (logType)
        {
            case LogType.Error:
                Debug.LogError(formattedMessage);
                break;
            case LogType.Warning:
                Debug.LogWarning(formattedMessage);
                break;
            default:
                Debug.Log(formattedMessage);
                break;
        }
    }
    
    public static void SetDebugValue(string key, object value)
    {
        if (Instance != null)
        {
            Instance.debugValues[key] = value;
        }
    }
    
    void OnGUI()
    {
        if (!ShowPerformanceStats) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"FPS: {1f / Time.deltaTime:F1}");
        GUILayout.Label($"Frame Time: {Time.deltaTime * 1000f:F2}ms");
        
        foreach (var kvp in debugValues)
        {
            GUILayout.Label($"{kvp.Key}: {kvp.Value}");
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
```

---

## 5. Pre-Refactor Validation

### 5.1 Current Codebase Health Check

**Code Metrics Analyzer:**
```csharp
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class CodeHealthAnalyzer : EditorWindow
{
    private CodeMetrics metrics = new CodeMetrics();
    
    [MenuItem("WallChessQuidor/Analysis/Code Health Check")]
    static void ShowWindow()
    {
        GetWindow<CodeHealthAnalyzer>("Code Health");
    }
    
    void OnGUI()
    {
        if (GUILayout.Button("Analyze Codebase"))
        {
            AnalyzeCodebase();
        }
        
        if (metrics.IsAnalyzed)
        {
            EditorGUILayout.LabelField("Code Metrics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Scripts: {metrics.TotalScripts}");
            EditorGUILayout.LabelField($"Total Lines: {metrics.TotalLines}");
            EditorGUILayout.LabelField($"Average Complexity: {metrics.AverageComplexity:F2}");
            EditorGUILayout.LabelField($"Code Duplication: {metrics.DuplicationPercentage:F1}%");
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Issues Found", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"God Classes: {metrics.GodClasses.Count}");
            EditorGUILayout.LabelField($"Long Methods: {metrics.LongMethods.Count}");
            EditorGUILayout.LabelField($"High Coupling: {metrics.HighCouplingClasses.Count}");
            
            if (metrics.GodClasses.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("God Classes:", EditorStyles.boldLabel);
                foreach (var godClass in metrics.GodClasses)
                {
                    EditorGUILayout.LabelField($"  - {godClass}");
                }
            }
        }
    }
    
    void AnalyzeCodebase()
    {
        metrics = new CodeMetrics();
        var scripts = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
        
        foreach (var guid in scripts)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            AnalyzeScript(path);
        }
        
        metrics.IsAnalyzed = true;
    }
    
    void AnalyzeScript(string path)
    {
        // Implementation for analyzing individual scripts
        var lines = System.IO.File.ReadAllLines(path);
        metrics.TotalScripts++;
        metrics.TotalLines += lines.Length;
        
        // Check for god classes (>500 lines)
        if (lines.Length > 500)
        {
            metrics.GodClasses.Add(System.IO.Path.GetFileName(path));
        }
        
        // Additional analysis...
    }
    
    [System.Serializable]
    public class CodeMetrics
    {
        public bool IsAnalyzed;
        public int TotalScripts;
        public int TotalLines;
        public float AverageComplexity;
        public float DuplicationPercentage;
        public List<string> GodClasses = new List<string>();
        public List<string> LongMethods = new List<string>();
        public List<string> HighCouplingClasses = new List<string>();
    }
}
```

### 5.2 Performance Baseline Measurements

**Performance Baseline Recorder:**
```csharp
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.IO;

public class PerformanceBaseline : MonoBehaviour
{
    [System.Serializable]
    public class BaselineData
    {
        public float averageFPS;
        public float minFPS;
        public float maxFPS;
        public float averageFrameTime;
        public long totalMemory;
        public long gcMemory;
        public int drawCalls;
        public int setPassCalls;
        public Dictionary<string, float> customMarkers = new Dictionary<string, float>();
    }
    
    private BaselineData baseline = new BaselineData();
    private List<float> fpsHistory = new List<float>();
    private float recordingTime = 60f; // Record for 60 seconds
    private float startTime;
    
    [ContextMenu("Start Recording Baseline")]
    public void StartRecording()
    {
        startTime = Time.time;
        fpsHistory.Clear();
        InvokeRepeating(nameof(RecordFrame), 0f, 0.1f);
    }
    
    void RecordFrame()
    {
        if (Time.time - startTime > recordingTime)
        {
            CancelInvoke(nameof(RecordFrame));
            CalculateBaseline();
            SaveBaseline();
            return;
        }
        
        float fps = 1f / Time.deltaTime;
        fpsHistory.Add(fps);
        
        // Record memory
        baseline.totalMemory = Profiler.GetTotalAllocatedMemoryLong();
        baseline.gcMemory = Profiler.GetMonoUsedSizeLong();
    }
    
    void CalculateBaseline()
    {
        baseline.averageFPS = fpsHistory.Average();
        baseline.minFPS = fpsHistory.Min();
        baseline.maxFPS = fpsHistory.Max();
        baseline.averageFrameTime = 1000f / baseline.averageFPS;
        
        Debug.Log($"Baseline Recorded: Avg FPS: {baseline.averageFPS:F1}, " +
                  $"Min: {baseline.minFPS:F1}, Max: {baseline.maxFPS:F1}");
    }
    
    void SaveBaseline()
    {
        string json = JsonUtility.ToJson(baseline, true);
        string path = Path.Combine(Application.dataPath, 
            "../ProjectSettings/PerformanceBaseline.json");
        File.WriteAllText(path, json);
        Debug.Log($"Baseline saved to: {path}");
    }
}
```

### 5.3 Functionality Verification Tests

**Automated Test Suite:**
```csharp
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class FunctionalityVerificationTests
{
    [Test]
    public void Test_BoardInitialization()
    {
        var board = new BoardSystem();
        board.Initialize(8, 8); // Chess board size
        
        Assert.AreEqual(64, board.TotalTiles);
        Assert.IsNotNull(board.GetTile(0, 0));
        Assert.IsNotNull(board.GetTile(7, 7));
    }
    
    [Test]
    public void Test_PieceCreation()
    {
        var pieceFactory = new PieceFactory();
        
        var pawn = pieceFactory.CreatePiece(PieceType.Pawn, TeamColor.White);
        Assert.IsNotNull(pawn);
        Assert.AreEqual(PieceType.Pawn, pawn.Type);
        
        var queen = pieceFactory.CreatePiece(PieceType.Queen, TeamColor.Black);
        Assert.IsNotNull(queen);
        Assert.AreEqual(PieceType.Queen, queen.Type);
    }
    
    [UnityTest]
    public IEnumerator Test_PieceMovement()
    {
        // Setup
        GameObject testScene = new GameObject("TestScene");
        var board = testScene.AddComponent<BoardSystem>();
        board.Initialize(8, 8);
        
        yield return null; // Wait a frame
        
        var piece = board.PlacePiece(PieceType.Pawn, new Vector2Int(1, 1));
        var startPos = piece.Position;
        
        // Act
        bool moveResult = board.MovePiece(piece, new Vector2Int(1, 2));
        
        yield return new WaitForSeconds(0.5f);
        
        // Assert
        Assert.IsTrue(moveResult);
        Assert.AreEqual(new Vector2Int(1, 2), piece.Position);
        Assert.AreNotEqual(startPos, piece.Position);
        
        // Cleanup
        Object.Destroy(testScene);
    }
    
    [Test]
    public void Test_GameRules_ValidMoves()
    {
        var rules = new GameRules();
        var board = new BoardSystem();
        board.Initialize(8, 8);
        
        var pawn = new Piece(PieceType.Pawn, TeamColor.White);
        pawn.Position = new Vector2Int(1, 1);
        
        var validMoves = rules.GetValidMoves(pawn, board);
        
        Assert.Greater(validMoves.Count, 0);
        Assert.Contains(new Vector2Int(1, 2), validMoves);
    }
    
    [Test]
    public void Test_SaveLoadSystem()
    {
        var saveSystem = new SaveLoadSystem();
        var testData = new GameState
        {
            currentTurn = TeamColor.White,
            turnNumber = 5,
            boardState = "test_board_state"
        };
        
        // Save
        saveSystem.SaveGame("test_save", testData);
        
        // Load
        var loadedData = saveSystem.LoadGame("test_save");
        
        Assert.IsNotNull(loadedData);
        Assert.AreEqual(testData.currentTurn, loadedData.currentTurn);
        Assert.AreEqual(testData.turnNumber, loadedData.turnNumber);
    }
}
```

### 5.4 Documentation of Existing Behavior

**Behavior Documentation Generator:**
```csharp
using UnityEditor;
using System.IO;
using System.Text;

public class BehaviorDocumentationGenerator : EditorWindow
{
    [MenuItem("WallChessQuidor/Documentation/Generate Behavior Docs")]
    static void GenerateDocs()
    {
        var window = GetWindow<BehaviorDocumentationGenerator>();
        window.CreateDocumentation();
    }
    
    void CreateDocumentation()
    {
        StringBuilder doc = new StringBuilder();
        
        doc.AppendLine("# WallChessQuidor Current Behavior Documentation");
        doc.AppendLine($"Generated: {System.DateTime.Now}");
        doc.AppendLine();
        
        // Document game flow
        doc.AppendLine("## Game Flow");
        doc.AppendLine("1. Main Menu → Game Setup");
        doc.AppendLine("2. Board Initialization");
        doc.AppendLine("3. Piece Placement");
        doc.AppendLine("4. Turn-based Gameplay Loop");
        doc.AppendLine("5. Win Condition Check");
        doc.AppendLine("6. Game End → Results");
        doc.AppendLine();
        
        // Document piece behaviors
        doc.AppendLine("## Piece Behaviors");
        DocumentPieceBehaviors(doc);
        
        // Document game rules
        doc.AppendLine("## Game Rules");
        DocumentGameRules(doc);
        
        // Save documentation
        string path = Path.Combine(Application.dataPath, 
            "../Documentation/CurrentBehavior.md");
        File.WriteAllText(path, doc.ToString());
        
        Debug.Log($"Documentation generated at: {path}");
        AssetDatabase.Refresh();
    }
    
    void DocumentPieceBehaviors(StringBuilder doc)
    {
        doc.AppendLine("### Pawn");
        doc.AppendLine("- Moves forward one square");
        doc.AppendLine("- Can move two squares on first move");
        doc.AppendLine("- Captures diagonally");
        doc.AppendLine();
        
        doc.AppendLine("### Knight");
        doc.AppendLine("- L-shaped movement");
        doc.AppendLine("- Can jump over pieces");
        doc.AppendLine();
        
        // Continue for other pieces...
    }
    
    void DocumentGameRules(StringBuilder doc)
    {
        doc.AppendLine("### Movement Rules");
        doc.AppendLine("- Players alternate turns");
        doc.AppendLine("- Must move when it's your turn");
        doc.AppendLine("- Cannot move into check");
        doc.AppendLine();
        
        doc.AppendLine("### Wall Placement Rules");
        doc.AppendLine("- Each player has limited walls");
        doc.AppendLine("- Walls block movement");
        doc.AppendLine("- Cannot completely block path to goal");
        doc.AppendLine();
    }
}
```

---

## 6. Tool Setup

### 6.1 Unity Profiler Configuration

**Custom Profiler Module:**
```csharp
using Unity.Profiling;
using Unity.Profiling.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

[System.Serializable]
[ProfilerModuleMetadata("WallChessQuidor Performance")]
public class WallChessQuidorProfilerModule : ProfilerModule
{
    static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
    {
        new ProfilerCounterDescriptor("Board Updates", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("Piece Movements", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("AI Calculations", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("Pathfinding Calls", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("UI Refreshes", ProfilerCategory.UI),
    };
    
    public WallChessQuidorProfilerModule() : base(k_Counters) { }
    
    public override void DrawToolbar(Rect position)
    {
        // Custom toolbar if needed
    }
    
    public override void DrawDetailsView(Rect position)
    {
        // Custom details view
        EditorGUILayout.LabelField("WallChessQuidor Performance Details");
        EditorGUILayout.LabelField($"Frame Budget: 16.67ms");
        EditorGUILayout.LabelField($"Target FPS: 60");
    }
}
```

### 6.2 Performance Monitoring Tools

**Runtime Performance Monitor:**
```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RuntimePerformanceMonitor : MonoBehaviour
{
    [Header("Monitoring Settings")]
    public bool enableMonitoring = true;
    public float sampleInterval = 0.5f;
    public int maxSamples = 120; // 1 minute of data at 0.5s intervals
    
    [Header("Thresholds")]
    public float fpsWarningThreshold = 45f;
    public float fpsErrorThreshold = 30f;
    public float frameTimeWarningMs = 22f;
    public float frameTimeErrorMs = 33f;
    public float memoryWarningMB = 400f;
    public float memoryErrorMB = 450f;
    
    private Queue<PerformanceSample> samples = new Queue<PerformanceSample>();
    private float nextSampleTime;
    
    [System.Serializable]
    public class PerformanceSample
    {
        public float timestamp;
        public float fps;
        public float frameTimeMs;
        public float memoryMB;
        public int drawCalls;
        public int setPassCalls;
        public Dictionary<string, float> customMetrics;
        
        public PerformanceSample()
        {
            customMetrics = new Dictionary<string, float>();
        }
    }
    
    void Start()
    {
        if (enableMonitoring)
        {
            InvokeRepeating(nameof(TakeSample), 0f, sampleInterval);
        }
    }
    
    void TakeSample()
    {
        var sample = new PerformanceSample
        {
            timestamp = Time.time,
            fps = 1f / Time.deltaTime,
            frameTimeMs = Time.deltaTime * 1000f,
            memoryMB = GC.GetTotalMemory(false) / (1024f * 1024f)
        };
        
        samples.Enqueue(sample);
        
        if (samples.Count > maxSamples)
        {
            samples.Dequeue();
        }
        
        CheckThresholds(sample);
    }
    
    void CheckThresholds(PerformanceSample sample)
    {
        if (sample.fps < fpsErrorThreshold)
        {
            Debug.LogError($"FPS critically low: {sample.fps:F1}");
        }
        else if (sample.fps < fpsWarningThreshold)
        {
            Debug.LogWarning($"FPS below target: {sample.fps:F1}");
        }
        
        if (sample.memoryMB > memoryErrorMB)
        {
            Debug.LogError($"Memory usage critical: {sample.memoryMB:F1}MB");
        }
        else if (sample.memoryMB > memoryWarningMB)
        {
            Debug.LogWarning($"Memory usage high: {sample.memoryMB:F1}MB");
        }
    }
    
    public PerformanceReport GenerateReport()
    {
        if (samples.Count == 0) return null;
        
        return new PerformanceReport
        {
            averageFPS = samples.Average(s => s.fps),
            minFPS = samples.Min(s => s.fps),
            maxFPS = samples.Max(s => s.fps),
            averageFrameTime = samples.Average(s => s.frameTimeMs),
            averageMemory = samples.Average(s => s.memoryMB),
            peakMemory = samples.Max(s => s.memoryMB)
        };
    }
    
    [System.Serializable]
    public class PerformanceReport
    {
        public float averageFPS;
        public float minFPS;
        public float maxFPS;
        public float averageFrameTime;
        public float averageMemory;
        public float peakMemory;
    }
}
```

### 6.3 Static Analysis Tools

**Code Quality Analyzer:**
```csharp
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class CodeQualityAnalyzer : EditorWindow
{
    private List<CodeIssue> issues = new List<CodeIssue>();
    
    [MenuItem("WallChessQuidor/Tools/Code Quality Analyzer")]
    static void ShowWindow()
    {
        GetWindow<CodeQualityAnalyzer>("Code Quality");
    }
    
    void OnGUI()
    {
        if (GUILayout.Button("Run Analysis"))
        {
            RunAnalysis();
        }
        
        if (issues.Count > 0)
        {
            EditorGUILayout.LabelField($"Found {issues.Count} issues:", EditorStyles.boldLabel);
            
            foreach (var issue in issues)
            {
                EditorGUILayout.BeginHorizontal();
                
                var color = GUI.color;
                GUI.color = GetSeverityColor(issue.severity);
                EditorGUILayout.LabelField($"[{issue.severity}]", GUILayout.Width(80));
                GUI.color = color;
                
                EditorGUILayout.LabelField(issue.description);
                
                if (GUILayout.Button("Go To", GUILayout.Width(50)))
                {
                    OpenScript(issue.filePath, issue.line);
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
    }
    
    void RunAnalysis()
    {
        issues.Clear();
        
        var scripts = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
        
        foreach (var guid in scripts)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            AnalyzeScript(path);
        }
        
        issues = issues.OrderByDescending(i => i.severity).ToList();
    }
    
    void AnalyzeScript(string path)
    {
        var lines = File.ReadAllLines(path);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Check for common issues
            if (line.Contains("GameObject.Find"))
            {
                issues.Add(new CodeIssue
                {
                    severity = IssueSeverity.Warning,
                    description = "GameObject.Find is expensive",
                    filePath = path,
                    line = i + 1
                });
            }
            
            if (line.Contains("public static") && !line.Contains("readonly"))
            {
                issues.Add(new CodeIssue
                {
                    severity = IssueSeverity.Warning,
                    description = "Mutable static field detected",
                    filePath = path,
                    line = i + 1
                });
            }
            
            if (line.Contains("Resources.Load"))
            {
                issues.Add(new CodeIssue
                {
                    severity = IssueSeverity.Info,
                    description = "Consider using Addressables instead of Resources",
                    filePath = path,
                    line = i + 1
                });
            }
            
            // Check for empty catch blocks
            if (line.Trim() == "catch" || line.Contains("catch ("))
            {
                if (i + 1 < lines.Length && lines[i + 1].Trim() == "{" &&
                    i + 2 < lines.Length && lines[i + 2].Trim() == "}")
                {
                    issues.Add(new CodeIssue
                    {
                        severity = IssueSeverity.Error,
                        description = "Empty catch block",
                        filePath = path,
                        line = i + 1
                    });
                }
            }
        }
    }
    
    void OpenScript(string path, int line)
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        if (script != null)
        {
            AssetDatabase.OpenAsset(script, line);
        }
    }
    
    Color GetSeverityColor(IssueSeverity severity)
    {
        switch (severity)
        {
            case IssueSeverity.Error: return Color.red;
            case IssueSeverity.Warning: return Color.yellow;
            case IssueSeverity.Info: return Color.cyan;
            default: return Color.white;
        }
    }
    
    [System.Serializable]
    public class CodeIssue
    {
        public IssueSeverity severity;
        public string description;
        public string filePath;
        public int line;
    }
    
    public enum IssueSeverity
    {
        Info,
        Warning,
        Error
    }
}
```

### 6.4 Custom Editor Tools for Refactoring

**Refactoring Assistant Window:**
```csharp
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class RefactoringAssistant : EditorWindow
{
    private Vector2 scrollPosition;
    private RefactorPhase currentPhase = RefactorPhase.Setup;
    
    private enum RefactorPhase
    {
        Setup,
        Architecture,
        Systems,
        Optimization,
        Polish
    }
    
    [MenuItem("WallChessQuidor/Refactoring Assistant")]
    static void ShowWindow()
    {
        var window = GetWindow<RefactoringAssistant>("Refactoring Assistant");
        window.minSize = new Vector2(400, 600);
    }
    
    void OnGUI()
    {
        DrawHeader();
        DrawPhaseSelector();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        switch (currentPhase)
        {
            case RefactorPhase.Setup:
                DrawSetupPhase();
                break;
            case RefactorPhase.Architecture:
                DrawArchitecturePhase();
                break;
            case RefactorPhase.Systems:
                DrawSystemsPhase();
                break;
            case RefactorPhase.Optimization:
                DrawOptimizationPhase();
                break;
            case RefactorPhase.Polish:
                DrawPolishPhase();
                break;
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("WallChessQuidor Refactoring Assistant", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
    }
    
    void DrawPhaseSelector()
    {
        EditorGUILayout.BeginHorizontal();
        
        foreach (RefactorPhase phase in System.Enum.GetValues(typeof(RefactorPhase)))
        {
            bool isSelected = phase == currentPhase;
            var style = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
            
            if (GUILayout.Toggle(isSelected, phase.ToString(), style))
            {
                currentPhase = phase;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }
    
    void DrawSetupPhase()
    {
        EditorGUILayout.LabelField("Setup Phase", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create Backup"))
        {
            ProjectBackupTool.CreateBackup();
        }
        
        if (GUILayout.Button("Initialize Git Branch"))
        {
            InitializeGitBranch();
        }
        
        if (GUILayout.Button("Setup Folder Structure"))
        {
            SetupFolderStructure();
        }
        
        if (GUILayout.Button("Configure Project Settings"))
        {
            ConfigureProjectSettings();
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "Complete all setup tasks before proceeding to Architecture phase.",
            MessageType.Info
        );
    }
    
    void DrawArchitecturePhase()
    {
        EditorGUILayout.LabelField("Architecture Phase", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Extract Interfaces"))
        {
            ExtractInterfaces();
        }
        
        if (GUILayout.Button("Create System Classes"))
        {
            CreateSystemClasses();
        }
        
        if (GUILayout.Button("Setup Dependency Injection"))
        {
            SetupDependencyInjection();
        }
        
        if (GUILayout.Button("Generate Assembly Definitions"))
        {
            GenerateAssemblyDefinitions();
        }
    }
    
    void DrawSystemsPhase()
    {
        EditorGUILayout.LabelField("Systems Phase", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refactor Board System"))
        {
            RefactorBoardSystem();
        }
        
        if (GUILayout.Button("Refactor Movement System"))
        {
            RefactorMovementSystem();
        }
        
        if (GUILayout.Button("Refactor UI System"))
        {
            RefactorUISystem();
        }
        
        if (GUILayout.Button("Refactor Save/Load System"))
        {
            RefactorSaveLoadSystem();
        }
    }
    
    void DrawOptimizationPhase()
    {
        EditorGUILayout.LabelField("Optimization Phase", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Run Performance Analysis"))
        {
            RunPerformanceAnalysis();
        }
        
        if (GUILayout.Button("Optimize Draw Calls"))
        {
            OptimizeDrawCalls();
        }
        
        if (GUILayout.Button("Implement Object Pooling"))
        {
            ImplementObjectPooling();
        }
        
        if (GUILayout.Button("Optimize Memory Usage"))
        {
            OptimizeMemoryUsage();
        }
    }
    
    void DrawPolishPhase()
    {
        EditorGUILayout.LabelField("Polish Phase", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Clean Up Code"))
        {
            CleanUpCode();
        }
        
        if (GUILayout.Button("Generate Documentation"))
        {
            GenerateDocumentation();
        }
        
        if (GUILayout.Button("Run Final Tests"))
        {
            RunFinalTests();
        }
        
        if (GUILayout.Button("Create Release Build"))
        {
            CreateReleaseBuild();
        }
    }
    
    // Implementation methods
    void InitializeGitBranch()
    {
        Debug.Log("Creating refactor branch...");
        System.Diagnostics.Process.Start("git", "checkout -b refactor/main-architecture");
    }
    
    void SetupFolderStructure()
    {
        // Implementation from earlier
        ProjectStructureMigrator.CreateFolderStructure();
    }
    
    void ConfigureProjectSettings()
    {
        // Configure Unity settings programmatically
        PlayerSettings.companyName = "WindmillHill";
        PlayerSettings.productName = "WallChessQuidor";
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard_2_0);
        
        Debug.Log("Project settings configured");
    }
    
    // Additional implementation methods...
}
```

---

## Summary & Next Steps

This Part 1 document has established the complete foundation for the WallChessQuidor refactoring process. You now have:

1. **Environment Ready**: Unity 2022.3 LTS with all required packages
2. **Safety Net**: Complete backup and rollback procedures
3. **Structure Plan**: Clear migration path from current to target architecture
4. **Monitoring Tools**: Performance baseline and analysis tools
5. **Workflow Setup**: Configured development environment and standards
6. **Validation Framework**: Tests to ensure nothing breaks during refactoring

### Immediate Action Items:

1. **Create Initial Backup** using the ProjectBackupTool
2. **Initialize Git Branch** for refactoring work
3. **Run Code Health Check** to identify priority refactoring targets
4. **Record Performance Baseline** for comparison
5. **Set Up Development Environment** according to specifications

### Ready for Part 2:
With this foundation in place, you're now ready to proceed to Part 2: Core Architecture Refactoring, where we'll begin the actual code transformation process.

---

*Document Version: 1.0*
*Last Updated: [Current Date]*
*Pages: 1-8 of Implementation Guide*