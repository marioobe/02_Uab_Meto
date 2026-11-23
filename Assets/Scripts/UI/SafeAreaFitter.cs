using UnityEngine;

/// <summary>Menyesuaikan UI dengan safe area (notch) Android.</summary>
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea;
    private Vector2Int lastScreen;

    private void Awake()
    {
        rect = (RectTransform)transform;
        Apply();
    }

    private void Update()
    {
        if (lastSafeArea != Screen.safeArea ||
            lastScreen.x != Screen.width || lastScreen.y != Screen.height)
        {
            Apply();
        }
    }

    private void Apply()
    {
        if (rect == null)
        {
            return;
        }

        Rect safe = Screen.safeArea;
        lastSafeArea = safe;
        lastScreen = new Vector2Int(Screen.width, Screen.height);

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
