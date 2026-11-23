using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Latihan pilihan ganda dari LessonItemData (Curriculum JSON).
/// Tidak menyentuh Save v4 dan tidak memakai hati/XP global.
/// </summary>
public class CurriculumExercisePanel : MonoBehaviour
{
    private static readonly Color OptionColor = new Color(0.96f, 0.92f, 0.82f, 1f);
    private static readonly Color CorrectColor = new Color(0.55f, 0.85f, 0.5f, 1f);
    private static readonly Color WrongColor = new Color(0.9f, 0.45f, 0.4f, 1f);

    private GameObject root;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI questionLabel;
    private RectTransform optionsRoot;
    private TextMeshProUGUI feedbackLabel;
    private Button closeButton;

    private LessonItemData item;
    private Action onCompleted;
    private bool answered;
    private int correctIndex;

    public void Build(GameObject panelRoot, TextMeshProUGUI title, TextMeshProUGUI question,
        RectTransform options, TextMeshProUGUI feedback, Button close)
    {
        root = panelRoot;
        titleLabel = title;
        questionLabel = question;
        optionsRoot = options;
        feedbackLabel = feedback;
        closeButton = close;
        closeButton.onClick.AddListener(Close);
        root.SetActive(false);
    }

    public void Show(LessonItemData lessonItem, Action completed = null)
    {
        if (root == null || lessonItem == null || lessonItem.type != "exercise")
        {
            return;
        }

        if (lessonItem.options == null || lessonItem.options.Count < 2 ||
            lessonItem.correctIndex < 0 || lessonItem.correctIndex >= lessonItem.options.Count)
        {
            ToastView.Instance?.Show("Latihan ini belum memiliki pilihan jawaban yang valid.");
            return;
        }

        item = lessonItem;
        onCompleted = completed;
        correctIndex = lessonItem.correctIndex;
        answered = false;

        titleLabel.text = string.IsNullOrWhiteSpace(lessonItem.kind)
            ? "Latihan"
            : "Latihan — " + lessonItem.kind;
        questionLabel.text = string.IsNullOrWhiteSpace(lessonItem.question)
            ? "Pilih jawaban yang benar."
            : lessonItem.question;
        feedbackLabel.text = string.Empty;
        feedbackLabel.color = UiFactory.Ink;

        BuildOptions();
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        UiModalState.Push(this);
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

    private void BuildOptions()
    {
        ClearOptions();
        for (int i = 0; i < item.options.Count; i++)
        {
            int choice = i;
            Button button = UiFactory.CreateButton(optionsRoot, "Option_" + i, item.options[i], null,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -i * 100f), new Vector2(1180f, 88f),
                OptionColor, UiFactory.Ink, 32f);
            button.onClick.AddListener(() => Answer(choice));
        }
    }

    private void Answer(int choice)
    {
        if (answered)
        {
            return;
        }

        answered = true;
        bool correct = choice == correctIndex;
        for (int i = 0; i < optionsRoot.childCount; i++)
        {
            Button button = optionsRoot.GetChild(i).GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }

        if (choice >= 0 && choice < optionsRoot.childCount)
        {
            optionsRoot.GetChild(choice).GetComponent<Image>().color = correct ? CorrectColor : WrongColor;
        }

        if (!correct && correctIndex >= 0 && correctIndex < optionsRoot.childCount)
        {
            optionsRoot.GetChild(correctIndex).GetComponent<Image>().color = CorrectColor;
        }

        if (correct)
        {
            AudioManager.Instance?.PlaySfx(SfxId.Correct);
            feedbackLabel.color = new Color(0.16f, 0.45f, 0.22f, 1f);
            feedbackLabel.text = "Benar.";
            CurriculumService.Instance?.MarkItemCompleted(item);
            onCompleted?.Invoke();
        }
        else
        {
            AudioManager.Instance?.PlaySfx(SfxId.Wrong);
            feedbackLabel.color = WrongColor;
            string answer = correctIndex >= 0 && correctIndex < item.options.Count
                ? item.options[correctIndex]
                : "-";
            feedbackLabel.text = "Belum tepat. Jawaban yang benar: " + answer;
        }
    }

    private void ClearOptions()
    {
        if (optionsRoot == null)
        {
            return;
        }

        for (int i = optionsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = optionsRoot.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }
}
