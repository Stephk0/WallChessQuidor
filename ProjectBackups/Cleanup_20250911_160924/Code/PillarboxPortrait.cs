using UnityEngine;

[ExecuteAlways]
public class PillarboxPortrait : MonoBehaviour
{
    [Tooltip("Target portrait aspect (width:height). Example: 9x16")]
    public Vector2 targetAspect = new Vector2(9, 16);

    Camera cam;

    void OnEnable() => cam = GetComponent<Camera>();

    void Update()
    {
        if (cam == null) return;

        float target = targetAspect.x / targetAspect.y;        // e.g., 9/16 = 0.5625
        float window = (float)Screen.width / Screen.height;

        if (window > target)
        {
            // Window is wider than portrait → pillarbox (reduce viewport width)
            float scale = target / window;                      // 0–1
            float pad = (1f - scale) * 0.5f;
            cam.rect = new Rect(pad, 0f, scale, 1f);
        }
        else
        {
            // (Rare) window is taller/narrower than target → letterbox vertically
            float scale = window / target;                      // 0–1
            float pad = (1f - scale) * 0.5f;
            cam.rect = new Rect(0f, pad, 1f, scale);
        }
    }
}
