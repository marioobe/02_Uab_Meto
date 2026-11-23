using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pembuat UI runtime yang dipakai semua panel (TMP + Canvas Scaler Android).</summary>
public static class UiFactory
{
    public static readonly Color Ink = new Color32(58, 36, 20, 255);
    public static readonly Color Cream = new Color32(246, 232, 198, 255);
    public static readonly Color Dim = new Color(0f, 0f, 0f, 0.62f);

    private static TMP_FontAsset cachedFont;
    private static Sprite cachedWhite;

    /// <summary>
    /// Sprite 1x1 putih. Image tanpa sprite tidak menggambar apa pun di Unity 6,
    /// jadi semua latar solid (dim, kotak HUD, tombol) memakai sprite ini.
    /// </summary>
    public static Sprite WhiteSprite
    {
        get
        {
            if (cachedWhite != null)
            {
                return cachedWhite;
            }

            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            cachedWhite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            cachedWhite.name = "UiWhite";
            return cachedWhite;
        }
    }

    public static TMP_FontAsset Font
    {
        get
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            cachedFont = TMP_Settings.defaultFontAsset;
            if (cachedFont == null)
            {
                Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtin != null)
                {
                    cachedFont = TMP_FontAsset.CreateFontAsset(builtin);
                }
            }

            return cachedFont;
        }
    }

    public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static RectTransform CreateSafeArea(Transform parent)
    {
        GameObject go = new GameObject("SafeArea", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.AddComponent<SafeAreaFitter>();
        return rect;
    }

    public static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    public static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float fontSize,
        Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 size, bool bold = false)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, size);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        if (bold)
        {
            label.fontStyle = FontStyles.Bold;
        }

        return label;
    }

    public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size,
        bool raycastTarget = true)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : WhiteSprite;
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    /// <summary>Garis tepi gelap tipis agar teks tetap terbaca di atas latar terang.</summary>
    public static void AddOutline(TMP_Text label, float width = 0.18f)
    {
        if (label == null)
        {
            return;
        }

        label.outlineWidth = width;
        label.outlineColor = new Color32(28, 18, 10, 220);
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize,
        Color color, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = Font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        return label;
    }

    public static Button CreateButton(Transform parent, string name, string labelText, Sprite sprite,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size,
        Color backgroundColor, Color textColor, float fontSize = 28f)
    {
        Image image = CreateImage(parent, name, sprite, backgroundColor, anchorMin, anchorMax, pivot,
            anchoredPosition, size);
        StylePanel(image);

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        button.colors = colors;

        CreateText(image.transform, "Label", labelText, fontSize, textColor,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one,
            new Vector2(12f, 6f), new Vector2(-12f, -6f));

        return button;
    }

    public static void StylePanel(Image image)
    {
        if (image == null || image.sprite == null)
        {
            return;
        }

        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.5f;
    }

    public static void AddDimBackground(Transform parent, out Image dim)
    {
        dim = CreateImage(parent, "Dim", null, Dim, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
    }

    public static void SetText(TextMeshProUGUI label, string text)
    {
        if (label != null)
        {
            label.text = text;
        }
    }
}
