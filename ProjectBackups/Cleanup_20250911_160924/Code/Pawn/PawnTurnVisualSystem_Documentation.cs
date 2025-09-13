/*
=== PAWN TURN VISUAL SYSTEM ===

**Model-View** principle for **pawn turn visuals** in Wall Chess.

== COMPONENTS ==

**PawnTurnVisualController** (polling-based):
- Monitors **gameManager.activePlayerIndex** every 0.1s
- **Updates visual effects** when turn changes
- **Simple** and **reliable** approach

**PawnTurnVisualControllerEvents** (event-based):
- **Listens** to turn change **events**
- More **efficient** than polling
- Requires **GameManagerTurnEventIntegration**

**GameManagerTurnEventIntegration**:
- **Extends** WallChessGameManager with **event broadcasting**
- **No modification** of existing game logic
- Follows **model-view** separation

**TurnEventBroadcaster**:
- **Static event system** for turn changes
- **Decouples** visual from game logic
- **Clean** and **reusable**

== SETUP INSTRUCTIONS ==

**Step 1**: **Pawn Prefab Setup**
- Create **child GameObjects** under pawn prefab:
  - "**ActiveEffect**" (shown when it's this pawn's turn)  
  - "**InactiveEffect**" (shown when it's not this pawn's turn)
- Attach **PawnTurnVisualController** to pawn prefab root
- **Assign** activeEffect and inactiveEffect references
- **Or enable** autoFindEffects to find by name

**Step 2**: **GameManager Integration** 
- Attach **GameManagerTurnEventIntegration** to same GameObject as WallChessGameManager
- **Enable** eventBroadcasting if using event-based system
- This **extends** functionality without **modifying** existing code

**Step 3**: **Choose Controller Type**
- Use **PawnTurnVisualController** for **simple polling** (default)
- Use **PawnTurnVisualControllerEvents** for **event-driven** efficiency
- **Both** work with same prefab setup

== FEATURES ==

**Auto-Detection**:
- **Automatically** finds pawn index by comparing GameObject to gameManager.pawns[].avatar
- **Auto-finds** effect objects by name ("ActiveEffect"/"InactiveEffect")

**Debug Support**:
- **Enable** enableDebugLogs for detailed turn change logging
- **Context menu** actions for manual testing
- **Compatible** with WallChessGameManager.debugMode

**Flexible Effects**:
- **Any GameObject** can be activeEffect/inactiveEffect
- **Particles**, **lights**, **UI elements**, **meshes**, etc.
- **Multiple** effects per state supported

== EXAMPLES ==

**Basic Setup**:
Pawn Prefab:
├── PawnTurnVisualController
├── Model (3D mesh)
├── ActiveEffect (Particle System - glowing)
└── InactiveEffect (Empty GameObject - hidden)

**Advanced Setup**:
Pawn Prefab:  
├── PawnTurnVisualControllerEvents
├── Model
├── ActiveEffect
│   ├── Glow Particles
│   ├── Highlight Ring
│   └── UI Turn Indicator
└── InactiveEffect
    ├── Dimmed Material
    └── Idle Animation

== INTEGRATION WITH EXISTING CODE ==

**No Changes Required** to:
- WallChessGameManager
- PlayerControllerV2  
- Existing pawn movement logic
- Grid system

**Purely Additive**:
- **Extends** functionality via composition
- **Follows** existing architecture patterns
- **Respects** model-view principle

== PERFORMANCE ==

**Polling Version**:
- **10 FPS** update rate (every 0.1s)
- **Minimal** CPU impact for 2-4 pawns
- **Rock solid** reliability

**Event Version**:
- **Zero polling** overhead
- **Instant** visual updates
- **Slightly** more complex setup

== TROUBLESHOOTING ==

**Effects not switching?**
- **Check** activeEffect/inactiveEffect assignments
- **Enable** debug logs to see turn changes
- **Verify** pawn avatar is properly associated with gameManager.pawns[]

**Events not firing?**
- **Ensure** GameManagerTurnEventIntegration is attached
- **Check** enableEventBroadcasting is true
- Use **Manual Trigger** context menu for testing
*/