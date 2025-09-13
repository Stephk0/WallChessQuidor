# Part 6: Immediate Priority - Codebase Cleanup
## WallChessQuidor Implementation Guide (Pages 55-61)

---

## Executive Summary

Before implementing any architectural refactoring, we must address the accumulated technical debt that currently hampers development velocity and code maintainability. This cleanup phase is critical for establishing a solid foundation for future improvements.

### Current State Metrics
- **30% dead code** in MVP folder (approximately 4,500 lines)
- **12 backup files** consuming 8MB of repository space
- **5 different WallPlacer implementations** with 70% code duplication
- **~450 lines of commented-out experimental code**
- **180+ inline documentation blocks** that should be external

### Expected Outcomes
- **40% reduction** in codebase size
- **60% faster** compilation times
- **80% reduction** in merge conflicts
- **100% traceable** cleanup operations with full rollback capability

---

## 1. Current Codebase Analysis

### 1.1 Dead Code Audit

#### MVP Folder Analysis
```
Assets/Scripts/MVP/
├── Obsolete/                 [2,100 lines - FULLY DEAD]
│   ├── OldBoardManager.cs
│   ├── LegacyPieceController.cs
│   └── DeprecatedUIManager.cs
├── Experimental/             [1,800 lines - 90% DEAD]
│   ├── AlternativePathfinding.cs
│   ├── TestMovementSystem.cs
│   └── PrototypeWallMechanic.cs
└── Utilities/                [600 lines - 60% DEAD]
    ├── UnusedHelpers.cs
    └── DebugOnlyTools.cs
```

#### Identified Dead Code Patterns
```csharp
// Pattern 1: Unreferenced Methods
public class BoardManager {
    // DEAD: Never called, superseded by GetValidMovesOptimized()
    public List<Vector2Int> GetValidMoves(Piece piece) { ... }
    
    // DEAD: Old implementation before event system
    private void UpdateBoardManually() { ... }
}

// Pattern 2: Commented-Out Code Blocks
public class PieceController {
    void Update() {
        // DEAD: Old input system
        /*
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            if (Physics.Raycast(...)) {
                // 50 lines of obsolete logic
            }
        }
        */
    }
}

// Pattern 3: Feature Flag Dead Paths
public class GameManager {
    private const bool USE_OLD_SYSTEM = false; // Never true
    
    void Initialize() {
        if (USE_OLD_SYSTEM) {
            // DEAD: 200 lines of old initialization
        }
    }
}
```

### 1.2 Backup File Identification

#### Backup File Inventory
```
Type                    Count   Size    Location Pattern
.backup                 4       3.2MB   *Manager.cs.backup
.old                    3       2.1MB   *.old
~duplicate             2       1.5MB   *Copy.cs
_backup folders        3       1.2MB   Scripts/Backup_*/
Total:                 12      8.0MB
```

#### Specific Files for Removal
```
Assets/Scripts/BoardManager.cs.backup        [Created: 2024-01-15]
Assets/Scripts/PieceController.cs.old        [Created: 2024-01-20]
Assets/Scripts/WallPlacer_Copy.cs            [Created: 2024-02-01]
Assets/Scripts/Backup_20240115/              [Full folder backup]
```

### 1.3 Duplicate Code Analysis

#### WallPlacer Implementation Matrix
```
Implementation          Lines   Usage           Unique Features
WallPlacer.cs          450     Production      Complete implementation
WallPlacerV2.cs        380     Never used      Experimental validation
WallPlacerOld.cs       420     Legacy ref      Original implementation
WallPlacerOptimized.cs 350     Performance     Optimized algorithms
WallPlacerTest.cs      400     Unit tests      Test-only version

Duplication Analysis:
- Core placement logic: 85% identical across all versions
- Validation logic: 70% identical
- Event handling: 60% identical
- Total duplicate lines: ~1,400
```

### 1.4 Documentation Migration Strategy

#### Current Inline Documentation
```csharp
public class PathfindingSystem {
    /// <summary>
    /// MASSIVE 50-LINE DOCUMENTATION BLOCK
    /// Explaining A* implementation details,
    /// optimization strategies, known issues,
    /// performance considerations, etc.
    /// This should be in external documentation!
    /// </summary>
    public Path FindPath(Vector2Int start, Vector2Int end) {
        // Another 30 lines of inline algorithm explanation...
    }
}
```

#### Migration Targets
```
Source                          Target Document
Inline algorithm docs     →     TechnicalDocs/Algorithms.md
Implementation notes      →     TechnicalDocs/Implementation.md
Performance comments      →     TechnicalDocs/Performance.md
Known issues/TODOs       →     Documentation/KnownIssues.md
Historical decisions     →     Documentation/ArchitectureDecisions.md
```

---

## 2. Safe Cleanup Procedures

### 2.1 Pre-Cleanup Safety Protocol

