using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom dialogue panel used for NPC lines and learning material.
/// Only one instance lives in each scene (village and hut interior).
/// A body with blank lines between paragraphs becomes several pages; tapping
/// the panel or pressing E advances to the next page, the last tap closes.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleLabel;
    [SerializeField] private Text bodyLabel;
    [SerializeField] private Text hintLabel;

    private readonly List<string> pages = new List<string>();
    private int pageIndex;
    private int openedFrame = -1;
    private int closedFrame = -1;

    public bool IsOpen => panel != null && panel.activeSelf;

    /// <summary>True on the frame the dialogue was closed, so callers can ignore the same key press.</summary>
    public bool JustClosed => Time.frameCount == closedFrame;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Bind(GameObject panelObject, Text title, Text body, Text hint)
    {
        panel = panelObject;
        titleLabel = title;
        bodyLabel = body;
        hintLabel = hint;
        Hide();
    }

    public void Show(string title, string body)
    {
        if (panel == null)
        {
            return;
        }

        if (titleLabel != null)
        {
            titleLabel.text = title;
        }

        pages.Clear();
        SplitPages(body);
        if (pages.Count == 0)
        {
            pages.Add(string.Empty);
        }

        pageIndex = 0;
        RenderPage();

        panel.SetActive(true);
        openedFrame = Time.frameCount;
    }

    /// <summary>Panel tap / interact key: next page, or close on the last page.</summary>
    public void Advance()
    {
        if (!IsOpen)
        {
            return;
        }

        if (pageIndex + 1 < pages.Count)
        {
            pageIndex++;
            RenderPage();
            return;
        }

        Hide();
    }

    public void Hide()
    {
        if (panel != null && panel.activeSelf)
        {
            closedFrame = Time.frameCount;
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void SplitPages(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        string[] paragraphs = body.Replace("\r\n", "\n")
            .Split(new[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string paragraph in paragraphs)
        {
            string trimmed = paragraph.Trim();
            if (trimmed.Length > 0)
            {
                pages.Add(trimmed);
            }
        }
    }

    private void RenderPage()
    {
        if (bodyLabel != null)
        {
            bodyLabel.text = pages[pageIndex];
        }

        if (hintLabel == null)
        {
            return;
        }

        bool hasNext = pageIndex + 1 < pages.Count;
        string navigation = hasNext ? "sentuh untuk lanjut" : "sentuh untuk menutup";
        hintLabel.text = pages.Count > 1
            ? $"{pageIndex + 1}/{pages.Count} · {navigation}"
            : navigation;
    }

    private void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
        {
            Advance();
        }
    }
}
