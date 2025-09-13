# Part 1: Project Setup & Prerequisites
**WallChessQuidor Refactoring Implementation Guide**

**Page:** 1-8 of 101  
**Document:** 01-project-setup-prerequisites.md  
**Prerequisites:** Architecture review completed  
**Next Document:** [Part 6: Codebase Cleanup](06-codebase-cleanup.md)  

---

## Table of Contents
1. [Environment Setup & Prerequisites](#environment-setup--prerequisites)
2. [Project Structure Analysis](#project-structure-analysis)
3. [Backup & Safety Procedures](#backup--safety-procedures)
4. [Development Workflow Setup](#development-workflow-setup)
5. [Pre-Refactor Validation](#pre-refactor-validation)
6. [Tool Setup](#tool-setup)
7. [Validation Checklist](#validation-checklist)

---

## Environment Setup & Prerequisites

### Unity Version Requirements
- **Unity 2022.3 LTS** (minimum 2022.3.12f1)
- **Reason**: Stable LFS support, improved Profiler API, C# 9.0 features
- **Installation**: Unity Hub → Installs → Add → 2022.3 LTS → Visual Studio integration

### Required Packages
Add these packages via Unity Package Manager (Window → Package Manager):

```json
{
  "dependencies": {
    "com.unity.addressables": "1.21.17",
    "com.unity.cinemachine": "2.9.7",
    "com.unity.profiling.core": "1.0.2",
    "com.unity.test-runner": "1.1.33",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.ugui": "1.0.0",
    "com.unity.visualscripting": "1.8.0"
  }
}
```

### Development Tools Setup

#### Git Configuration
```bash
# Configure Git LFS for Unity assets
git lfs install
git lfs track "*.unity"
git lfs track "*.asset"
git lfs track "*.prefab"
git lfs track "*.mat"
git lfs track "*.png"
git lfs track "*.jpg"
git lfs track "*.psd"
git lfs track "*.fbx"
git lfs track "*.wav"
git lfs track "*.mp3"
git lfs track "*.ogg"

# Update .gitattributes
git add .gitattributes
git commit -m "Configure Unity LFS tracking"
```

#### IDE Setup (Visual Studio/Rider)
1. **Code Style**: Import Unity coding conventions
2. **Extensions**: Unity plugin, Git integration
3. **Debugger**: Unity remote debugging enabled

---

## Project Structure Analysis

### Current Structure Issues
```
Assets/
├── Code/
│   ├── MVP/ ❌ (30% dead code)
│   ├── *.backup ❌ (12 backup files)
│   └── Mixed responsibilities ❌
├── AmplifyShaderEditor/ ⚠️ (External dependency)
└── Resources/ ⚠️ (Not optimized)
```

### Proposed New Structure
```
Assets/
├── _Project/
│   ├── 00_Core/
│   │   ├── Managers/
│   │   ├── Systems/
│   │   └── Utilities/
│   ├── 01_GameLogic/
│   │   ├── Board/
│   │   ├── Pieces/
│   │   ├── Rules/
│   │   └── States/
│   ├── 02_Input/
│   │   ├── Handlers/
│   │   ├── Interfaces/
│   │   └── Processors/
│   ├── 03_AI/
│   │   ├── Strategies/
│   │   ├── Evaluation/
│   │   └── Algorithms/
│   ├── 04_UI/
│   │   ├── Screens/
│   │   ├── Components/
│   │   └── Data/
│   ├── 05_Audio/
│   ├── 06_Art/
│   │   ├── Materials/
│   │   ├── Textures/
│   │   └── Models/
│   └── 99_Testing/
├── Plugins/
│   └── AmplifyShaderEditor/
└── StreamingAssets/
```

### Migration Path Setup

**Create folder structure with this script:**

```csharp
// Assets/Editor/ProjectStructureMigrator.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public static class ProjectStructureMigrator
{
    private static readonly string[] FoldersToCreate = {
        "Assets/_Project",
        "Assets/_Project/00_Core/Managers",
        "Assets/_Project/00_Core/Systems", 
        "Assets/_Project/00_Core/Utilities",
        "Assets/_Project/01_GameLogic/Board",
        "Assets/_Project/01_GameLogic/Pieces",
        "Assets/_Project/01_GameLogic/Rules",
        "Assets/_Project/01_GameLogic/States",
        "Assets/_Project/02_Input/Handlers",
        "Assets/_Project/02_Input/Interfaces",
        "Assets/_Project/02_Input/Processors",
        "Assets/_Project/03_AI/Strategies",
        "Assets/_Project/03_AI/Evaluation",
        "Assets/_Project/03_AI/Algorithms",
        "Assets/_Project/04_UI/Screens",
        "Assets/_Project/04_UI/Components",
        "Assets/_Project/04_UI/Data",
        "Assets/_Project/05_Audio",
        "Assets/_Project/06_Art/Materials",
        "Assets/_Project/06_Art/Textures",
        "Assets/_Project/06_Art/Models",
        "Assets/_Project/99_Testing"
    };

    [MenuItem("WallChess/Setup/Create Project Structure")]
    public static void CreateProjectStructure()
    {
        foreach (string folder in FoldersToCreate)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                
                // Create .gitkeep to ensure folders are tracked
                string gitkeepPath = Path.Combine(folder, ".gitkeep");
                File.WriteAllText(gitkeepPath, "");
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"Created {FoldersToCreate.Length} project folders");
    }
}
```

### Naming Conventions

#### Scripts
- **Classes**: PascalCase (`GameStateManager`)
- **Methods**: PascalCase (`InitializeBoard`)
- **Fields**: camelCase with underscore prefix (`_currentState`)
- **Properties**: PascalCase (`CurrentPlayer`)
- **Constants**: UPPER_SNAKE_CASE (`MAX_BOARD_SIZE`)

#### Assets
- **Prefabs**: `PF_ComponentName` (`PF_ChessPiece`)
- **Materials**: `MAT_MaterialName` (`MAT_ChessPiece`)
- **Textures**: `TEX_TextureName` (`TEX_BoardTile`)
- **Audio**: `SFX_SoundName` (`SFX_PieceMove`)
- **Scenes**: `SC_SceneName` (`SC_MainMenu`)

---

## Backup & Safety Procedures

### Complete Project Backup

**Automated backup script:**

```csharp
// Assets/Editor/ProjectBackupTool.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Diagnostics;

public class ProjectBackupTool : EditorWindow
{
    private string backupPath = "";
    private bool includeLibrary = false;
    
    [MenuItem("WallChess/Tools/Project Backup")]
    public static void ShowWindow()
    {
        GetWindow<ProjectBackupTool>("Project Backup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Project Backup Tool", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        backupPath = EditorGUILayout.TextField("Backup Path:", backupPath);
        
        if (GUILayout.Button("Select Backup Folder"))
        {
            backupPath = EditorUtility.OpenFolderPanel("Select Backup Location", "", "");
        }
        
        includeLibrary = EditorGUILayout.Toggle("Include Library Folder", includeLibrary);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Backup"))
        {
            CreateBackup();
        }
        
        if (GUILayout.Button("Quick Backup (Desktop)"))
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            backupPath = Path.Combine(desktop, "WallChessBackups");
            CreateBackup();
        }
    }
    
    private void CreateBackup()
    {
        if (string.IsNullOrEmpty(backupPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a backup path", "OK");
            return;
        }
        
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string projectName = Path.GetFileName(Application.dataPath.Replace("/Assets", ""));
        string fullBackupPath = Path.Combine(backupPath, $"{projectName}_backup_{timestamp}");
        
        try
        {
            Directory.CreateDirectory(fullBackupPath);
            
            // Copy essential folders
            CopyDirectory(Application.dataPath, Path.Combine(fullBackupPath, "Assets"));
            CopyDirectory(Path.Combine(Application.dataPath, "../ProjectSettings"), 
                         Path.Combine(fullBackupPath, "ProjectSettings"));
            CopyDirectory(Path.Combine(Application.dataPath, "../Packages"), 
                         Path.Combine(fullBackupPath, "Packages"));
            
            if (includeLibrary)
            {
                CopyDirectory(Path.Combine(Application.dataPath, "../Library"), 
                             Path.Combine(fullBackupPath, "Library"));
            }
            
            EditorUtility.DisplayDialog("Success", 
                $"Backup created successfully at:\n{fullBackupPath}", "OK");
            
            Process.Start(fullBackupPath);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", 
                $"Backup failed: {e.Message}", "OK");
        }
    }
    
    private static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source)) return;
        
        Directory.CreateDirectory(destination);
        
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(source, file);
            string destFile = Path.Combine(destination, relativePath);
            
            Directory.CreateDirectory(Path.GetDirectoryName(destFile));
            File.Copy(file, destFile, true);
        }
    }
}
```

### Git Branching Strategy

```bash
# Create main refactoring branch
git checkout -b refactor/architecture-improvement
git push -u origin refactor/architecture-improvement

# Create feature branches for each major refactor
git checkout -b feature/state-management-consolidation
git checkout -b feature/grid-system-simplification
git checkout -b feature/object-pooling
git checkout -b feature/god-object-extraction
git checkout -b feature/input-system-unification
git checkout -b feature/ai-system-decoupling
```

### Emergency Rollback Procedure

**Rollback UI Tool:**

```csharp
// Assets/Editor/EmergencyRollback.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public class EmergencyRollback : EditorWindow
{
    [MenuItem("WallChess/Emergency/Rollback Options")]
    public static void ShowWindow()
    {
        GetWindow<EmergencyRollback>("Emergency Rollback");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Emergency Rollback", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "Use these options if a refactor goes wrong!", 
            MessageType.Warning);
        
        if (GUILayout.Button("Git - Reset to Last Commit"))
        {
            if (EditorUtility.DisplayDialog("Confirm Rollback", 
                "This will reset ALL changes to the last commit. Continue?", 
                "Yes", "Cancel"))
            {
                System.Diagnostics.Process.Start("git", "reset --hard HEAD");
            }
        }
        
        if (GUILayout.Button("Git - Show Recent Commits"))
        {
            System.Diagnostics.Process.Start("git", "log --oneline -10");
        }
        
        if (GUILayout.Button("Open Backup Folder"))
        {
            string desktop = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Desktop);
            string backupPath = Path.Combine(desktop, "WallChessBackups");
            
            if (Directory.Exists(backupPath))
                System.Diagnostics.Process.Start(backupPath);
            else
                EditorUtility.DisplayDialog("Error", "No backup folder found", "OK");
        }
    }
}
```

---

## Development Workflow Setup

### Unity Editor Configuration

**Essential Editor Settings:**
1. **Edit → Preferences → External Tools**
   - Set External Script Editor to VS/Rider
   - Enable "Editor Attaching" 

2. **Edit → Project Settings → Player**
   - Configuration: Master
   - Scripting Backend: IL2CPP
   - Api Compatibility: .NET Standard 2.1

3. **Edit → Project Settings → Physics**
   - Enable "Reuse Collision Callbacks"
   - Set "Default Solver Iterations": 8

### Code Style Configuration

**Create `.editorconfig` in project root:**

```ini
# .editorconfig
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

# Unity C# conventions
dotnet_naming_rule.unity_serialized_field.symbols = unity_serialized_field
dotnet_naming_rule.unity_serialized_field.style = camelcase_underscore_prefix
dotnet_naming_rule.unity_serialized_field.severity = warning

dotnet_naming_symbols.unity_serialized_field.applicable_kinds = field
dotnet_naming_symbols.unity_serialized_field.applicable_accessibilities = *
dotnet_naming_symbols.unity_serialized_field.required_modifiers = 

dotnet_naming_style.camelcase_underscore_prefix.capitalization = camel_case
dotnet_naming_style.camelcase_underscore_prefix.required_prefix = _
```

### Performance Profiling Setup

**Profiler Markers Implementation:**

```csharp
// Assets/_Project/00_Core/Utilities/ProfilingMarkers.cs
using Unity.Profiling;

public static class WallChessProfilerMarkers
{
    public static readonly ProfilerMarker GameStateUpdate = 
        new ProfilerMarker("WallChess.GameState.Update");
    
    public static readonly ProfilerMarker BoardValidation = 
        new ProfilerMarker("WallChess.Board.ValidateMove");
    
    public static readonly ProfilerMarker AIDecision = 
        new ProfilerMarker("WallChess.AI.MakeDecision");
    
    public static readonly ProfilerMarker UIUpdate = 
        new ProfilerMarker("WallChess.UI.UpdateDisplay");
    
    public static readonly ProfilerMarker RenderingUpdate = 
        new ProfilerMarker("WallChess.Rendering.UpdateVisuals");
}

// Usage example:
// using (WallChessProfilerMarkers.GameStateUpdate.Auto())
// {
//     // Your performance-critical code here
// }
```

### Debug Configuration

**Debug Manager System:**

```csharp
// Assets/_Project/00_Core/Utilities/DebugManager.cs
using UnityEngine;
using System;

public class DebugManager : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    public bool enablePerformanceLogging = false;
    public bool enableStateLogging = false;
    public bool enableUIDebug = false;
    
    private static DebugManager _instance;
    public static DebugManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DebugManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DebugManager");
                    _instance = go.AddComponent<DebugManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    public static void Log(string message, LogType type = LogType.Log)
    {
        if (!Instance.enableDebugLogs) return;
        
        string formattedMessage = $"[WallChess] {DateTime.Now:HH:mm:ss} - {message}";
        
        switch (type)
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
    
    public static void LogPerformance(string operation, float timeMs)
    {
        if (Instance.enablePerformanceLogging)
        {
            Log($"PERF: {operation} took {timeMs:F2}ms");
        }
    }
    
    public static void LogState(string stateName, string details)
    {
        if (Instance.enableStateLogging)
        {
            Log($"STATE: {stateName} - {details}");
        }
    }
}
```

---

## Pre-Refactor Validation

### Code Health Analysis

**Automated code health analyzer:**

```csharp
// Assets/Editor/CodeHealthAnalyzer.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class CodeHealthAnalyzer : EditorWindow
{
    private Vector2 scrollPosition;
    private CodeHealthReport report;
    
    [MenuItem("WallChess/Analysis/Code Health")]
    public static void ShowWindow()
    {
        GetWindow<CodeHealthAnalyzer>("Code Health");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Code Health Analysis", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Analyze Codebase"))
        {
            AnalyzeCodebase();
        }
        
        if (report != null)
        {
            DrawReport();
        }
    }
    
    private void AnalyzeCodebase()
    {
        report = new CodeHealthReport();
        
        string[] scriptPaths = Directory.GetFiles(
            Path.Combine(Application.dataPath, "Code"), 
            "*.cs", 
            SearchOption.AllDirectories);
        
        foreach (string path in scriptPaths)
        {
            AnalyzeScript(path);
        }
        
        report.GenerateRecommendations();
    }
    
    private void AnalyzeScript(string filePath)
    {
        string content = File.ReadAllText(filePath);
        string fileName = Path.GetFileName(filePath);
        
        // God class detection (>500 lines)
        int lineCount = content.Split('\n').Length;
        if (lineCount > 500)
        {
            report.GodClasses.Add($"{fileName}: {lineCount} lines");
        }
        
        // Duplicate detection (method signatures)
        var methods = Regex.Matches(content, 
            @"(public|private|protected)\s+\w+\s+(\w+)\s*\(");
        
        foreach (Match method in methods)
        {
            string signature = method.Groups[2].Value;
            if (!report.MethodSignatures.ContainsKey(signature))
                report.MethodSignatures[signature] = 0;
            report.MethodSignatures[signature]++;
        }
        
        // TODO comment detection
        int todoCount = Regex.Matches(content, @"//\s*TODO", 
            RegexOptions.IgnoreCase).Count;
        if (todoCount > 0)
        {
            report.TodoComments.Add($"{fileName}: {todoCount} TODOs");
        }
        
        // Magic number detection
        var numbers = Regex.Matches(content, @"\b\d{2,}\b");
        report.MagicNumbers.AddRange(
            numbers.Cast<Match>()
                  .Select(m => $"{fileName}: {m.Value}")
                  .Distinct());
    }
    
    private void DrawReport()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // God Classes
        EditorGUILayout.LabelField("God Classes (>500 lines):", EditorStyles.boldLabel);
        foreach (string godClass in report.GodClasses)
        {
            EditorGUILayout.LabelField($"⚠️ {godClass}", EditorStyles.miniLabel);
        }
        
        // Method Duplicates
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Potential Duplicates:", EditorStyles.boldLabel);
        foreach (var method in report.MethodSignatures.Where(m => m.Value > 1))
        {
            EditorGUILayout.LabelField($"🔄 {method.Key}: {method.Value} instances", 
                EditorStyles.miniLabel);
        }
        
        // Recommendations
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Recommendations:", EditorStyles.boldLabel);
        foreach (string recommendation in report.Recommendations)
        {
            EditorGUILayout.LabelField($"💡 {recommendation}", EditorStyles.wordWrappedMiniLabel);
        }
        
        EditorGUILayout.EndScrollView();
    }
}

[System.Serializable]
public class CodeHealthReport
{
    public System.Collections.Generic.List<string> GodClasses = 
        new System.Collections.Generic.List<string>();
    public System.Collections.Generic.Dictionary<string, int> MethodSignatures = 
        new System.Collections.Generic.Dictionary<string, int>();
    public System.Collections.Generic.List<string> TodoComments = 
        new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<string> MagicNumbers = 
        new System.Collections.Generic.List<string>();
    public System.Collections.Generic.List<string> Recommendations = 
        new System.Collections.Generic.List<string>();
    
    public void GenerateRecommendations()
    {
        if (GodClasses.Count > 0)
            Recommendations.Add($"Consider breaking down {GodClasses.Count} large classes");
        
        if (TodoComments.Count > 10)
            Recommendations.Add($"Address {TodoComments.Count} TODO comments before refactoring");
        
        if (MagicNumbers.Count > 20)
            Recommendations.Add("Extract magic numbers to constants");
    }
}
```

### Performance Baseline Recording

**Performance baseline recorder:**

```csharp
// Assets/_Project/00_Core/Utilities/PerformanceBaseline.cs
using UnityEngine;
using Unity.Profiling;
using System.Collections.Generic;
using System.IO;

public class PerformanceBaseline : MonoBehaviour
{
    [Header("Baseline Settings")]
    public int framesToRecord = 300; // 5 seconds at 60fps
    public bool recordOnStart = false;
    
    private List<FrameData> frameData = new List<FrameData>();
    private int currentFrame = 0;
    private bool isRecording = false;
    
    private ProfilerRecorder mainThreadTimeRecorder;
    private ProfilerRecorder renderTimeRecorder;
    private ProfilerRecorder gcMemoryRecorder;
    
    void Start()
    {
        // Initialize profiler recorders
        mainThreadTimeRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal, "Main Thread", 15);
        renderTimeRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Render, "Gfx.WaitForRenderThread", 15);
        gcMemoryRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Memory, "GC Reserved Memory", 15);
        
        if (recordOnStart)
            StartRecording();
    }
    
    void Update()
    {
        if (!isRecording) return;
        
        // Record frame data
        frameData.Add(new FrameData
        {
            frameNumber = currentFrame,
            deltaTime = Time.deltaTime,
            fps = 1f / Time.deltaTime,
            mainThreadTime = GetRecorderFrameAverage(mainThreadTimeRecorder) * 1e-6f,
            renderTime = GetRecorderFrameAverage(renderTimeRecorder) * 1e-6f,
            gcMemory = gcMemoryRecorder.LastValue
        });
        
        currentFrame++;
        
        if (currentFrame >= framesToRecord)
        {
            StopRecording();
            SaveBaseline();
        }
    }
    
    [ContextMenu("Start Recording")]
    public void StartRecording()
    {
        frameData.Clear();
        currentFrame = 0;
        isRecording = true;
        Debug.Log("Started performance baseline recording...");
    }
    
    [ContextMenu("Stop Recording")]
    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"Stopped recording. Captured {frameData.Count} frames");
    }
    
    private void SaveBaseline()
    {
        var baseline = new PerformanceBaselineData
        {
            recordingDate = System.DateTime.Now.ToString(),
            averageFPS = CalculateAverageFPS(),
            averageFrameTime = CalculateAverageFrameTime(),
            maxFrameTime = CalculateMaxFrameTime(),
            gcMemoryPeak = CalculateGCMemoryPeak(),
            frames = frameData
        };
        
        string json = JsonUtility.ToJson(baseline, true);
        string path = Path.Combine(Application.persistentDataPath, 
            "performance_baseline.json");
        
        File.WriteAllText(path, json);
        Debug.Log($"Performance baseline saved to: {path}");
        
        // Also log to console for immediate viewing
        Debug.Log($"BASELINE - AVG FPS: {baseline.averageFPS:F1}, " +
                 $"AVG Frame: {baseline.averageFrameTime:F2}ms, " +
                 $"MAX Frame: {baseline.maxFrameTime:F2}ms");
    }
    
    private float CalculateAverageFPS()
    {
        return frameData.Count > 0 ? frameData.Average(f => f.fps) : 0f;
    }
    
    private float CalculateAverageFrameTime()
    {
        return frameData.Count > 0 ? frameData.Average(f => f.deltaTime * 1000f) : 0f;
    }
    
    private float CalculateMaxFrameTime()
    {
        return frameData.Count > 0 ? frameData.Max(f => f.deltaTime * 1000f) : 0f;
    }
    
    private long CalculateGCMemoryPeak()
    {
        return frameData.Count > 0 ? frameData.Max(f => f.gcMemory) : 0L;
    }
    
    private double GetRecorderFrameAverage(ProfilerRecorder recorder)
    {
        var samplesCount = recorder.Capacity;
        if (samplesCount == 0)
            return 0;

        double r = 0;
        unsafe
        {
            var samples = stackalloc ProfilerRecorderSample[samplesCount];
            recorder.CopyTo(samples, samplesCount);
            for (var i = 0; i < samplesCount; ++i)
                r += samples[i].Value;
            r /= samplesCount;
        }

        return r;
    }
    
    void OnDestroy()
    {
        mainThreadTimeRecorder.Dispose();
        renderTimeRecorder.Dispose();
        gcMemoryRecorder.Dispose();
    }
}

[System.Serializable]
public class FrameData
{
    public int frameNumber;
    public float deltaTime;
    public float fps;
    public float mainThreadTime;
    public float renderTime;
    public long gcMemory;
}

[System.Serializable]
public class PerformanceBaselineData
{
    public string recordingDate;
    public float averageFPS;
    public float averageFrameTime;
    public float maxFrameTime;
    public long gcMemoryPeak;
    public List<FrameData> frames;
}

// Extension method for LINQ operations
public static class FrameDataExtensions
{
    public static float Average(this List<FrameData> frames, System.Func<FrameData, float> selector)
    {
        if (frames.Count == 0) return 0f;
        
        float sum = 0f;
        foreach (var frame in frames)
        {
            sum += selector(frame);
        }
        return sum / frames.Count;
    }
    
    public static float Max(this List<FrameData> frames, System.Func<FrameData, float> selector)
    {
        if (frames.Count == 0) return 0f;
        
        float max = float.MinValue;
        foreach (var frame in frames)
        {
            float value = selector(frame);
            if (value > max) max = value;
        }
        return max;
    }
    
    public static long Max(this List<FrameData> frames, System.Func<FrameData, long> selector)
    {
        if (frames.Count == 0) return 0L;
        
        long max = long.MinValue;
        foreach (var frame in frames)
        {
            long value = selector(frame);
            if (value > max) max = value;
        }
        return max;
    }
}
```

### Functionality Verification Tests

**Behavior documentation generator:**

```csharp
// Assets/Editor/BehaviorDocumentationGenerator.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;

public class BehaviorDocumentationGenerator : EditorWindow
{
    [MenuItem("WallChess/Analysis/Generate Behavior Documentation")]
    public static void ShowWindow()
    {
        GetWindow<BehaviorDocumentationGenerator>("Behavior Documentation");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Behavior Documentation Generator", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "This tool documents current behavior before refactoring", 
            MessageType.Info);
        
        if (GUILayout.Button("Generate Documentation"))
        {
            GenerateDocumentation();
        }
        
        if (GUILayout.Button("Open Documentation Folder"))
        {
            string path = Path.Combine(Application.dataPath, "..", "Documentation");
            if (Directory.Exists(path))
                System.Diagnostics.Process.Start(path);
        }
    }
    
    private void GenerateDocumentation()
    {
        string docPath = Path.Combine(Application.dataPath, "..", "Documentation");
        Directory.CreateDirectory(docPath);
        
        var sb = new StringBuilder();
        sb.AppendLine("# WallChessQuidor Current Behavior Documentation");
        sb.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        
        // Document game manager behavior
        DocumentGameManager(sb);
        
        // Document input system behavior  
        DocumentInputSystem(sb);
        
        // Document AI behavior
        DocumentAISystem(sb);
        
        // Document UI behavior
        DocumentUISystem(sb);
        
        string filePath = Path.Combine(docPath, "current-behavior.md");
        File.WriteAllText(filePath, sb.ToString());
        
        EditorUtility.DisplayDialog("Success", 
            $"Behavior documentation generated at:\n{filePath}", "OK");
    }
    
    private void DocumentGameManager(StringBuilder sb)
    {
        sb.AppendLine("## Game Manager Behavior");
        
        // Find GameManager in scene
        var gameManager = FindObjectOfType<MonoBehaviour>();
        if (gameManager != null && gameManager.GetType().Name.Contains("GameManager"))
        {
            Type type = gameManager.GetType();
            
            sb.AppendLine($"**Class**: {type.Name}");
            sb.AppendLine($"**Location**: {type.Assembly.GetName().Name}");
            sb.AppendLine();
            
            // Document public methods
            sb.AppendLine("### Public Methods:");
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                             .Where(m => m.DeclaringType == type)
                             .ToArray();
            
            foreach (var method in methods)
            {
                sb.AppendLine($"- `{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})`");
            }
            
            // Document public fields/properties
            sb.AppendLine();
            sb.AppendLine("### Public Properties:");
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Where(p => p.DeclaringType == type)
                               .ToArray();
            
            foreach (var property in properties)
            {
                sb.AppendLine($"- `{property.PropertyType.Name} {property.Name}`");
            }
        }
        
        sb.AppendLine();
    }
    
    private void DocumentInputSystem(StringBuilder sb)
    {
        sb.AppendLine("## Input System Behavior");
        sb.AppendLine("- Mouse click detection for piece selection");
        sb.AppendLine("- Touch input for mobile devices"); 
        sb.AppendLine("- Keyboard shortcuts for game controls");
        sb.AppendLine("- Drag and drop for piece movement");
        sb.AppendLine();
    }
    
    private void DocumentAISystem(StringBuilder sb)
    {
        sb.AppendLine("## AI System Behavior");
        sb.AppendLine("- Minimax algorithm with alpha-beta pruning");
        sb.AppendLine("- Configurable search depth");
        sb.AppendLine("- Position evaluation based on piece values");
        sb.AppendLine("- Wall placement evaluation");
        sb.AppendLine();
    }
    
    private void DocumentUISystem(StringBuilder sb)
    {
        sb.AppendLine("## UI System Behavior");
        sb.AppendLine("- Game board visualization");
        sb.AppendLine("- Piece movement highlights");
        sb.AppendLine("- Turn indicator display");
        sb.AppendLine("- Move history tracking");
        sb.AppendLine("- Game state notifications");
        sb.AppendLine();
    }
}
```

---

## Tool Setup

### Custom Unity Profiler Module

```csharp
// Assets/_Project/00_Core/Utilities/WallChessProfilerModule.cs
using Unity.Profiling;
using Unity.Profiling.Editor;

[System.Serializable]
[ProfilerModuleMetadata("WallChess Game")]
public class WallChessProfilerModule : ProfilerModule
{
    static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
    {
        new ProfilerCounterDescriptor("Active Pieces", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("Board Updates", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("AI Calculations", ProfilerCategory.Scripts),
        new ProfilerCounterDescriptor("UI Updates", ProfilerCategory.Scripts),
    };

    public WallChessProfilerModule() : base(k_Counters) { }
}
```

### Runtime Performance Monitor

```csharp
// Assets/_Project/00_Core/Utilities/RuntimePerformanceMonitor.cs
using UnityEngine;
using TMPro;

public class RuntimePerformanceMonitor : MonoBehaviour
{
    [Header("Display Settings")]
    public TextMeshProUGUI performanceText;
    public bool showInBuild = false;
    
    [Header("Thresholds")]
    public float targetFPS = 60f;
    public float warningThreshold = 45f;
    public float criticalThreshold = 30f;
    
    private float _deltaTime = 0.0f;
    private float _currentFPS = 0.0f;
    private int _frameCount = 0;
    private float _updateInterval = 0.5f;
    private float _accumulatedTime = 0.0f;
    
    void Start()
    {
        if (!Debug.isDebugBuild && !showInBuild)
        {
            gameObject.SetActive(false);
            return;
        }
        
        if (performanceText == null)
        {
            // Create UI text if not assigned
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                GameObject textObj = new GameObject("PerformanceMonitor");
                textObj.transform.SetParent(canvas.transform);
                
                var rectTransform = textObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(10, -10);
                rectTransform.sizeDelta = new Vector2(200, 100);
                
                performanceText = textObj.AddComponent<TextMeshProUGUI>();
                performanceText.fontSize = 14;
                performanceText.color = Color.white;
            }
        }
    }
    
    void Update()
    {
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        _accumulatedTime += Time.unscaledDeltaTime;
        _frameCount++;
        
        if (_accumulatedTime >= _updateInterval)
        {
            _currentFPS = _frameCount / _accumulatedTime;
            _frameCount = 0;
            _accumulatedTime = 0.0f;
            
            UpdateDisplay();
        }
    }
    
    private void UpdateDisplay()
    {
        if (performanceText == null) return;
        
        float msec = _deltaTime * 1000.0f;
        Color textColor = GetPerformanceColor();
        
        performanceText.text = string.Format(
            "FPS: {0:F1}\nFrame: {1:F2}ms\nMemory: {2:F1}MB",
            _currentFPS,
            msec,
            (System.GC.GetTotalMemory(false) / 1048576.0f)
        );
        
        performanceText.color = textColor;
        
        // Log performance warnings
        if (_currentFPS < criticalThreshold)
        {
            DebugManager.Log($"CRITICAL: FPS dropped to {_currentFPS:F1}", LogType.Error);
        }
        else if (_currentFPS < warningThreshold)
        {
            DebugManager.Log($"WARNING: FPS at {_currentFPS:F1}", LogType.Warning);
        }
    }
    
    private Color GetPerformanceColor()
    {
        if (_currentFPS >= targetFPS)
            return Color.green;
        else if (_currentFPS >= warningThreshold)
            return Color.yellow;
        else if (_currentFPS >= criticalThreshold)
            return Color.red;
        else
            return Color.magenta;
    }
}
```

### Static Code Analysis Tool

```csharp
// Assets/Editor/CodeQualityAnalyzer.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public class CodeQualityAnalyzer : EditorWindow
{
    private Vector2 scrollPosition;
    private List<CodeIssue> issues = new List<CodeIssue>();
    
    [MenuItem("WallChess/Analysis/Code Quality")]
    public static void ShowWindow()
    {
        GetWindow<CodeQualityAnalyzer>("Code Quality");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Code Quality Analysis", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Analyze Code Quality"))
        {
            AnalyzeCodeQuality();
        }
        
        if (GUILayout.Button("Export Report"))
        {
            ExportReport();
        }
        
        if (issues.Count > 0)
        {
            DrawIssues();
        }
    }
    
    private void AnalyzeCodeQuality()
    {
        issues.Clear();
        
        string[] scriptPaths = Directory.GetFiles(
            Application.dataPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (string path in scriptPaths)
        {
            AnalyzeScript(path);
        }
        
        // Sort by severity
        issues = issues.OrderByDescending(i => (int)i.severity).ToList();
    }
    
    private void AnalyzeScript(string filePath)
    {
        string content = File.ReadAllText(filePath);
        string relativePath = Path.GetRelativePath(Application.dataPath, filePath);
        
        // Check for common Unity anti-patterns
        CheckForFindObjectsUsage(content, relativePath);
        CheckForStringComparisons(content, relativePath);
        CheckForUnityNullChecks(content, relativePath);
        CheckForUpdatePerformance(content, relativePath);
        CheckForMemoryAllocations(content, relativePath);
    }
    
    private void CheckForFindObjectsUsage(string content, string filePath)
    {
        var matches = Regex.Matches(content, @"Find(Object|Objects)(OfType|WithTag)");
        foreach (Match match in matches)
        {
            issues.Add(new CodeIssue
            {
                type = "Performance",
                severity = IssueSeverity.Warning,
                file = filePath,
                description = $"Found {match.Value} - consider caching references",
                line = GetLineNumber(content, match.Index)
            });
        }
    }
    
    private void CheckForStringComparisons(string content, string filePath)
    {
        var matches = Regex.Matches(content, @"\.tag\s*==\s*""");
        foreach (Match match in matches)
        {
            issues.Add(new CodeIssue
            {
                type = "Performance", 
                severity = IssueSeverity.Info,
                file = filePath,
                description = "Use CompareTag() instead of tag == \"\"",
                line = GetLineNumber(content, match.Index)
            });
        }
    }
    
    private void CheckForUnityNullChecks(string content, string filePath)
    {
        var matches = Regex.Matches(content, @"!=\s*null.*MonoBehaviour");
        foreach (Match match in matches)
        {
            issues.Add(new CodeIssue
            {
                type = "Best Practice",
                severity = IssueSeverity.Info,
                file = filePath,
                description = "Consider null-conditional operators for Unity objects",
                line = GetLineNumber(content, match.Index)
            });
        }
    }
    
    private void CheckForUpdatePerformance(string content, string filePath)
    {
        if (content.Contains("void Update()"))
        {
            // Check for expensive operations in Update
            string[] expensiveOps = { "Find", "GetComponent", "Instantiate", "Resources.Load" };
            
            foreach (string op in expensiveOps)
            {
                if (content.Contains(op))
                {
                    issues.Add(new CodeIssue
                    {
                        type = "Performance",
                        severity = IssueSeverity.Warning,
                        file = filePath,
                        description = $"Expensive operation '{op}' found in Update method",
                        line = -1 // Would need more complex parsing for exact line
                    });
                }
            }
        }
    }
    
    private void CheckForMemoryAllocations(string content, string filePath)
    {
        var matches = Regex.Matches(content, @"new\s+\w+\[");
        if (matches.Count > 5)
        {
            issues.Add(new CodeIssue
            {
                type = "Memory",
                severity = IssueSeverity.Warning,
                file = filePath,
                description = $"Multiple array allocations ({matches.Count}) - consider object pooling",
                line = -1
            });
        }
    }
    
    private int GetLineNumber(string content, int charIndex)
    {
        return content.Substring(0, charIndex).Count(c => c == '\n') + 1;
    }
    
    private void DrawIssues()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        var groupedIssues = issues.GroupBy(i => i.severity);
        
        foreach (var group in groupedIssues)
        {
            EditorGUILayout.LabelField($"{group.Key} Issues ({group.Count()})", 
                EditorStyles.boldLabel);
            
            foreach (var issue in group)
            {
                EditorGUILayout.BeginHorizontal();
                
                string icon = GetSeverityIcon(issue.severity);
                EditorGUILayout.LabelField($"{icon} [{issue.type}]", 
                    GUILayout.Width(100));
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(issue.description, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField($"File: {issue.file} (Line: {issue.line})", 
                    EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private string GetSeverityIcon(IssueSeverity severity)
    {
        switch (severity)
        {
            case IssueSeverity.Critical: return "🔴";
            case IssueSeverity.Warning: return "🟡"; 
            case IssueSeverity.Info: return "🔵";
            default: return "⚪";
        }
    }
    
    private void ExportReport()
    {
        string path = EditorUtility.SaveFilePanel("Export Code Quality Report", 
            "", "code-quality-report.md", "md");
        
        if (string.IsNullOrEmpty(path)) return;
        
        var report = new System.Text.StringBuilder();
        report.AppendLine("# Code Quality Report");
        report.AppendLine($"Generated: {System.DateTime.Now}");
        report.AppendLine($"Total Issues: {issues.Count}");
        report.AppendLine();
        
        var groupedIssues = issues.GroupBy(i => i.severity);
        
        foreach (var group in groupedIssues)
        {
            report.AppendLine($"## {group.Key} Issues ({group.Count()})");
            report.AppendLine();
            
            foreach (var issue in group)
            {
                report.AppendLine($"**{issue.type}**: {issue.description}");
                report.AppendLine($"*File*: {issue.file} (Line: {issue.line})");
                report.AppendLine();
            }
        }
        
        File.WriteAllText(path, report.ToString());
        EditorUtility.DisplayDialog("Success", $"Report exported to {path}", "OK");
    }
}

[System.Serializable]
public class CodeIssue
{
    public string type;
    public IssueSeverity severity;
    public string file;
    public string description;
    public int line;
}

public enum IssueSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
```

### Refactoring Assistant Window

```csharp
// Assets/Editor/RefactoringAssistant.cs
using UnityEngine;
using UnityEditor;

public class RefactoringAssistant : EditorWindow
{
    private int currentPhase = 0;
    private bool[] phaseCompleted = new bool[9];
    
    private readonly string[] phaseNames = {
        "Project Setup",
        "Codebase Cleanup", 
        "State Management",
        "Object Pooling",
        "Grid System",
        "God Object Extraction",
        "Input Unification",
        "AI Decoupling",
        "Final Testing"
    };
    
    [MenuItem("WallChess/Tools/Refactoring Assistant")]
    public static void ShowWindow()
    {
        var window = GetWindow<RefactoringAssistant>("Refactoring Assistant");
        window.minSize = new Vector2(400, 600);
    }
    
    void OnGUI()
    {
        GUILayout.Label("WallChess Refactoring Assistant", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "This tool guides you through the refactoring process step by step.", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Progress overview
        DrawProgressOverview();
        
        EditorGUILayout.Space();
        
        // Current phase details
        DrawCurrentPhase();
        
        EditorGUILayout.Space();
        
        // Navigation buttons
        DrawNavigationButtons();
    }
    
    private void DrawProgressOverview()
    {
        GUILayout.Label("Refactoring Progress", EditorStyles.boldLabel);
        
        for (int i = 0; i < phaseNames.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Status icon
            string icon = phaseCompleted[i] ? "✅" : (i == currentPhase ? "🔄" : "⏳");
            GUILayout.Label(icon, GUILayout.Width(25));
            
            // Phase name
            GUIStyle style = i == currentPhase ? EditorStyles.boldLabel : EditorStyles.label;
            GUILayout.Label($"{i + 1}. {phaseNames[i]}", style);
            
            // Mark complete button
            if (i == currentPhase && !phaseCompleted[i])
            {
                if (GUILayout.Button("Mark Complete", GUILayout.Width(100)))
                {
                    phaseCompleted[i] = true;
                    if (currentPhase < phaseNames.Length - 1)
                        currentPhase++;
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void DrawCurrentPhase()
    {
        GUILayout.Label($"Current Phase: {phaseNames[currentPhase]}", EditorStyles.boldLabel);
        
        switch (currentPhase)
        {
            case 0: // Project Setup
                DrawPhaseSetup();
                break;
            case 1: // Codebase Cleanup
                DrawPhaseCleanup();
                break;
            case 2: // State Management
                DrawPhaseStateManagement();
                break;
            case 3: // Object Pooling
                DrawPhaseObjectPooling();
                break;
            case 4: // Grid System
                DrawPhaseGridSystem();
                break;
            case 5: // God Object Extraction
                DrawPhaseGodObject();
                break;
            case 6: // Input Unification
                DrawPhaseInput();
                break;
            case 7: // AI Decoupling
                DrawPhaseAI();
                break;
            case 8: // Final Testing
                DrawPhaseTesting();
                break;
        }
    }
    
    private void DrawPhaseSetup()
    {
        EditorGUILayout.HelpBox(
            "Set up your development environment and create project backups.", 
            MessageType.Info);
        
        EditorGUILayout.LabelField("Tasks:");
        EditorGUILayout.LabelField("• Create project backup");
        EditorGUILayout.LabelField("• Set up Git branches");
        EditorGUILayout.LabelField("• Install required packages");
        EditorGUILayout.LabelField("• Create folder structure");
        
        if (GUILayout.Button("Create Project Structure"))
        {
            ProjectStructureMigrator.CreateProjectStructure();
        }
        
        if (GUILayout.Button("Open Backup Tool"))
        {
            ProjectBackupTool.ShowWindow();
        }
    }
    
    private void DrawPhaseCleanup()
    {
        EditorGUILayout.HelpBox(
            "Clean up dead code and organize the codebase.", 
            MessageType.Warning);
        
        EditorGUILayout.LabelField("Tasks:");
        EditorGUILayout.LabelField("• Delete MVP folder");
        EditorGUILayout.LabelField("• Remove backup files");
        EditorGUILayout.LabelField("• Clean commented code");
        
        if (GUILayout.Button("Run Code Health Analysis"))
        {
            CodeHealthAnalyzer.ShowWindow();
        }
    }
    
    private void DrawPhaseStateManagement()
    {
        EditorGUILayout.HelpBox(
            "Consolidate the dual state management systems.", 
            MessageType.Warning);
        
        EditorGUILayout.LabelField("Reference: Part 2 of implementation guide");
        
        if (GUILayout.Button("Open Part 2 Guide"))
        {
            // Open documentation file
            string path = System.IO.Path.Combine(Application.dataPath, 
                "..", "refactoring-docs", "02-state-management-consolidation.md");
            if (System.IO.File.Exists(path))
                System.Diagnostics.Process.Start(path);
        }
    }
    
    private void DrawPhaseObjectPooling()
    {
        EditorGUILayout.HelpBox(
            "Implement object pooling for better performance.", 
            MessageType.Info);
        
        if (GUILayout.Button("Start Performance Recording"))
        {
            var recorder = FindObjectOfType<PerformanceBaseline>();
            if (recorder != null)
                recorder.StartRecording();
        }
    }
    
    private void DrawPhaseGridSystem()
    {
        EditorGUILayout.HelpBox(
            "Simplify the grid coordinate system.", 
            MessageType.Warning);
        
        EditorGUILayout.LabelField("Current: 3 coordinate systems");
        EditorGUILayout.LabelField("Target: 1 unified system");
    }
    
    private void DrawPhaseGodObject()
    {
        EditorGUILayout.HelpBox(
            "Extract specialized managers from GameManager.", 
            MessageType.Error);
        
        EditorGUILayout.LabelField("Target managers:");
        EditorGUILayout.LabelField("• GameStateManager");
        EditorGUILayout.LabelField("• InputHandler");
        EditorGUILayout.LabelField("• AICoordinator");
        EditorGUILayout.LabelField("• RenderingManager");
    }
    
    private void DrawPhaseInput()
    {
        EditorGUILayout.HelpBox(
            "Unify the input handling system.", 
            MessageType.Info);
        
        EditorGUILayout.LabelField("Target: Single input abstraction layer");
    }
    
    private void DrawPhaseAI()
    {
        EditorGUILayout.HelpBox(
            "Decouple AI system with interfaces.", 
            MessageType.Info);
        
        EditorGUILayout.LabelField("Target: Strategy pattern for AI");
    }
    
    private void DrawPhaseTesting()
    {
        EditorGUILayout.HelpBox(
            "Final validation and testing.", 
            MessageType.Info);
        
        if (GUILayout.Button("Run Performance Comparison"))
        {
            // Compare with baseline
        }
    }
    
    private void DrawNavigationButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        if (currentPhase > 0)
        {
            if (GUILayout.Button("Previous Phase"))
            {
                currentPhase--;
            }
        }
        
        if (currentPhase < phaseNames.Length - 1)
        {
            if (GUILayout.Button("Next Phase"))
            {
                currentPhase++;
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
}
```

---

## Validation Checklist

### Pre-Refactor Checklist
- [ ] All required packages added via Package Manager
- [ ] Git repository configured with LFS
- [ ] Project backup created and verified
- [ ] Development branch created (`refactor/architecture-improvement`)
- [ ] Folder structure created using `ProjectStructureMigrator`
- [ ] Code health analysis completed
- [ ] Performance baseline recorded
- [ ] Behavior documentation generated
- [ ] All custom tools installed and tested

### Environment Validation
- [ ] Unity editor starts without errors
- [ ] All scenes load correctly
- [ ] Game plays normally (baseline behavior)
- [ ] Performance metrics recorded
- [ ] No compilation errors
- [ ] Git status clean

### Tool Validation  
- [ ] ProjectBackupTool creates valid backups
- [ ] CodeHealthAnalyzer runs without errors
- [ ] PerformanceBaseline records metrics
- [ ] RefactoringAssistant opens correctly
- [ ] All editor windows accessible via menu

### Safety Validation
- [ ] Emergency rollback procedure tested
- [ ] Git branches created and accessible
- [ ] Backup restoration tested
- [ ] Performance monitor displays correctly
- [ ] Debug manager logs events

---

**Next Steps:**
1. Complete all checklist items above
2. Proceed to [Part 6: Codebase Cleanup](06-codebase-cleanup.md) for immediate wins
3. Then follow [Part 2: State Management Consolidation](02-state-management-consolidation.md)

---

**Page 8 of 8**  
**Next Document:** [Part 6: Codebase Cleanup](06-codebase-cleanup.md)  
**Master Index:** [00-master-index.md](00-master-index.md)  

---
*WallChessQuidor Refactoring Implementation Guide - Generated by unity-indie-architect agent*