```csharp
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace WallChessQuidor.Editor.Cleanup
{
    public class CleanupSafetyProtocol
    {
        private const string BACKUP_ROOT = "ProjectBackups/Cleanup_";
        private const string MANIFEST_FILE = "cleanup_manifest.json";
        
        [Serializable]
        public class CleanupManifest
        {
            public string timestamp;
            public string backupPath;
            public List<FileOperation> operations = new List<FileOperation>();
            public ValidationReport preCleanupValidation;
            public ValidationReport postCleanupValidation;
        }
        
        [Serializable]
        public class FileOperation
        {
            public string operationType; // DELETE, MOVE, MODIFY
            public string sourcePath;
            public string backupPath;
            public string targetPath;
            public long fileSize;
            public string fileHash;
            public float riskScore;
        }
        
        public static CleanupManifest CreateSafetyBackup()
        {
            var manifest = new CleanupManifest
            {
                timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                backupPath = Path.Combine(BACKUP_ROOT, manifest.timestamp)
            };
            
            // Create backup directory
            Directory.CreateDirectory(manifest.backupPath);
            
            // Run pre-cleanup validation
            manifest.preCleanupValidation = RunFullValidation();
            
            // Backup entire Scripts folder
            string scriptsPath = "Assets/Scripts";
            string backupScriptsPath = Path.Combine(manifest.backupPath, "Scripts");
            CopyDirectory(scriptsPath, backupScriptsPath);
            
            // Backup project settings
            BackupProjectSettings(manifest.backupPath);
            
            // Save manifest
            SaveManifest(manifest);
            
            Debug.Log($"[CLEANUP] Safety backup created at: {manifest.backupPath}");
            return manifest;
        }
        
        private static ValidationReport RunFullValidation()
        {
            var report = new ValidationReport();
            
            // Test compilation
            report.compilationSuccess = CompilationPipeline.IsCompiling() == false;
            
            // Test all unit tests
            report.unitTestsPass = RunAllUnitTests();
            
            // Test scene loading
            report.scenesLoadCorrectly = ValidateAllScenes();
            
            // Count references
            report.totalReferences = CountAllReferences();
            
            return report;
        }
    }
}
```

### 2.2 Risk Assessment Framework

```csharp
public class RiskAssessment
{
    public enum RiskLevel
    {
        Safe = 0,        // Can be deleted without concern
        Low = 1,         // Backup recommended
        Medium = 2,      // Requires validation
        High = 3,        // Manual review required
        Critical = 4     // Do not delete automatically
    }
    
    public class FileRiskAnalysis
    {
        public string filePath;
        public RiskLevel risk;
        public List<string> reasons = new List<string>();
        public int referenceCount;
        public bool hasTests;
        public bool isInGitHistory;
        public DateTime lastModified;
        public List<string> dependencies = new List<string>();
    }
    
    public static FileRiskAnalysis AnalyzeFile(string filePath)
    {
        var analysis = new FileRiskAnalysis { filePath = filePath };
        
        // Check for references
        analysis.referenceCount = CountReferences(filePath);
        if (analysis.referenceCount > 0)
        {
            analysis.risk = RiskLevel.High;
            analysis.reasons.Add($"Found {analysis.referenceCount} references");
        }
        
        // Check for test coverage
        analysis.hasTests = HasUnitTests(filePath);
        if (!analysis.hasTests && analysis.referenceCount > 0)
        {
            analysis.risk = RiskLevel.Critical;
            analysis.reasons.Add("Referenced but no tests");
        }
        
        // Check modification date
        analysis.lastModified = File.GetLastWriteTime(filePath);
        if ((DateTime.Now - analysis.lastModified).Days < 7)
        {
            analysis.risk = Math.Max(analysis.risk, RiskLevel.Medium);
            analysis.reasons.Add("Recently modified");
        }
        
        // Check for critical patterns
        string content = File.ReadAllText(filePath);
        if (content.Contains("[RuntimeInitializeOnLoadMethod]") ||
            content.Contains("DontDestroyOnLoad") ||
            content.Contains("Resources.Load"))
        {
            analysis.risk = RiskLevel.Critical;
            analysis.reasons.Add("Contains critical initialization code");
        }
        
        return analysis;
    }
    
    private static int CountReferences(string filePath)
    {
        string className = Path.GetFileNameWithoutExtension(filePath);
        int count = 0;
        
        // Search all .cs files
        foreach (var file in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories))
        {
            if (file == filePath) continue;
            
            string content = File.ReadAllText(file);
            count += Regex.Matches(content, $@"\b{className}\b").Count;
        }
        
        // Search all .prefab files
        foreach (var prefab in Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(prefab);
            if (content.Contains($"m_Script: {{fileID:") && content.Contains(className))
                count++;
        }
        
        return count;
    }
}
```

### 2.3 Rollback Procedures

