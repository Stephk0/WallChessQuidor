using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace WallChessQuidor.Editor.Cleanup
{
    /// <summary>
    /// Safety protocol for refactoring operations. Creates backups and provides controlled cleanup.
    /// Part of Phase 1: Emergency Codebase Cleanup (MVP Implementation Guide)
    /// </summary>
    public class CleanupSafetyProtocol
    {
        private const string BACKUP_ROOT = "ProjectBackups/Cleanup_";
        private static readonly string[] DEAD_CODE_PATTERNS = { "*.backup", "*.old", "*Copy.cs", "*~", "*.cs2", "*.cs3" };
        
        [MenuItem("WallChessQuidor/Safety/Create Full Backup")]
        public static void CreateSafetyBackup()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(Application.dataPath, "..", BACKUP_ROOT + timestamp);
            
            // Create backup directory
            Directory.CreateDirectory(backupPath);
            
            // Copy entire Code folder
            string codePath = Path.Combine(Application.dataPath, "Code");
            if (Directory.Exists(codePath))
            {
                string backupCodePath = Path.Combine(backupPath, "Code");
                CopyDirectory(codePath, backupCodePath);
                Debug.Log($"[CLEANUP] Code backup created at: {backupCodePath}");
            }
            
            // Copy settings and configurations
            string settingsPath = Path.Combine(Application.dataPath, "Settings");
            if (Directory.Exists(settingsPath))
            {
                string backupSettingsPath = Path.Combine(backupPath, "Settings");
                CopyDirectory(settingsPath, backupSettingsPath);
            }
            
            Debug.Log($"[CLEANUP] Full safety backup created at: {backupPath}");
            EditorUtility.DisplayDialog("Backup Complete", 
                $"Safety backup created successfully at:\n{backupPath}", "OK");
        }
        
        [MenuItem("WallChessQuidor/Cleanup/1. Analyze Dead Code")]
        public static void AnalyzeDeadCode()
        {
            Debug.Log("[CLEANUP] Starting dead code analysis...");
            
            var deadFiles = new List<string>();
            long totalSize = 0;
            
            // Check MVP folder
            string mvpPath = Path.Combine(Application.dataPath, "Code/MVP");
            if (Directory.Exists(mvpPath))
            {
                var mvpFiles = Directory.GetFiles(mvpPath, "*.cs", SearchOption.AllDirectories);
                deadFiles.AddRange(mvpFiles);
                foreach (var file in mvpFiles)
                {
                    totalSize += new FileInfo(file).Length;
                }
                Debug.Log($"[CLEANUP] Found MVP folder with {mvpFiles.Length} files ({FormatBytes(totalSize)})");
            }
            
            // Find backup files
            foreach (var pattern in DEAD_CODE_PATTERNS)
            {
                var files = Directory.GetFiles(Application.dataPath, pattern, SearchOption.AllDirectories)
                    .Where(f => f.Contains("/Code/")).ToArray();
                    
                foreach (var file in files)
                {
                    deadFiles.Add(file);
                    totalSize += new FileInfo(file).Length;
                    Debug.Log($"[CLEANUP] Found backup file: {Path.GetFileName(file)}");
                }
            }
            
            // Count lines of dead code
            int totalLines = 0;
            foreach (var file in deadFiles)
            {
                if (file.EndsWith(".cs") || file.EndsWith(".cs2") || file.EndsWith(".cs3"))
                {
                    totalLines += File.ReadAllLines(file).Length;
                }
            }
            
            Debug.Log($"[CLEANUP] Analysis complete:");
            Debug.Log($"  - Total dead files: {deadFiles.Count}");
            Debug.Log($"  - Total size: {FormatBytes(totalSize)}");
            Debug.Log($"  - Total lines of code: {totalLines:N0}");
            
            if (deadFiles.Count > 0)
            {
                EditorUtility.DisplayDialog("Dead Code Analysis", 
                    $"Found {deadFiles.Count} dead files\n" +
                    $"Total size: {FormatBytes(totalSize)}\n" +
                    $"Lines of code: {totalLines:N0}\n\n" +
                    "Run 'Remove Dead Code' to clean these files.", "OK");
            }
        }
        
        [MenuItem("WallChessQuidor/Cleanup/2. Remove Dead Code (After Backup)")]
        public static void RemoveDeadCode()
        {
            if (!EditorUtility.DisplayDialog("Remove Dead Code", 
                "This will permanently delete dead code files.\n\n" +
                "Have you created a backup using 'Create Full Backup'?", 
                "Yes, Continue", "Cancel"))
            {
                return;
            }
            
            int removedCount = 0;
            long freedSpace = 0;
            
            // Remove MVP folder
            string mvpPath = "Assets/Code/MVP";
            if (AssetDatabase.IsValidFolder(mvpPath))
            {
                var files = Directory.GetFiles(Path.Combine(Application.dataPath, "Code/MVP"), "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    freedSpace += new FileInfo(file).Length;
                    removedCount++;
                }
                
                AssetDatabase.DeleteAsset(mvpPath);
                Debug.Log($"[CLEANUP] Removed MVP folder ({files.Length} files)");
            }
            
            // Remove backup files
            foreach (var pattern in DEAD_CODE_PATTERNS)
            {
                var files = Directory.GetFiles(Application.dataPath, pattern, SearchOption.AllDirectories)
                    .Where(f => f.Contains("/Code/")).ToArray();
                    
                foreach (var file in files)
                {
                    freedSpace += new FileInfo(file).Length;
                    
                    // Convert to Unity asset path
                    string assetPath = "Assets" + file.Replace(Application.dataPath, "").Replace('\\', '/');
                    
                    if (AssetDatabase.DeleteAsset(assetPath))
                    {
                        removedCount++;
                        Debug.Log($"[CLEANUP] Removed: {Path.GetFileName(file)}");
                    }
                    else
                    {
                        // Try direct file deletion if AssetDatabase fails
                        try
                        {
                            File.Delete(file);
                            removedCount++;
                            Debug.Log($"[CLEANUP] Removed: {Path.GetFileName(file)}");
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[CLEANUP] Failed to remove {file}: {e.Message}");
                        }
                    }
                }
            }
            
            // Remove .meta files for deleted files
            CleanOrphanedMetaFiles();
            
            AssetDatabase.Refresh();
            
            Debug.Log($"[CLEANUP] Dead code removal complete:");
            Debug.Log($"  - Files removed: {removedCount}");
            Debug.Log($"  - Space freed: {FormatBytes(freedSpace)}");
            
            EditorUtility.DisplayDialog("Cleanup Complete", 
                $"Removed {removedCount} dead files\n" +
                $"Freed {FormatBytes(freedSpace)} of disk space", "OK");
        }
        
        [MenuItem("WallChessQuidor/Cleanup/3. Find Duplicate WallPlacers")]
        public static void FindDuplicateWallPlacers()
        {
            Debug.Log("[CLEANUP] Searching for WallPlacer implementations...");
            
            var wallPlacerFiles = new List<string>();
            
            // Search patterns for WallPlacer variants
            string[] searchPatterns = { 
                "WallPlacer*.cs", 
                "*WallPlacer.cs", 
                "*WallPlac*.cs",
                "FixedWallPlacer.cs",
                "ImprovedWallPlacer*"
            };
            
            foreach (var pattern in searchPatterns)
            {
                var files = Directory.GetFiles(Application.dataPath, pattern, SearchOption.AllDirectories);
                wallPlacerFiles.AddRange(files);
            }
            
            // Remove duplicates and sort
            wallPlacerFiles = wallPlacerFiles.Distinct().OrderBy(f => f).ToList();
            
            Debug.Log($"[CLEANUP] Found {wallPlacerFiles.Count} WallPlacer implementations:");
            foreach (var file in wallPlacerFiles)
            {
                var fi = new FileInfo(file);
                var lines = File.ReadAllLines(file).Length;
                Debug.Log($"  - {Path.GetFileName(file)} ({lines} lines, {FormatBytes(fi.Length)})");
            }
            
            if (wallPlacerFiles.Count > 1)
            {
                EditorUtility.DisplayDialog("WallPlacer Analysis", 
                    $"Found {wallPlacerFiles.Count} WallPlacer implementations.\n\n" +
                    "These should be consolidated into a single implementation.\n" +
                    "Review the console for details.", "OK");
            }
        }
        
        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            
            // Copy files
            foreach (string file in Directory.GetFiles(source))
            {
                string dest = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }
            
            // Copy subdirectories
            foreach (string dir in Directory.GetDirectories(source))
            {
                string dest = Path.Combine(destination, Path.GetFileName(dir));
                CopyDirectory(dir, dest);
            }
        }
        
        private static void CleanOrphanedMetaFiles()
        {
            var metaFiles = Directory.GetFiles(Application.dataPath, "*.meta", SearchOption.AllDirectories);
            int cleanedCount = 0;
            
            foreach (var metaFile in metaFiles)
            {
                string assetFile = metaFile.Substring(0, metaFile.Length - 5);
                if (!File.Exists(assetFile) && !Directory.Exists(assetFile))
                {
                    try
                    {
                        File.Delete(metaFile);
                        cleanedCount++;
                    }
                    catch { }
                }
            }
            
            if (cleanedCount > 0)
            {
                Debug.Log($"[CLEANUP] Removed {cleanedCount} orphaned .meta files");
            }
        }
        
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
    
    /// <summary>
    /// Progress monitoring window for cleanup operations
    /// </summary>
    public class CleanupProgressWindow : EditorWindow
    {
        private static CleanupProgressWindow window;
        private string currentOperation = "";
        private float progress = 0f;
        
        [MenuItem("WallChessQuidor/Cleanup/Show Progress Window")]
        public static void ShowWindow()
        {
            window = GetWindow<CleanupProgressWindow>("Cleanup Progress");
            window.minSize = new Vector2(400, 150);
        }
        
        void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Refactoring Cleanup Progress", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Current Operation:", currentOperation);
            
            EditorGUILayout.Space(5);
            
            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(rect, progress, $"{(progress * 100):0}%");
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Run Full Cleanup Sequence"))
            {
                RunFullCleanup();
            }
        }
        
        private void RunFullCleanup()
        {
            currentOperation = "Creating backup...";
            progress = 0.2f;
            Repaint();
            
            CleanupSafetyProtocol.CreateSafetyBackup();
            
            currentOperation = "Analyzing dead code...";
            progress = 0.4f;
            Repaint();
            
            CleanupSafetyProtocol.AnalyzeDeadCode();
            
            currentOperation = "Removing dead code...";
            progress = 0.6f;
            Repaint();
            
            CleanupSafetyProtocol.RemoveDeadCode();
            
            currentOperation = "Finding duplicate implementations...";
            progress = 0.8f;
            Repaint();
            
            CleanupSafetyProtocol.FindDuplicateWallPlacers();
            
            currentOperation = "Complete!";
            progress = 1.0f;
            Repaint();
        }
    }
}