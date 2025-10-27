// Wall Chess Application FSM - README
//
// Overview:
// This refactor implements a main application FSM that handles the core loop:
// Start > MainMenu > Settings > Game > GameOver > MainMenu
//
// Setup Instructions:
//
// 1. Add MainApplicationController to your main GameObject (alongside WallChessGameManager)
// 2. Optionally add ApplicationFSMTester for testing
// 3. Configure MainApplicationController:
//    - Enable Application FSM: true
//    - Debug Application FSM: true (for development)
//    - Start In Menu State: true (skip StartState for faster testing)
//
// Testing Controls:
// - P: Start new game
// - S: Open settings
// - M: Return to main menu
// - V: Simulate victory
// - Tab: Print debug info
//
// Settings Controls (when in SettingsState):
// - 2/4: Set player count
// - +/-: Adjust grid size
// - Enter: Start game
// - Escape: Return to menu
//
// Architecture:
// - StartState: Initial loading
// - MenuState: Main menu navigation
// - SettingsState: Pre-session configuration
// - GameState: Wraps existing gameplay FSM
// - ApplicationGameOverState: Post-game results
//
// The system maintains full backwards compatibility with existing WallChessGameManager.
// If no MainApplicationController is present, uses legacy initialization.

using UnityEngine;

namespace WallChess.Core.ApplicationStates
{
    /// <summary>
    /// This file serves as documentation for the Application FSM system.
    /// See the comments above for setup and usage instructions.
    /// </summary>
    public class ApplicationFSMDocumentation
    {
        // This class exists purely for documentation purposes
    }
}