using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Notifikasi singkat di atas layar; tidak memblokir input.</summary>
public class ToastView : MonoBehaviour
{
    public static ToastView Instance { get; private set; }

    private readonly Queue<string> queue = new Queue<string>();
    private CanvasGroup group;
    private TextMeshProUGUI label;
    private Coroutine runner;

    private void Awake()
    {
        Instance = this;

        Canvas canvas = UiFactory.CreateCanvas("ToastCanvas", 32010, transform);
        Image panel = UiFactory.CreateImage(canvas.transform, "ToastPanel", null,
            new Color(0.12f, 0.09f, 0.06f, 0.94f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -120f), new Vector2(920f, 96f));

        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        label = UiFactory.CreateText(panel.transform, "Label", string.Empty, 30f,
            new Color(1f, 0.95f, 0.82f, 1f), TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(24f, 8f), new Vector2(-24f, -8f));
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show(string message, float seconds = 2.2f)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        queue.Enqueue(message);
        if (runner == null)
        {
            runner = StartCoroutine(Run(seconds));
        }
    }

    private IEnumerator Run(float seconds)
    {
        while (queue.Count > 0)
        {
            label.text = queue.Dequeue();
            yield return Fade(0f, 1f, 0.12f);
            yield return new WaitForSecondsRealtime(seconds);
            yield return Fade(1f, 0f, 0.18f);
        }

        runner = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(time / duration));
            yield return null;
        }

        group.alpha = to;
    }
}
