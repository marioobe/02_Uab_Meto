using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Panel dialog berhalaman dengan typewriter TMP.</summary>
public class DialoguePanel : MonoBehaviour
{
    private GameObject panelObject;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;
    private TextMeshProUGUI hintLabel;

    private string[] pages = new string[0];
    private int pageIndex;
    private float visibleCharacters;
    private bool typing;
    private Action onClose;

    public bool IsOpen => panelObject != null && panelObject.activeSelf;

    public void Build(GameObject panel, TextMeshProUGUI title, TextMeshProUGUI body, TextMeshProUGUI hint)
    {
        panelObject = panel;
        titleLabel = title;
        bodyLabel = body;
        hintLabel = hint;
        panelObject.SetActive(false);
    }

    public void Show(string title, string[] content, Action closed = null)
    {
        if (panelObject == null)
        {
            return;
        }

        titleLabel.text = string.IsNullOrEmpty(title) ? "Informasi" : title;
        pages = content != null && content.Length > 0 ? content : new[] { string.Empty };
        pageIndex = 0;
        onClose = closed;
        panelObject.SetActive(true);
        panelObject.transform.SetAsLastSibling();
        UiModalState.Push(this);
        RenderPage(true);
    }

    public void ShowMessage(string title, string body, Action closed = null)
    {
        string normalized = (body ?? string.Empty).Replace("\r\n", "\n");
        string[] split = normalized.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        Show(title, split, closed);
    }

    public void Advance()
    {
        if (!IsOpen || !ReferenceEquals(UiModalState.Top, this))
        {
            return;
        }

        if (typing)
        {
            visibleCharacters = bodyLabel.text != null ? bodyLabel.text.Length : 0;
            bodyLabel.maxVisibleCharacters = Mathf.CeilToInt(visibleCharacters);
            typing = false;
            UpdateHint();
            return;
        }

        if (pageIndex + 1 < pages.Length)
        {
            pageIndex++;
            RenderPage(true);
            return;
        }

        Hide();
    }

    public void Hide()
    {
        if (!IsOpen)
        {
            return;
        }

        panelObject.SetActive(false);
        UiModalState.Pop(this);
        Action callback = onClose;
        onClose = null;
        typing = false;
        callback?.Invoke();
    }

    private void RenderPage(bool animate)
    {
        string page = pages[Mathf.Clamp(pageIndex, 0, pages.Length - 1)] ?? string.Empty;
        bodyLabel.text = page;
        bodyLabel.maxVisibleCharacters = animate ? 0 : page.Length;
        visibleCharacters = 0f;
        typing = animate && page.Length > 0;
        UpdateHint();
    }

    private void Update()
    {
        if (!IsOpen || !ReferenceEquals(UiModalState.Top, this))
        {
            return;
        }

        if (typing)
        {
            visibleCharacters += Time.unscaledDeltaTime * 55f;
            bodyLabel.maxVisibleCharacters = Mathf.CeilToInt(visibleCharacters);
            if (bodyLabel.maxVisibleCharacters >= bodyLabel.text.Length)
            {
                typing = false;
                UpdateHint();
            }
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            Advance();
        }
    }

    private void UpdateHint()
    {
        if (hintLabel == null)
        {
            return;
        }

        bool hasNext = pageIndex + 1 < pages.Length;
        string navigation = hasNext ? "sentuh untuk lanjut" : "sentuh untuk menutup";
        hintLabel.text = pages.Length > 1
            ? $"{pageIndex + 1}/{pages.Length} · {navigation}"
            : navigation;
    }

    private void OnDestroy()
    {
        UiModalState.Pop(this);
    }
}