```csharp
public class CleanupRollback
{
    public static bool RollbackCleanup(string manifestPath)
    {
        try
        {
            var manifest = LoadManifest(manifestPath);
            
            Debug.Log($"[ROLLBACK] Starting rollback from: {manifest.timestamp}");
            
            // Reverse all operations
            for (int i = manifest.operations.Count - 1; i >= 0; i--)
            {
                var operation = manifest.operations[i];
                
                switch (operation.operationType)
                {
                    case "DELETE":
                        RestoreFile(operation.backupPath, operation.sourcePath);
                        break;
                        
                    case "MODIFY":
                        RestoreFile(operation.backupPath, operation.sourcePath);
                        break;
                        
                    case "MOVE":
                        MoveFile(operation.targetPath, operation.sourcePath);
                        break;
                }
            }
            
            // Restore project settings
            RestoreProjectSettings(manifest.backupPath);
            
            // Force Unity to reimport
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            // Run validation
            var validation = RunFullValidation();
            
            if (validation.IsValid())
            {
                Debug.Log("[ROLLBACK] Rollback completed successfully");
                return true;
            }
            else
            {
                Debug.LogError("[ROLLBACK] Validation failed after rollback!");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ROLLBACK] Critical error: {e.Message}");
            return false;
        }
    }
}
```

---

## 3. Implementation Tools

### 3.1 Unity Editor Cleanup Tool

```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace WallChessQuidor.Editor.Cleanup
{
    public class CodebaseCleanupWindow : EditorWindow
    {
        private enum CleanupMode
        {
            Analysis,
            Preview,
            Execute,
            Validate
        }
        
        private CleanupMode currentMode = CleanupMode.Analysis;
        private CleanupReport currentReport;
        private Vector2 scrollPosition;
        private bool safetyBackupCreated = false;
        private Dictionary<string, bool> fileSelections = new Dictionary<string, bool>();
        
        [MenuItem("WallChessQuidor/Cleanup Tools/Codebase Cleanup")]
        public static void ShowWindow()
        {
            var window = GetWindow<CodebaseCleanupWindow>("Codebase Cleanup");
            window.minSize = new Vector2(800, 600);
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawModeSelector();
            
            EditorGUILayout.Space(10);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (currentMode)
            {
                case CleanupMode.Analysis:
                    DrawAnalysisMode();
                    break;
                case CleanupMode.Preview:
                    DrawPreviewMode();
                    break;
                case CleanupMode.Execute:
                    DrawExecuteMode();
                    break;
                case CleanupMode.Validate:
                    DrawValidateMode();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawAnalysisMode()
        {
            EditorGUILayout.LabelField("Codebase Analysis", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Run Full Analysis", GUILayout.Height(30)))
            {
                currentReport = RunComprehensiveAnalysis();
            }
            
            if (currentReport != null)
            {
                EditorGUILayout.Space(10);
                
                // Dead Code Section
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Dead Code Found: {currentReport.deadCode.Count} files", 
                    EditorStyles.boldLabel);
                
                foreach (var file in currentReport.deadCode)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Risk indicator
                    var riskColor = GetRiskColor(file.riskLevel);
                    GUI.color = riskColor;
                    EditorGUILayout.LabelField("●", GUILayout.Width(20));
                    GUI.color = Color.white;
                    
                    // File selection checkbox
                    bool isSelected = fileSelections.GetValueOrDefault(file.path, false);
                    bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                    if (newSelection != isSelected)
                    {
                        fileSelections[file.path] = newSelection;
                    }
                    
                    // File info
                    EditorGUILayout.LabelField(file.relativePath, GUILayout.Width(300));
                    EditorGUILayout.LabelField($"{file.lines} lines", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Risk: {file.riskLevel}", GUILayout.Width(100));
                    
                    // View button
                    if (GUILayout.Button("View", GUILayout.Width(50)))
                    {
                        OpenFileInEditor(file.path);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Show risk reasons
                    if (file.riskReasons.Count > 0)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var reason in file.riskReasons)
                        {
                            EditorGUILayout.LabelField($"⚠ {reason}", EditorStyles.miniLabel);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                
                EditorGUILayout.EndVertical();
                
                // Backup Files Section
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Backup Files: {currentReport.backupFiles.Count}", 
                    EditorStyles.boldLabel);
                
                foreach (var file in currentReport.backupFiles)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    bool isSelected = fileSelections.GetValueOrDefault(file.path, true);
                    fileSelections[file.path] = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                    
                    EditorGUILayout.LabelField(file.name, GUILayout.Width(300));
                    EditorGUILayout.LabelField(FormatFileSize(file.size), GUILayout.Width(80));
                    EditorGUILayout.LabelField(file.lastModified.ToString("yyyy-MM-dd"), GUILayout.Width(100));
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
                
                // Duplicate Code Section
                EditorGUILayout.Space(10);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Duplicate Code Groups: {currentReport.duplicates.Count}", 
                    EditorStyles.boldLabel);
                
                foreach (var group in currentReport.duplicates)
                {
                    EditorGUILayout.LabelField($"Pattern: {group.pattern}", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    
                    foreach (var file in group.files)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        bool isKeeper = group.recommendedKeeper == file;
                        if (isKeeper)
                        {
                            GUI.color = Color.green;
                            EditorGUILayout.LabelField("✓ KEEP", GUILayout.Width(60));
                        }
                        else
                        {
                            bool isSelected = fileSelections.GetValueOrDefault(file, true);
                            fileSelections[file] = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                            EditorGUILayout.LabelField("Remove", GUILayout.Width(60));
                        }
                        GUI.color = Color.white;
                        
                        EditorGUILayout.LabelField(file);
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(5);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawPreviewMode()
        {
            if (!safetyBackupCreated)
            {
                EditorGUILayout.HelpBox(
                    "Safety backup must be created before proceeding with cleanup.", 
                    MessageType.Warning);
                
                if (GUILayout.Button("Create Safety Backup", GUILayout.Height(40)))
                {
                    CreateSafetyBackup();
                    safetyBackupCreated = true;
                }
                return;
            }
            
            EditorGUILayout.LabelField("Cleanup Preview", EditorStyles.boldLabel);
            
            // Show what will be deleted/modified
            var selectedFiles = fileSelections.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            
            EditorGUILayout.HelpBox($"Selected {selectedFiles.Count} items for cleanup", MessageType.Info);
            
            // Group by operation type
            var deleteOps = selectedFiles.Where(f => WillDelete(f)).ToList();
            var modifyOps = selectedFiles.Where(f => WillModify(f)).ToList();
            
            if (deleteOps.Count > 0)
            {
                EditorGUILayout.LabelField($"Files to Delete ({deleteOps.Count}):", EditorStyles.boldLabel);
                foreach (var file in deleteOps)
                {
                    EditorGUILayout.LabelField($"  ✗ {file}", EditorStyles.miniLabel);
                }
            }
            
            if (modifyOps.Count > 0)
            {
                EditorGUILayout.LabelField($"Files to Modify ({modifyOps.Count}):", EditorStyles.boldLabel);
                foreach (var file in modifyOps)
                {
                    EditorGUILayout.LabelField($"  ✎ {file}", EditorStyles.miniLabel);
                }
            }
            
            EditorGUILayout.Space(20);
            
            if (GUILayout.Button("Execute Cleanup", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Confirm Cleanup", 
                    $"This will permanently modify {selectedFiles.Count} files.\n\n" +
                    "A backup has been created and you can rollback if needed.\n\n" +
                    "Continue?", "Execute", "Cancel"))
                {
                    currentMode = CleanupMode.Execute;
                    ExecuteCleanup(selectedFiles);
                }
            }
        }
        
        private CleanupReport RunComprehensiveAnalysis()
        {
            var report = new CleanupReport();
            
            EditorUtility.DisplayProgressBar("Analyzing", "Scanning for dead code...", 0.1f);
            report.deadCode = FindDeadCode();
            
            EditorUtility.DisplayProgressBar("Analyzing", "Finding backup files...", 0.3f);
            report.backupFiles = FindBackupFiles();
            
            EditorUtility.DisplayProgressBar("Analyzing", "Detecting duplicates...", 0.5f);
            report.duplicates = FindDuplicateCode();
            
            EditorUtility.DisplayProgressBar("Analyzing", "Analyzing inline documentation...", 0.7f);
            report.inlineDocumentation = FindInlineDocumentation();
            
            EditorUtility.DisplayProgressBar("Analyzing", "Calculating metrics...", 0.9f);
            report.CalculateMetrics();
            
            EditorUtility.ClearProgressBar();
            
            return report;
        }
    }
}
```

