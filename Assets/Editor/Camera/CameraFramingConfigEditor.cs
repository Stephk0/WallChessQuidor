using UnityEngine;
using UnityEditor;
using WallChess.Core.Config;
using WallChess.View.Camera;

namespace WallChess.Editor.Camera
{
    /// <summary>
    /// Custom editor for CameraFramingConfig with scene gizmos and handle manipulation.
    /// Allows visual editing of the perspective trapezoid with mirrored corner handles.
    /// </summary>
    [CustomEditor(typeof(CameraFramingConfig))]
    public class CameraFramingConfigEditor : UnityEditor.Editor
    {
        private CameraFramingConfig _config;
        private bool _previewInScene = true;
        private bool _showComputedValues = true;

        private static bool _lockMainCamera = false;
        private static Vector3 _lockedCameraPosition;
        private static Quaternion _lockedCameraRotation;

        // Colors
        private static readonly Color TrapezoidFillColor = new Color(0.2f, 0.8f, 0.2f, 0.15f);
        private static readonly Color TrapezoidOutlineColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        private static readonly Color HandleColor = Color.white;
        private static readonly Color GridBoundsColor = new Color(0.8f, 0.6f, 0.2f, 0.5f);

        private void OnEnable()
        {
            _config = (CameraFramingConfig)target;
            SceneView.duringSceneGui += OnSceneGUI;

            // Store camera position if locking
            if (_lockMainCamera)
            {
                CacheCameraTransform();
            }
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

            // Toggle buttons
            _previewInScene = EditorGUILayout.Toggle("Preview in Scene", _previewInScene);

            EditorGUI.BeginChangeCheck();
            _lockMainCamera = EditorGUILayout.Toggle("Lock Main Camera", _lockMainCamera);
            if (EditorGUI.EndChangeCheck() && _lockMainCamera)
            {
                CacheCameraTransform();
            }

            EditorGUILayout.Space(5);

            // Apply button
            if (GUILayout.Button("Apply to Main Camera"))
            {
                ApplyToMainCamera();
            }

            EditorGUILayout.Space(10);

            // Computed values foldout
            _showComputedValues = EditorGUILayout.Foldout(_showComputedValues, "Computed Values", true);
            if (_showComputedValues)
            {
                EditorGUI.indentLevel++;
                DrawComputedValues();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawComputedValues()
        {
            var trapezoid = _config.Trapezoid;

            EditorGUILayout.LabelField($"Convergence Ratio: {trapezoid.ConvergenceRatio:F3}");

            float pitchDeg = _config.CalculatePitchForConvergence(_config.fieldOfView);
            EditorGUILayout.LabelField($"Calculated Pitch: {pitchDeg:F1}°");

            // Find active framing controller for more info
            var controller = FindFirstObjectByType<CameraFramingController>();
            if (controller != null && controller.FramingConfig == _config)
            {
                EditorGUILayout.LabelField($"Camera Distance: {controller.CurrentFrameBounds.size.x:F1} units");
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_previewInScene || _config == null)
                return;

            // Enforce camera lock
            if (_lockMainCamera)
            {
                EnforceCameraLock();
            }

            // Draw trapezoid
            DrawTrapezoidGizmo();

            // Draw and handle manipulation
            DrawAndProcessHandles();

            // Draw grid bounds reference (if controller exists)
            DrawGridBoundsReference();

            // Draw lock indicator
            if (_lockMainCamera)
            {
                DrawLockIndicator(sceneView);
            }
        }

        private void DrawTrapezoidGizmo()
        {
            var trapezoid = _config.Trapezoid;
            var corners = trapezoid.GetCornersArray();

            // Draw filled quad
            Handles.DrawSolidRectangleWithOutline(corners, TrapezoidFillColor, TrapezoidOutlineColor);

            // Draw center lines
            Handles.color = Color.yellow * 0.7f;
            Vector3 center = Vector3.zero;
            Handles.DrawLine(center + Vector3.left * 0.5f, center + Vector3.right * 0.5f, 1f);
            Handles.DrawLine(center + Vector3.back * 0.5f, center + Vector3.forward * 0.5f, 1f);

            // Draw labels
            Handles.Label(corners[0] + Vector3.left * 0.3f + Vector3.back * 0.3f, "Near", EditorStyles.miniLabel);
            Handles.Label(corners[3] + Vector3.left * 0.3f + Vector3.forward * 0.3f, "Far", EditorStyles.miniLabel);
        }

        private void DrawAndProcessHandles()
        {
            var trapezoid = _config.Trapezoid;
            var (bl, br, tr, tl) = trapezoid.GetCorners();

            Handles.color = HandleColor;
            float handleSize = HandleUtility.GetHandleSize(Vector3.zero) * 0.08f;

            // Near width handle (bottom-right, mirrored to bottom-left)
            EditorGUI.BeginChangeCheck();
            Vector3 newBR = Handles.FreeMoveHandle(
                br,
                handleSize,
                Vector3.one * 0.1f,
                Handles.SphereHandleCap
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_config, "Adjust Near Width");
                // Constrain to XZ plane and positive X
                newBR.y = 0;
                newBR.z = -trapezoid.HalfHeight; // Keep at near edge
                _config.nearWidth = Mathf.Max(0.1f, Mathf.Abs(newBR.x) * 2f);
                EditorUtility.SetDirty(_config);
            }

            // Far width handle (top-right, mirrored to top-left)
            EditorGUI.BeginChangeCheck();
            Vector3 newTR = Handles.FreeMoveHandle(
                tr,
                handleSize,
                Vector3.one * 0.1f,
                Handles.SphereHandleCap
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_config, "Adjust Far Width");
                // Constrain to XZ plane and positive X
                newTR.y = 0;
                newTR.z = trapezoid.HalfHeight; // Keep at far edge
                _config.farWidth = Mathf.Max(0.1f, Mathf.Abs(newTR.x) * 2f);
                EditorUtility.SetDirty(_config);
            }

            // Far edge handle (top center) - moves far edge independently
            Vector3 topCenter = (tl + tr) * 0.5f;
            EditorGUI.BeginChangeCheck();
            Vector3 newTopCenter = Handles.Slider(
                topCenter,
                Vector3.forward,
                HandleUtility.GetHandleSize(topCenter) * 0.15f,
                Handles.ConeHandleCap,
                0.1f
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_config, "Adjust Far Edge");
                // Calculate new far Z position
                float newFarZ = newTopCenter.z;
                float currentNearZ = bl.z;
                // Update height and offset to achieve new far edge position
                float newHeight = Mathf.Max(0.1f, newFarZ - currentNearZ);
                float newCenterZ = (newFarZ + currentNearZ) * 0.5f;
                _config.trapezoidHeight = newHeight;
                _config.centerOffsetZ = newCenterZ;
                EditorUtility.SetDirty(_config);
            }

