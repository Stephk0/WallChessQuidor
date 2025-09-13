# Part 6: Immediate Priority - Codebase Cleanup
**WallChessQuidor Refactoring Implementation Guide**

**Page:** 55-61 of 101  
**Document:** 06-codebase-cleanup.md  
**Priority:** ⚡ IMMEDIATE  
**Estimated Time:** 2 hours  
**Prerequisites:** [Part 1: Project Setup](01-project-setup-prerequisites.md)  
**Next Document:** [Part 2: State Management](02-state-management-consolidation.md)  

---

## Why Clean First

Before doing any major refactoring, we need a clean workspace. The codebase currently has:
- 30% dead code (entire MVP folder)
- 12 backup files cluttering directories
- 5 different WallPlacer implementations
- Tons of commented-out experimental code

**Goal**: Reduce codebase size by 30-40% in 2 hours, making the real refactoring work much clearer.

---

## User Stories

### Story 1: Remove Dead Code Safely
**As a developer, I want to remove all dead code so that I only work with active, relevant code.**

**Acceptance Criteria:**
- Delete the entire `/Assets/Code/MVP/` folder
- Remove all commented-out code blocks
- Keep only the actively used WallPlacer implementation
- Verify game still compiles and runs

**Steps:**
1. **Backup first**: Create project backup using the tool from Part 1
2. **Delete MVP folder**: Right-click `/Assets/Code/MVP/` → Delete
3. **Remove backup files**: Search for `*.backup` files and delete them
4. **Clean WallPlacer versions**: Keep only the newest, delete the other 4
5. **Test**: Run the game, make sure it works

### Story 2: Clean Up Backup Files
**As a developer, I want to remove all backup files so the project structure is clean.**

**Acceptance Criteria:**
- No files with `.backup` extension remain
- No duplicate files with version numbers (e.g., `Script_v2.cs`)
- Project builds without errors

**Steps:**
1. **Find all backups**: Use Unity search for "backup" and "copy"
2. **Review each file**: Make sure the main version exists
3. **Delete safely**: Remove backup files one by one
4. **Refresh**: Let Unity reimport everything

### Story 3: Consolidate Duplicate Code
**As a developer, I want one clear implementation of each system so I don't have confusion.**

**Acceptance Criteria:**
- Only one WallPlacer script remains
- No duplicate utility functions
- Clear naming for all remaining scripts

**Steps:**
1. **Identify the best version**: Look at modification dates and complexity
2. **Merge useful code**: Copy any good bits from old versions to the keeper
3. **Delete the rest**: Remove the 4 unused WallPlacer versions
4. **Update references**: Fix any broken script references

### Story 4: Move Documentation Out of Code
**As a developer, I want code comments to be about code, not project documentation.**

**Acceptance Criteria:**
- Large comment blocks moved to markdown files
- Code comments are short and explain "why", not "what"
- Project documentation is in `/Documentation/` folder

**Steps:**
1. **Create docs folder**: Make `/Documentation/` in project root
2. **Extract big comments**: Copy large explanatory comments to `.md` files
3. **Update code comments**: Keep only technical "why" comments
4. **Reference docs**: Add links from code to relevant documentation

### Story 5: Organize Asset Files
**As a developer, I want unused assets removed so the project is lean.**

**Acceptance Criteria:**
- No unused prefabs, materials, or textures
- Assets are organized in logical folders
- Build size is noticeably smaller

**Steps:**
1. **Find unused assets**: Use Unity's built-in "Select Dependencies" tool
2. **Review carefully**: Make sure assets aren't loaded dynamically
3. **Delete unused**: Remove assets not referenced anywhere
4. **Organize remaining**: Put similar assets in same folders

---

## Quick Cleanup Script

Here's a simple editor script to help with the cleanup:

```csharp
// Assets/Editor/QuickCleanup.cs
using UnityEngine;
using UnityEditor;
using System.IO;

public class QuickCleanup : EditorWindow
{
    [MenuItem("WallChess/Quick Cleanup")]
    public static void ShowWindow()
    {
        GetWindow<QuickCleanup>("Quick Cleanup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Quick Project Cleanup", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "This will help clean up common clutter. Review each step carefully!", 
            MessageType.Info);
        
        if (GUILayout.Button("1. Delete MVP Folder"))
        {
            if (EditorUtility.DisplayDialog("Confirm", 
                "Delete entire MVP folder? This cannot be undone!", 
                "Yes", "Cancel"))
            {
                DeleteMVPFolder();
            }
        }
        
        if (GUILayout.Button("2. Find Backup Files"))
        {
            FindBackupFiles();
        }
        
        if (GUILayout.Button("3. List WallPlacer Scripts"))
        {
            ListWallPlacerScripts();
        }
        
        if (GUILayout.Button("4. Show Project Stats"))
        {
            ShowProjectStats();
        }
    }
    
    private void DeleteMVPFolder()
    {
        string mvpPath = Path.Combine(Application.dataPath, "Code", "MVP");
        if (Directory.Exists(mvpPath))
        {
            Directory.Delete(mvpPath, true);
            AssetDatabase.Refresh();
            Debug.Log("MVP folder deleted");
        }
        else
        {
            Debug.Log("MVP folder not found");
        }
    }
    
    private void FindBackupFiles()
    {
        string[] backupFiles = Directory.GetFiles(
            Application.dataPath, 
            "*backup*", 
            SearchOption.AllDirectories);
        
        Debug.Log($"Found {backupFiles.Length} backup files:");
        foreach (string file in backupFiles)
        {
            Debug.Log($"- {file}");
        }
    }
    
    private void ListWallPlacerScripts()
    {
        string[] wallPlacerFiles = Directory.GetFiles(
            Application.dataPath, 
            "*WallPlacer*", 
            SearchOption.AllDirectories);
        
        Debug.Log($"Found {wallPlacerFiles.Length} WallPlacer scripts:");
        foreach (string file in wallPlacerFiles)
        {
            FileInfo info = new FileInfo(file);
            Debug.Log($"- {Path.GetFileName(file)} (modified: {info.LastWriteTime:yyyy-MM-dd})");
        }
    }
    
    private void ShowProjectStats()
    {
        string[] allFiles = Directory.GetFiles(
            Application.dataPath, 
            "*.*", 
            SearchOption.AllDirectories);
        
        string[] csFiles = Directory.GetFiles(
            Application.dataPath, 
            "*.cs", 
            SearchOption.AllDirectories);
        
        Debug.Log($"Project stats:");
        Debug.Log($"- Total files: {allFiles.Length}");
        Debug.Log($"- C# scripts: {csFiles.Length}");
        
        long totalSize = 0;
        foreach (string file in allFiles)
        {
            totalSize += new FileInfo(file).Length;
        }
        
        Debug.Log($"- Total size: {totalSize / 1024 / 1024} MB");
    }
}
```

---

## Validation Checklist

After cleanup, verify these work:

### Functionality Test
- [ ] Game starts without errors
- [ ] Can start a new game
- [ ] Can move pieces
- [ ] Can place walls
- [ ] AI makes moves
- [ ] Game detects win/loss
- [ ] Can pause/resume

### Technical Test  
- [ ] Project compiles without errors
- [ ] No broken script references
- [ ] No missing prefab references  
- [ ] Build size is smaller
- [ ] Unity console is clean (no warnings)

### Before/After Comparison
- [ ] Record file count before cleanup
- [ ] Record project size before cleanup
- [ ] Compare after cleanup
- [ ] Document what was removed

---

## Common Issues & Solutions

**Issue**: "Script component missing" errors after cleanup
**Solution**: Some prefabs might reference deleted scripts. Find and fix these references.

**Issue**: Build fails after cleanup
**Solution**: Check for Resources.Load() calls to deleted assets.

**Issue**: Game behaves differently
**Solution**: You might have deleted something important. Use git to see what changed.

**Issue**: Can't find where code moved
**Solution**: Use your IDE's "Find in Files" to locate moved functionality.

---

## Expected Results

After this 2-hour cleanup:

- **30-40% fewer files** in the project
- **Cleaner folder structure** with logical organization
- **No dead code** cluttering searches and navigation
- **Smaller build size** and faster compilation
- **Clear starting point** for architectural refactoring

The project should feel much cleaner and be easier to navigate. You'll have a solid foundation for the more complex refactoring work ahead.

---

**Time Investment**: 2 hours  
**Risk Level**: Low (mostly deleting obviously dead code)  
**Impact**: High (much cleaner workspace for all future work)

---

**Page 61 of 61**  
**Next Document**: [Part 2: State Management Consolidation](02-state-management-consolidation.md)  
**Previous Document**: [Part 1: Project Setup](01-project-setup-prerequisites.md)  
**Master Index**: [00-master-index.md](00-master-index.md)  

---
*WallChessQuidor Refactoring Implementation Guide - Keep it simple, keep it clean*