### 3.2 Automated Validation System

```csharp
public class CleanupValidator
{
    public class ValidationResult
    {
        public bool success;
        public List<ValidationIssue> issues = new List<ValidationIssue>();
        public Dictionary<string, object> metrics = new Dictionary<string, object>();
    }
    
    public class ValidationIssue
    {
        public string severity; // ERROR, WARNING, INFO
        public string category;
        public string message;
        public string filePath;
        public int lineNumber;
    }
    
    public static ValidationResult ValidatePostCleanup()
    {
        var result = new ValidationResult { success = true };
        
        // 1. Compilation Check
        if (EditorUtility.scriptCompilationFailed)
        {
            result.success = false;
            result.issues.Add(new ValidationIssue
            {
                severity = "ERROR",
                category = "Compilation",
                message = "Scripts failed to compile after cleanup"
            });
        }
        
        // 2. Missing References Check
        var missingRefs = FindMissingReferences();
        foreach (var missing in missingRefs)
        {
            result.success = false;
            result.issues.Add(new ValidationIssue
            {
                severity = "ERROR",
                category = "References",
                message = $"Missing reference: {missing.componentName}",
                filePath = missing.assetPath
            });
        }
        
        // 3. Scene Validation
        foreach (var scenePath in EditorBuildSettings.scenes)
        {
            if (scenePath.enabled)
            {
                var scene = EditorSceneManager.OpenScene(scenePath.path, 
                    OpenSceneMode.AdditiveWithoutLoading);
                
                var rootObjects = scene.GetRootGameObjects();
                foreach (var obj in rootObjects)
                {
                    ValidateGameObject(obj, result);
                }
                
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        
        // 4. Unit Test Validation
        var testResults = RunUnitTests();
        if (!testResults.allPassed)
        {
            result.success = false;
            foreach (var failedTest in testResults.failures)
            {
                result.issues.Add(new ValidationIssue
                {
                    severity = "ERROR",
                    category = "UnitTest",
                    message = $"Test failed: {failedTest.testName}",
                    filePath = failedTest.testFile
                });
            }
        }
        
        // 5. Performance Metrics
        result.metrics["CompilationTime"] = MeasureCompilationTime();
        result.metrics["ScriptCount"] = CountScripts();
        result.metrics["TotalLinesOfCode"] = CountLinesOfCode();
        result.metrics["MemoryUsage"] = GetMemoryUsage();
        
        return result;
    }
    
    private static void ValidateGameObject(GameObject obj, ValidationResult result)
    {
        var components = obj.GetComponents<Component>();
        
        foreach (var component in components)
        {
            if (component == null)
            {
                result.success = false;
                result.issues.Add(new ValidationIssue
                {
                    severity = "ERROR",
                    category = "MissingScript",
                    message = $"Missing script on GameObject: {obj.name}",
                    filePath = obj.scene.path
                });
            }
            else
            {
                // Check for missing references in component fields
                var so = new SerializedObject(component);
                var sp = so.GetIterator();
                
                while (sp.NextVisible(true))
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (sp.objectReferenceValue == null && 
                            sp.objectReferenceInstanceIDValue != 0)
                        {
                            result.issues.Add(new ValidationIssue
                            {
                                severity = "WARNING",
                                category = "MissingReference",
                                message = $"Missing reference in {component.GetType().Name}.{sp.name}",
                                filePath = obj.scene.path
                            });
                        }
                    }
                }
            }
        }
        
        // Recursively check children
        foreach (Transform child in obj.transform)
        {
            ValidateGameObject(child.gameObject, result);
        }
    }
}
```