            // Near edge handle (bottom center) - moves near edge independently
            Vector3 bottomCenter = (bl + br) * 0.5f;
            EditorGUI.BeginChangeCheck();
            Vector3 newBottomCenter = Handles.Slider(
                bottomCenter,
                Vector3.forward,
                HandleUtility.GetHandleSize(bottomCenter) * 0.15f,
                Handles.ConeHandleCap,
                0.1f
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_config, "Adjust Near Edge");
                // Calculate new near Z position
                float newNearZ = newBottomCenter.z;
                float currentFarZ = tr.z;
                // Update height and offset to achieve new near edge position
                float newHeight = Mathf.Max(0.1f, currentFarZ - newNearZ);
                float newCenterZ = (currentFarZ + newNearZ) * 0.5f;
                _config.trapezoidHeight = newHeight;
                _config.centerOffsetZ = newCenterZ;
                EditorUtility.SetDirty(_config);
            }

            // Draw mirrored handles as visual indicators (non-interactive)
            Handles.color = new Color(1f, 1f, 1f, 0.3f);
            Handles.SphereHandleCap(0, bl, Quaternion.identity, handleSize * 0.8f, EventType.Repaint);
            Handles.SphereHandleCap(0, tl, Quaternion.identity, handleSize * 0.8f, EventType.Repaint);
        }

        private void DrawGridBoundsReference()
        {
            // Try to find a BoardConfig to show grid bounds
            var controller = FindFirstObjectByType<CameraFramingController>();
            BoardConfig boardConfig = controller?.BoardConfig;

            if (boardConfig == null)
            {
                // Try to find any BoardConfig asset
                var guids = AssetDatabase.FindAssets("t:BoardConfig");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    boardConfig = AssetDatabase.LoadAssetAtPath<BoardConfig>(path);
                }
            }

            if (boardConfig != null)
            {
                float width = boardConfig.BoardWorldWidth;
                float height = boardConfig.BoardWorldHeight;

                Handles.color = GridBoundsColor;
                var gridCorners = new Vector3[]
                {
                    new Vector3(-width * 0.5f, 0, -height * 0.5f),
                    new Vector3(width * 0.5f, 0, -height * 0.5f),
                    new Vector3(width * 0.5f, 0, height * 0.5f),
                    new Vector3(-width * 0.5f, 0, height * 0.5f),
                };

                Handles.DrawSolidRectangleWithOutline(
                    gridCorners,
                    new Color(0.8f, 0.6f, 0.2f, 0.1f),
                    GridBoundsColor
                );

                Handles.Label(gridCorners[1] + Vector3.right * 0.3f, $"Grid {boardConfig.gridWidth}x{boardConfig.gridHeight}", EditorStyles.miniLabel);
            }
        }

        private void DrawLockIndicator(SceneView sceneView)
        {
            Handles.BeginGUI();

            var rect = new Rect(10, 10, 120, 22);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(rect, " Camera Locked", EditorStyles.boldLabel);

            Handles.EndGUI();
        }

        private void CacheCameraTransform()
        {
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null)
            {
                _lockedCameraPosition = mainCamera.transform.position;
                _lockedCameraRotation = mainCamera.transform.rotation;
            }
        }

        private void EnforceCameraLock()
        {
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null) return;

            // Check if camera moved
            if (mainCamera.transform.position != _lockedCameraPosition ||
                mainCamera.transform.rotation != _lockedCameraRotation)
            {
                // Restore locked position
                mainCamera.transform.position = _lockedCameraPosition;
                mainCamera.transform.rotation = _lockedCameraRotation;
            }
        }

        private void ApplyToMainCamera()
        {
            var mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("No main camera found");
                return;
            }

            // Try to find or add CameraFramingController
            var controller = mainCamera.GetComponent<CameraFramingController>();
            if (controller == null)
            {
                controller = mainCamera.gameObject.AddComponent<CameraFramingController>();
            }

            // Find BoardConfig
            BoardConfig boardConfig = null;
            var guids = AssetDatabase.FindAssets("t:BoardConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                boardConfig = AssetDatabase.LoadAssetAtPath<BoardConfig>(path);
            }

            if (boardConfig == null)
            {
                Debug.LogWarning("No BoardConfig found in project");
                return;
            }

            // Set references via serialized properties
            var so = new SerializedObject(controller);
            so.FindProperty("framingConfig").objectReferenceValue = _config;
            so.FindProperty("boardConfig").objectReferenceValue = boardConfig;
            so.ApplyModifiedProperties();

            // Apply framing
            controller.Initialize(boardConfig, _config);

            // Update lock position
            if (_lockMainCamera)
            {
                CacheCameraTransform();
            }

            Debug.Log($"Camera framing applied: pitch={_config.CalculatePitchForConvergence(_config.fieldOfView):F1}°");
        }
    }
}
