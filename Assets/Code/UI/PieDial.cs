using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(RectTransform))]
public class PieDial : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
[Header("References")]
[SerializeField] private RectTransform baseRect; // The clickable circle
[SerializeField] private RectTransform compassRect; // Shown while held
[SerializeField] private RectTransform dotRect; // Draggable dot shown while held


[Header("Behavior")]
[Tooltip("If true, the compass graphic will rotate to face the drag direction.")]
[SerializeField] private bool rotateCompass = true;


[Tooltip("When true, the dot is clamped to the dial radius while dragging.")]
[SerializeField] private bool clampDotToRadius = true;


[Tooltip("Minimum normalized magnitude required to confirm on release (0..1 of radius). 1.0 means must cross the edge.")]
[Range(0f, 1.5f)]
[SerializeField] private float confirmMagnitudeNormalized = 1.0f; // 1.0 == at or beyond edge


[Tooltip("Snap the resulting direction to N sectors (e.g., 8 for 8-way). Use 0 for no snapping.")]
[SerializeField] private int snapSectors = 0; // 0 = continuous


[Header("Events")]
public Vector2Event OnDirectionConfirmed; // Fired when user releases beyond threshold
public Vector2Event OnDirectionChanged; // Fired during drag (normalized 0..1 magnitude)


[System.Serializable]
public class Vector2Event : UnityEvent<Vector2> { }


private Canvas _rootCanvas;
private Camera _uiCamera;
private RectTransform _selfRect;
private bool _isHeld;
private Vector2 _centerLocal;
private float _radius; // in local space units (half of min(width,height))


private void Awake()
{
_selfRect = GetComponent<RectTransform>();
if (baseRect == null) baseRect = _selfRect;
_rootCanvas = GetComponentInParent<Canvas>();
if (_rootCanvas != null)
{
_uiCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceCamera ? _rootCanvas.worldCamera : null;
}
}


private void Start()
{
CacheCenterAndRadius();
HideIndicators();
}


private void CacheCenterAndRadius()
{
// Center in local space
_centerLocal = baseRect.rect.center;
// Radius = half of the min dimension
_radius = Mathf.Min(baseRect.rect.width, baseRect.rect.height) * 0.5f;
}


public void OnPointerDown(PointerEventData eventData)
{
_isHeld = true;
CacheCenterAndRadius();
ShowIndicators();
UpdateDrag(eventData);
}


public void OnDrag(PointerEventData eventData)
{
if (!_isHeld) return;
UpdateDrag(eventData);
}


public void OnPointerUp(PointerEventData eventData)
{
if (!_isHeld) return;
_isHeld = false;

// Calculate final vector at release
Vector2 localPoint;
if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, _uiCamera, out localPoint))
{
HideIndicators();
return;
}


Vector2 delta = localPoint - _centerLocal;
float mag = delta.magnitude;
Vector2 dir = mag > 0.0001f ? delta / mag : Vector2.zero;
float normalizedMag = _radius > 0f ? mag / _radius : 0f;


bool confirmed = normalizedMag >= confirmMagnitudeNormalized;
if (confirmed)
{
Vector2 finalDir = dir;
if (snapSectors > 0)
finalDir = SnapDirection(dir, snapSectors);


OnDirectionConfirmed?.Invoke(finalDir);
}


HideIndicators();
}


private void UpdateDrag(PointerEventData eventData)
{
Vector2 localPoint;
if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, _uiCamera, out localPoint))
return;


Vector2 delta = localPoint - _centerLocal;
float mag = delta.magnitude;
Vector2 dir = mag > 0.0001f ? delta / mag : Vector2.right; // default


// Move dot
if (dotRect != null)
{
Vector2 target = delta;
if (clampDotToRadius && mag > _radius)
target = dir * _radius;


dotRect.anchoredPosition = target;
}


// Rotate compass
if (rotateCompass && compassRect != null)
{
float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
compassRect.localRotation = Quaternion.Euler(0, 0, ang - 90f); // assume art points up
}


float normalizedMag = _radius > 0f ? Mathf.Clamp01(mag / _radius) : 0f;
OnDirectionChanged?.Invoke(dir * normalizedMag);
}


private void ShowIndicators()
{
if (compassRect != null) compassRect.gameObject.SetActive(true);
if (dotRect != null)
{
dotRect.gameObject.SetActive(true);
dotRect.anchoredPosition = Vector2.zero; // start at center
}
}


private void HideIndicators()
{
if (compassRect != null) compassRect.gameObject.SetActive(false);
if (dotRect != null) dotRect.gameObject.SetActive(false);
}


private static Vector2 SnapDirection(Vector2 dir, int sectors)
{
    if (sectors <= 0) return dir;
    float angle = Mathf.Atan2(dir.y, dir.x); // -pi..pi
    float sectorSize = (2f * Mathf.PI) / sectors;
    int sectorIndex = Mathf.RoundToInt(angle / sectorSize);
    float snappedAngle = sectorIndex * sectorSize;
    return new Vector2(Mathf.Cos(snappedAngle), Mathf.Sin(snappedAngle));
}


#if UNITY_EDITOR
private void OnValidate()
{
    if (baseRect == null) baseRect = GetComponent<RectTransform>();
    if (Application.isPlaying) CacheCenterAndRadius();
}
#endif
}