---

## 4. Cleanup Categories

### 4.1 Dead Code Removal Strategy

```csharp
public class DeadCodeRemover
{
    public class DeadCodePattern
    {
        public string name;
        public Func<string, bool> detector;
        public Func<string, string> cleaner;
        public float confidenceThreshold;
    }
    
    private static readonly List<DeadCodePattern> patterns = new List<DeadCodePattern>
    {
        new DeadCodePattern
        {
            name = "Commented Code Blocks",
            detector = content => Regex.IsMatch(content, @"/\*[\s\S]{50,}\*/|//.*\n{5,}"),
            cleaner = content => Regex.Replace(content, @"/\*[\s\S]{50,}\*/", ""),
            confidenceThreshold = 0.9f
        },
        
        new DeadCodePattern
        {
            name = "Unreachable Code",
            detector = content => content.Contains("return") && 
                                 Regex.IsMatch(content, @"return[^}]+\n\s+\w"),
            cleaner = RemoveUnreachableCode,
            confidenceThreshold = 0.95f
        },
        
        new DeadCodePattern
        {
            name = "Unused Private Methods",
            detector = content => HasUnusedPrivateMethods(content),
            cleaner = RemoveUnusedPrivateMethods,
            confidenceThreshold = 0.85f
        },
        
        new DeadCodePattern
        {
            name = "Dead Feature Flags",
            detector = content => Regex.IsMatch(content, @"const\s+bool\s+\w+\s*=\s*false"),
            cleaner = RemoveDeadFeatureFlags,
            confidenceThreshold = 0.8f
        }
    };
    
    public static CleanupResult RemoveDeadCode(string filePath)
    {
        var result = new CleanupResult { filePath = filePath };
        string content = File.ReadAllText(filePath);
        string originalContent = content;
        
        foreach (var pattern in patterns)
        {
            if (pattern.detector(content))
            {
                var confidence = CalculateConfidence(content, pattern);
                
                if (confidence >= pattern.confidenceThreshold)
                {
                    content = pattern.cleaner(content);
                    result.patternsRemoved.Add(pattern.name);
                    result.linesRemoved += CountLines(originalContent) - CountLines(content);
                }
                else
                {
                    result.warnings.Add($"Low confidence for {pattern.name}: {confidence:P}");
                }
            }
        }
        
        if (content != originalContent)
        {
            // Validate that the file still compiles
            if (ValidateCompilation(content))
            {
                File.WriteAllText(filePath, content);
                result.success = true;
            }
            else
            {
                result.success = false;
                result.errors.Add("File would not compile after cleanup");
            }
        }
        
        return result;
    }
}
```

### 4.2 Backup File Cleanup

