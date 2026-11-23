using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dialog berhalaman dari LessonItemData ( Curriculum JSON).
/// Mendukung audioRef (.m4a/.wav) lewat DawanAudioPlayer path overload.
/// Tidak menulis Save v4.
/// </summary>
public class CurriculumDialoguePanel : MonoBehaviour
{
    private GameObject root;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI bodyLabel;
    private TextMeshProUGUI hintLabel;
    private Button nextButton;
    private Button closeButton;
    private Button audioButton;

    private readonly List<string> pages = new List<string>();
    private LessonItemData item;
    private int index;

    public void Build(GameObject panelRoot, TextMeshProUGUI title, TextMeshProUGUI body,
        TextMeshProUGUI hint, Button next, Button close, Button audio)
    {
        root = panelRoot;
        titleLabel = title;
        bodyLabel = body;
        hintLabel = hint;
        nextButton = next;
        closeButton = close;
        audioButton = audio;

        nextButton.onClick.AddListener(Next);
        closeButton.onClick.AddListener(Close);
        audioButton.onClick.AddListener(PlayAudio);
        root.SetActive(false);
    }

    public void Show(LessonItemData lessonItem)
    {
        if (root == null || lessonItem == null || lessonItem.type != "dialogue")
        {
            return;
        }

        if (lessonItem.lines == null || lessonItem.lines.Count == 0)
        {
            ToastView.Instance?.Show("Dialog ini belum memiliki teks.");
            return;
        }

        item = lessonItem;
        pages.Clear();
        for (int i = 0; i < lessonItem.lines.Count; i++)
        {
            DialogueLineData line = lessonItem.lines[i];
            if (line == null)
            {
                continue;
            }

            StringBuilder builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(line.speaker))
            {
                builder.Append(line.speaker).Append(": ");
            }

            builder.Append(line.text ?? string.Empty);
            pages.Add(builder.ToString());
        }

        if (pages.Count == 0)
        {
            ToastView.Instance?.Show("Dialog ini belum memiliki teks.");
            return;
        }

        titleLabel.text = "Dialog";
        index = 0;
        audioButton.interactable = !string.IsNullOrWhiteSpace(item.audioRef);
        Render();
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        UiModalState.Push(this);
    }

    public void Next()
    {
        if (root == null || !root.activeSelf)
        {
            return;
        }

        if (index + 1 < pages.Count)
        {
            index++;
            Render();
            return;
        }

        CurriculumService.Instance?.MarkItemCompleted(item);
        Close();
    }

    public void Close()
    {
        if (root == null || !root.activeSelf)
        {
            return;
        }

        root.SetActive(false);
        UiModalState.Pop(this);
    }

    private void OnDestroy()
    {
        UiModalState.Pop(this);
    }

    private void Render()
    {
        bodyLabel.text = pages[index];
        hintLabel.text = pages.Count > 1
            ? $"{index + 1}/{pages.Count} · lanjut"
            : "tutup";
        TextMeshProUGUI nextLabel = nextButton.GetComponentInChildren<TextMeshProUGUI>();
        if (nextLabel != null)
        {
            nextLabel.text = index + 1 < pages.Count ? "Lanjut" : "Tutup";
        }
    }

    private void PlayAudio()
    {
        if (item == null || string.IsNullOrWhiteSpace(item.audioRef))
        {
            return;
        }

        float volume = GameState.Ensure().Data.sfxVolume;
        if (!DawanAudioPlayer.TryPlayPath(item.audioRef, volume))
        {
            ToastView.Instance?.Show("Audio dialog belum tersedia di perangkat ini.");
        }
    }
}
