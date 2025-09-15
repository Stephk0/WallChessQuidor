using UnityEngine;

namespace WallChess
{
    /// <summary>
    /// Simple setup guide for the curve-based dangle animation system (no pivot point)
    /// </summary>
    public class SimpleDangleAnimationGuide : MonoBehaviour
    {
        [Header("Simple Curve-Based Dangle Animation Guide")]
        [TextArea(20, 30)]
        [SerializeField] private string setupGuide = 
@"SIMPLE DANGLE ANIMATION SETUP (SIMPLIFIED):

This is a much cleaner, curve-based dangle animation system without complex physics.

1. PAWN PREFAB STRUCTURE:
   PawnRoot (with SimplePawnDangleAnimation component)
   └── Visual (your mesh/model) - OR assign any child transform

2. REQUIRED COMPONENTS:
   - SimplePawnDangleAnimation (simplified version)
   - AvatarDragController (handles drag integration)

3. COMPONENT SETTINGS:

   SimplePawnDangleAnimation:
   - Visual Transform: Assign your visual mesh (auto-detects if not set)
   - Animation Speed: 2.0 (how fast the dangle cycles)
   - Max Rotation Angle: 15° (maximum dangle amount)
   - Curve Intensity Multiplier: 1.0 (scales the curve values)
   - Loop Animation: ✓ (continuous dangle)

4. ANIMATION CURVE SETUP:
   The Dangle Rotation Curve controls the entire animation:
   - Time 0.0: Value 0 (center position)
   - Time 0.25: Value 1 (maximum right)
   - Time 0.5: Value 0 (back to center)
   - Time 0.75: Value -1 (maximum left)
   - Time 1.0: Value 0 (back to center)

   This creates a smooth pendulum motion!

5. TRANSITION SETTINGS:
   - Fade In Duration: 0.2s (how long to start dangling)
   - Fade Out Duration: 0.3s (how long to stop dangling)
   - Transition Curve: Ease in/out for smooth starts/stops

6. EASY CUSTOMIZATION:
   - Change Animation Speed for faster/slower dangling
   - Adjust Max Rotation Angle for more/less dramatic dangle
   - Modify Curve Intensity Multiplier for fine-tuning strength
   - Modify the curve shape for different motion patterns:
     * Linear = robotic motion
     * Smooth curves = natural pendulum
     * Sharp peaks = bouncy motion

7. CONTEXT MENU TESTING:
   Right-click SimplePawnDangleAnimation component:
   - 'Test Start Dangle' - See the animation in editor
   - 'Test Stop Dangle' - Stop the animation
   - 'Test Intensity 0.5x/2x' - Try different intensities
   - 'Create Default Curve' - Reset to default pendulum curve

8. AUTOMATIC INTEGRATION:
   - Starts when player drags pawn
   - Stops when drag ends
   - No complex setup needed beyond adding the component!

WHY THIS SIMPLIFIED VERSION IS BETTER:
✓ No unnecessary pivot point
✓ Direct visual transform rotation
✓ Predictable, smooth motion
✓ Easy to customize with curves
✓ Better performance
✓ More reliable results
✓ Simple to understand and modify
✓ Auto-detects visual transform";

        [Header("Animation Curve Examples")]
        [TextArea(10, 15)]
        [SerializeField] private string curveExamples =
@"CURVE PATTERN IDEAS:

GENTLE PENDULUM (Default):
0.0 → 0, 0.25 → 1, 0.5 → 0, 0.75 → -1, 1.0 → 0

BOUNCY DANGLE:
0.0 → 0, 0.1 → 1, 0.3 → 0, 0.4 → -1, 0.6 → 0, 1.0 → 0

ASYMMETRIC WOBBLE:
0.0 → 0, 0.3 → 1, 0.5 → 0, 0.8 → -0.5, 1.0 → 0

SHAKE/JITTER:
Multiple small peaks and valleys for nervous energy

TIP: Use the curve editor in Unity to visually design your motion!

CURVE INTENSITY MULTIPLIER:

The Curve Intensity Multiplier scales your entire curve:
- 1.0 = Normal intensity (curve values used as-is)
- 0.5 = Half intensity (gentle, subtle dangle)
- 2.0 = Double intensity (dramatic, exaggerated dangle)
- 0.0 = No dangle (completely still)

This lets you adjust animation strength without redrawing curves!

Example: If your curve goes from -1 to +1, and Max Rotation is 15°:
- Multiplier 1.0 = 15° dangle range
- Multiplier 0.5 = 7.5° dangle range  
- Multiplier 2.0 = 30° dangle range

TIP: Use Context Menu → 'Test Intensity 0.5x/2x' to preview!";

        [ContextMenu("Add Simple Animation To This GameObject")]
        public void AddSimpleAnimation()
        {
            if (GetComponent<SimplePawnDangleAnimation>() == null)
            {
                gameObject.AddComponent<SimplePawnDangleAnimation>();
                Debug.Log("Added SimplePawnDangleAnimation to " + name);
            }
            else
            {
                Debug.Log("SimplePawnDangleAnimation already exists on " + name);
            }
        }
    }
}