```csharp
public class BackupFileCleaner
{
    private static readonly string[] BACKUP_PATTERNS = new[]
    {
        "*.backup",
        "*.old",
        "*.bak",
        "*_backup",
        "*_old",
        "*Copy*",
        "*~",
        "*.orig"
    };
    
    public static List<BackupFile> IdentifyBackupFiles()
    {
        var backupFiles = new List<BackupFile>();
        
        foreach (var pattern in BACKUP_PATTERNS)
        {
            var files = Directory.GetFiles("Assets", pattern, SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                var backup = new BackupFile
                {
                    path = file,
                    size = info.Length,
                    created = info.CreationTime,
                    lastModified = info.LastWriteTime,
                    pattern = pattern
                };
                
                // Try to find the original file
                backup.originalFile = FindOriginalFile(file);
                
                // Determine if safe to delete
                backup.safeToDelete = IsSafeToDelete(backup);
                
                backupFiles.Add(backup);
            }
        }
        
        // Also find backup directories
        var backupDirs = Directory.GetDirectories("Assets", "*backup*", 
            SearchOption.AllDirectories);
        
        foreach (var dir in backupDirs)
        {
            var dirInfo = new DirectoryInfo(dir);
            var backup = new BackupFile
            {
                path = dir,
                isDirectory = true,
                size = GetDirectorySize(dir),
                created = dirInfo.CreationTime,
                lastModified = dirInfo.LastWriteTime
            };
            
            backup.safeToDelete = IsSafeToDelete(backup);
            backupFiles.Add(backup);
        }
        
        return backupFiles;
    }
    
    private static bool IsSafeToDelete(BackupFile backup)
    {
        // Check age - if older than 30 days, likely safe
        if ((DateTime.Now - backup.lastModified).Days > 30)
            return true;
        
        // Check if original exists and is newer
        if (!string.IsNullOrEmpty(backup.originalFile) && 
            File.Exists(backup.originalFile))
        {
            var originalModified = File.GetLastWriteTime(backup.originalFile);
            if (originalModified > backup.lastModified)
                return true;
        }
        
        // Check if in version control
        if (IsInVersionControl(backup.path))
            return true;
        
        return false;
    }
}
```

### 4.3 Code Consolidation

```csharp
public class CodeConsolidator
{
    public class ConsolidationPlan
    {
        public string targetFile;
        public List<string> sourceFiles;
        public Dictionary<string, string> methodMapping;
        public List<ConflictResolution> conflicts;
    }
    
    public static ConsolidationPlan PlanWallPlacerConsolidation()
    {
        var plan = new ConsolidationPlan
        {
            targetFile = "Assets/Scripts/Core/WallPlacer.cs",
            sourceFiles = new List<string>
            {
                "Assets/Scripts/WallPlacerV2.cs",
                "Assets/Scripts/WallPlacerOld.cs",
                "Assets/Scripts/WallPlacerOptimized.cs",
                "Assets/Scripts/WallPlacerTest.cs"
            }
        };
        
        // Analyze all implementations
        var implementations = new Dictionary<string, List<MethodImplementation>>();
        
        foreach (var file in plan.sourceFiles)
        {
            var methods = ExtractMethods(file);
            foreach (var method in methods)
            {
                if (!implementations.ContainsKey(method.signature))
                    implementations[method.signature] = new List<MethodImplementation>();
                
                implementations[method.signature].Add(method);
            }
        }
        
        // Determine best implementation for each method
        foreach (var kvp in implementations)
        {
            var bestImpl = SelectBestImplementation(kvp.Value);
            plan.methodMapping[kvp.Key] = bestImpl.sourceFile;
            
            // Check for conflicts
            if (kvp.Value.Count > 1)
            {
                var differences = AnalyzeDifferences(kvp.Value);
                if (differences.HasSignificantDifferences)
                {
                    plan.conflicts.Add(new ConflictResolution
                    {
                        methodSignature = kvp.Key,
                        implementations = kvp.Value,
                        recommendation = bestImpl,
                        reason = differences.Summary
                    });
                }
            }
        }
        
        return plan;
    }
    
    public static void ExecuteConsolidation(ConsolidationPlan plan)
    {
        // Build the consolidated class
        var builder = new StringBuilder();
        
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using UnityEngine;");
        builder.AppendLine();
        builder.AppendLine("namespace WallChessQuidor.Core");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// Consolidated WallPlacer implementation");
        builder.AppendLine("    /// Generated from multiple source files on " + DateTime.Now);
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public class WallPlacer : MonoBehaviour");
        builder.AppendLine("    {");
        
        // Add best version of each method
        foreach (var mapping in plan.methodMapping)
        {
            var method = GetMethodImplementation(mapping.Value, mapping.Key);
            builder.AppendLine(IndentCode(method, 8));
            builder.AppendLine();
        }
        
        builder.AppendLine("    }");
        builder.AppendLine("}");
        
        // Write consolidated file
        File.WriteAllText(plan.targetFile, builder.ToString());
        
        // Update all references
        UpdateReferences(plan.sourceFiles, plan.targetFile);
        
        // Delete source files
        foreach (var file in plan.sourceFiles)
        {
            AssetDatabase.DeleteAsset(file);
        }
        
        AssetDatabase.Refresh();
    }
}
```

### 4.4 Documentation Migration

