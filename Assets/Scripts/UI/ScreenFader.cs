using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Fade hitam persisten antar scene.</summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    private Image cover;
    private Canvas canvas;

    private void Awake()
    {
        Instance = this;

        GameObject canvasObject = new GameObject("FadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("Cover");
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        cover = imageObject.AddComponent<Image>();
        cover.color = new Color(0f, 0f, 0f, 0f);
        cover.raycastTarget = false;
    }

    public IEnumerator FadeOut(float duration = 0.22f)
    {
        cover.raycastTarget = true;
        yield return Fade(0f, 1f, duration);
    }

    public IEnumerator FadeIn(float duration = 0.22f)
    {
        yield return Fade(1f, 0f, duration);
        cover.raycastTarget = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;
        Color color = cover.color;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(from, to, Mathf.Clamp01(time / duration));
            cover.color = color;
            yield return null;
        }

        color.a = to;
        cover.color = color;
    }
}