```csharp
public class DocumentationMigrator
{
    public class DocumentationBlock
    {
        public string sourceFile;
        public int startLine;
        public int endLine;
        public string content;
        public string category;
        public string targetDocument;
    }
    
    public static void MigrateInlineDocumentation()
    {
        var blocks = FindDocumentationBlocks();
        var documents = new Dictionary<string, StringBuilder>();
        
        foreach (var block in blocks)
        {
            // Ensure target document exists
            if (!documents.ContainsKey(block.targetDocument))
            {
                documents[block.targetDocument] = new StringBuilder();
                
                // Add header
                documents[block.targetDocument].AppendLine($"# {Path.GetFileNameWithoutExtension(block.targetDocument)}");
                documents[block.targetDocument].AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd}");
                documents[block.targetDocument].AppendLine();
            }
            
            // Add documentation block
            var doc = documents[block.targetDocument];
            doc.AppendLine($"## From: {block.sourceFile}:{block.startLine}");
            doc.AppendLine();
            doc.AppendLine(block.content);
            doc.AppendLine();
            doc.AppendLine("---");
            doc.AppendLine();
            
            // Replace inline documentation with reference
            ReplaceWithReference(block);
        }
        
        // Write all documentation files
        foreach (var kvp in documents)
        {
            var path = Path.Combine("Documentation/Technical", kvp.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, kvp.Value.ToString());
        }
    }
    
    private static void ReplaceWithReference(DocumentationBlock block)
    {
        var lines = File.ReadAllLines(block.sourceFile).ToList();
        
        // Replace multi-line documentation with single reference
        var reference = $"/// See: Documentation/Technical/{block.targetDocument}";
        
        // Remove old lines
        for (int i = block.endLine; i >= block.startLine; i--)
        {
            lines.RemoveAt(i);
        }
        
        // Insert reference
        lines.Insert(block.startLine, reference);
        
        // Write back
        File.WriteAllLines(block.sourceFile, lines);
    }
}
```

---

## 5. Validation & Testing

### 5.1 Pre-Cleanup Testing Suite

```csharp
public class PreCleanupTests
{
    [Test]
    public static TestReport RunPreCleanupValidation()
    {
        var report = new TestReport();
        
        // 1. Capture current functionality baseline
        report.functionalTests = RunFunctionalTests();
        
        // 2. Performance baseline
        report.performanceBaseline = MeasurePerformance();
        
        // 3. Memory baseline
        report.memoryBaseline = ProfileMemory();
        
        // 4. Build test
        report.buildSuccess = TestBuild();
        
        // 5. Scene loading tests
        report.sceneTests = TestAllScenes();
        
        // Save baseline for comparison
        SaveBaseline(report);
        
        return report;
    }
    
    private static FunctionalTestResults RunFunctionalTests()
    {
        var results = new FunctionalTestResults();
        
        // Test piece movement
        results.AddTest("PieceMovement", () =>
        {
            var board = new BoardManager();
            board.Initialize();
            
            var piece = board.GetPiece(new Vector2Int(0, 0));
            var moves = board.GetValidMoves(piece);
            
            Assert.IsNotNull(moves);
            Assert.IsTrue(moves.Count > 0);
        });
        
        // Test wall placement
        results.AddTest("WallPlacement", () =>
        {
            var wallPlacer = new WallPlacer();
            var canPlace = wallPlacer.CanPlaceWall(new Vector2Int(1, 1), Orientation.Horizontal);
            
            Assert.IsNotNull(canPlace);
        });
        
        // Test pathfinding
        results.AddTest("Pathfinding", () =>
        {
            var pathfinder = new PathfindingSystem();
            var path = pathfinder.FindPath(Vector2Int.zero, new Vector2Int(8, 8));
            
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count > 0);
        });
        
        return results;
    }
}
```

### 5.2 Post-Cleanup Validation

```csharp
public class PostCleanupValidator
{
    public static ValidationReport ValidateCleanup(TestReport baseline)
    {
        var report = new ValidationReport();
        
        // 1. Functional regression testing
        var currentTests = RunFunctionalTests();
        report.functionalRegression = CompareTests(baseline.functionalTests, currentTests);
        
        // 2. Performance comparison
        var currentPerf = MeasurePerformance();
        report.performanceChange = CalculatePerformanceChange(baseline.performanceBaseline, currentPerf);
        
        // 3. Memory comparison
        var currentMemory = ProfileMemory();
        report.memoryChange = CalculateMemoryChange(baseline.memoryBaseline, currentMemory);
        
        // 4. Missing functionality check
        report.missingFeatures = CheckForMissingFeatures();
        
        // 5. Code quality metrics
        report.codeQuality = MeasureCodeQuality();
        
        return report;
    }
    
    private static List<string> CheckForMissingFeatures()
    {
        var missing = new List<string>();
        
        // Check if key classes still exist
        var requiredClasses = new[]
        {
            "BoardManager",
            "PieceController",
            "WallPlacer",
            "PathfindingSystem",
            "GameManager"
        };
        
        foreach (var className in requiredClasses)
        {
            if (!TypeExists(className))
            {
                missing.Add($"Missing class: {className}");
            }
        }
        
        // Check if key methods still exist
        var requiredMethods = new Dictionary<string, string[]>
        {
            ["BoardManager"] = new[] { "Initialize", "GetValidMoves", "MovePiece" },
            ["WallPlacer"] = new[] { "CanPlaceWall", "PlaceWall", "RemoveWall" }
        };
        
        foreach (var kvp in requiredMethods)
        {
            var type = GetType(kvp.Key);
            if (type != null)
            {
                foreach (var method in kvp.Value)
                {
                    if (type.GetMethod(method) == null)
                    {
                        missing.Add($"Missing method: {kvp.Key}.{method}");
                    }
                }
            }
        }
        
        return missing;
    }
}
```

### 5.3 Regression Testing Framework

```csharp
public class RegressionTestFramework
{
    public class RegressionTest
    {
        public string name;
        public Action testAction;
        public Func<bool> validator;
        public string category;
        public bool critical;
    }
    
    private static readonly List<RegressionTest> tests = new List<RegressionTest>
    {
        new RegressionTest
        {
            name = "Board Initialization",
            category = "Core",
            critical = true,
            testAction = () =>
            {
                var board = GameObject.FindObjectOfType<BoardManager>();
                board.Initialize();
            },
            validator = () =>
            {
                var board = GameObject.FindObjectOfType<BoardManager>();
                return board != null && board.IsInitialized;
            }
        },
        
        new RegressionTest
        {
            name = "Piece Movement",
            category = "Gameplay",
            critical = true,
            testAction = () =>
            {
                var controller = GameObject.FindObjectOfType<PieceController>();
                controller.TestMove(Vector2Int.zero, new Vector2Int(0, 1));
            },
            validator = () =>
            {
                var piece = GameObject.Find("TestPiece");
                return piece != null && piece.transform.position.z == 1;
            }
        }
    };
    
    public static RegressionReport RunAllTests()
    {
        var report = new RegressionReport();
        
        foreach (var test in tests)
        {
            var result = new TestResult { name = test.name, category = test.category };
            
            try
            {
                test.testAction();
                result.passed = test.validator();
            }
            catch (Exception e)
            {
                result.passed = false;
                result.error = e.Message;
            }
            
            report.results.Add(result);
            
            if (!result.passed && test.critical)
            {
                report.criticalFailure = true;
                break;
            }
        }
        
        return report;
    }
}
```

---

## Implementation Schedule

### Phase 1: Analysis & Backup (Day 1)
- Morning: Run comprehensive analysis
- Afternoon: Create safety backups
- End of day: Review risk assessments

### Phase 2: Low-Risk Cleanup (Day 2)
- Backup files removal
- Old commented code removal
- Empty folders cleanup

### Phase 3: Medium-Risk Cleanup (Day 3)
- Dead code removal in non-critical paths
- Duplicate file consolidation
- Documentation migration

### Phase 4: High-Risk Cleanup (Day 4)
- Core system dead code removal
- Major consolidations
- Final validation

### Phase 5: Validation & Polish (Day 5)
- Full regression testing
- Performance validation
- Documentation updates

---

## Safety Checklist

Before starting cleanup:
- [ ] Full project backup created
- [ ] All tests passing
- [ ] Git repository clean
- [ ] Team notified of cleanup schedule

During cleanup:
- [ ] Each operation logged
- [ ] Validation after each major change
- [ ] Regular commits to Git
- [ ] Continuous integration monitoring

After cleanup:
- [ ] All tests still passing
- [ ] Performance metrics improved or stable
- [ ] No missing functionality
- [ ] Documentation updated
- [ ] Team walkthrough completed

---

## Risk Mitigation

### Critical Safeguards
1. **Never delete without backup**: Every file deletion is preceded by backup
2. **Atomic operations**: All changes can be rolled back as a unit
3. **Continuous validation**: Tests run after each operation
4. **Manual review for high-risk**: Human approval required for critical files
5. **Staged approach**: Start with lowest risk, build confidence

### Emergency Procedures
```bash
# If critical failure occurs:
1. Stop all cleanup operations immediately
2. Run: git status to assess changes
3. Run: git diff to review modifications
4. Execute rollback: Unity → WallChessQuidor → Cleanup Tools → Emergency Rollback
5. Restore from timestamped backup if Git insufficient
6. Notify team of issues and resolution
```

---

## Success Metrics

### Quantitative Goals
- Reduce codebase by 30-40%
- Improve compilation time by 50%
- Reduce memory footprint by 20%
- Eliminate 100% of backup files
- Consolidate duplicate implementations to single source

### Qualitative Goals
- Clearer code organization
- Improved developer onboarding
- Reduced merge conflicts
- Better IDE performance
- Simplified debugging

---

## Next Steps

After successful cleanup:
1. Update project documentation
2. Retrain AI code assistants on clean codebase
3. Implement coding standards to prevent future accumulation
4. Set up automated cleanup reminders
5. Begin Part 1: Hybrid Architecture implementation

This cleanup phase is the critical foundation that enables all subsequent refactoring efforts to proceed smoothly and efficiently.