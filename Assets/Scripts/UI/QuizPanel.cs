using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kuis scene. Salah = -1 hati. Hati 0 memblokir lanjut sampai dipulihkan.
/// Nilai 0–100 (persentase); lulus minimal 70.
/// </summary>
public class QuizPanel : MonoBehaviour
{
    private const int MaxQuestions = 5;

    private class Entry
    {
        public LearningObjectDefinition definition;
        public QuizQuestion question;
    }

    private GameObject root;
    private TextMeshProUGUI progressLabel;
    private TextMeshProUGUI heartsLabel;
    private TextMeshProUGUI questionLabel;
    private RectTransform optionsRoot;
    private TextMeshProUGUI feedbackLabel;
    private Button nextButton;
    private TextMeshProUGUI nextLabel;
    private Button closeButton;

    private readonly List<Entry> questions = new List<Entry>();
    private readonly List<Entry> wrongEntries = new List<Entry>();
    private SceneController scene;
    private Action onPass;
    private int index;
    private int correctCount;
    private bool answered;
    private bool finished;

    public void Build(GameObject panelRoot, TextMeshProUGUI progress, TextMeshProUGUI hearts,
        TextMeshProUGUI question, RectTransform options, TextMeshProUGUI feedback,
        Button next, TextMeshProUGUI nextText, Button close)
    {
        root = panelRoot;
        progressLabel = progress;
        heartsLabel = hearts;
        questionLabel = question;
        optionsRoot = options;
        feedbackLabel = feedback;
        nextButton = next;
        nextLabel = nextText;
        closeButton = close;

        nextButton.onClick.AddListener(Next);
        closeButton.onClick.AddListener(Close);
        root.SetActive(false);
    }

    public void StartQuiz(SceneController sceneController, Action passed = null)
    {
        scene = sceneController;
        onPass = passed;
        finished = false;

        if (scene == null || scene.database == null)
        {
            ToastView.Instance?.Show("Kuis belum tersedia.");
            return;
        }

        BuildQuestions();
        if (questions.Count == 0)
        {
            ToastView.Instance?.Show("Belum ada soal untuk scene ini.");
            return;
        }

        index = 0;
        correctCount = 0;
        wrongEntries.Clear();

        root.SetActive(true);
        UiModalState.Push(this);
        RefreshHearts();
        ShowQuestion();
    }

    private void BuildQuestions()
    {
        questions.Clear();
        List<LearningObjectDefinition> definitions = scene.database.ForScene(scene.sceneKey);
        GameState state = GameState.Ensure();

        for (int d = 0; d < definitions.Count; d++)
        {
            LearningObjectDefinition definition = definitions[d];
            if (definition == null || definition.questions == null)
            {
                continue;
            }

            // Hanya gunakan soal dari objek yang sudah dipelajari (panel info dibuka).
            if (!state.IsLearned(definition.id))
            {
                continue;
            }

            for (int q = 0; q < definition.questions.Count; q++)
            {
                QuizQuestion question = definition.questions[q];
                if (question == null || question.options == null || question.options.Length < 2)
                {
                    continue;
                }

                if (question.correctIndex < 0 || question.correctIndex >= question.options.Length)
                {
                    continue;
                }

                if (question.usesDawan && !definition.IsValidated)
                {
                    continue;
                }

                questions.Add(new Entry { definition = definition, question = question });
            }
        }

        // Utamakan soal kosakata Dawan (satu soal per objek), lalu sisa soal
        // fungsi/pengetahuan hanya dipakai bila soal Dawan tidak cukup.
        System.Random random = new System.Random(Environment.TickCount);
        List<Entry> dawanEntries = new List<Entry>();
        List<Entry> otherEntries = new List<Entry>();
        for (int i = 0; i < questions.Count; i++)
        {
            if (questions[i].question.usesDawan)
            {
                dawanEntries.Add(questions[i]);
            }
            else
            {
                otherEntries.Add(questions[i]);
            }
        }

        Shuffle(dawanEntries, random);
        Shuffle(otherEntries, random);

        List<Entry> selected = new List<Entry>();
        HashSet<string> usedObjects = new HashSet<string>();

        for (int i = 0; i < dawanEntries.Count && selected.Count < MaxQuestions; i++)
        {
            if (usedObjects.Add(dawanEntries[i].definition.id))
            {
                selected.Add(dawanEntries[i]);
            }
        }

        for (int i = 0; i < dawanEntries.Count && selected.Count < MaxQuestions; i++)
        {
            if (!selected.Contains(dawanEntries[i]))
            {
                selected.Add(dawanEntries[i]);
            }
        }

        for (int i = 0; i < otherEntries.Count && selected.Count < MaxQuestions; i++)
        {
            selected.Add(otherEntries[i]);
        }

        questions.Clear();
        questions.AddRange(selected);
    }

    private static void Shuffle<T>(List<T> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private void ShowQuestion()
    {
        answered = false;
        finished = false;
        Entry entry = questions[index];

        SetFeedbackLayout(false);
        progressLabel.text = $"Soal {index + 1}/{questions.Count}";
        questionLabel.text = entry.question.question;
        feedbackLabel.text = string.Empty;
        nextButton.gameObject.SetActive(false);

        ClearOptions();

        for (int i = 0; i < entry.question.options.Length; i++)
        {
            int choice = i;
            Button button = UiFactory.CreateButton(optionsRoot, $"Option_{i}", entry.question.options[i],
                null, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -i * 100f), new Vector2(1230f, 92f),
                new Color(0.96f, 0.92f, 0.82f, 1f), UiFactory.Ink, 34f);
            button.onClick.AddListener(() => Answer(choice));
        }

        RefreshHearts();
    }

    private void Answer(int choice)
    {
        if (answered || finished)
        {
            return;
        }

        answered = true;
        Entry entry = questions[index];
        bool correct = choice == entry.question.correctIndex;

        Transform options = optionsRoot;
        for (int i = 0; i < options.childCount; i++)
        {
            Button button = options.GetChild(i).GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }

        if (correct)
        {
            correctCount++;
            AudioManager.Instance?.PlaySfx(SfxId.Correct);
            if (choice < optionsRoot.childCount)
            {
                optionsRoot.GetChild(choice).GetComponent<Image>().color =
                    new Color(0.55f, 0.85f, 0.5f, 1f);
            }

            // XP hanya sekali per soal (identitas soal stabil).
            GamificationService.OnQuizAnswer(entry.definition, entry.question);

            feedbackLabel.text = string.IsNullOrEmpty(entry.question.explanation)
                ? "Benar."
                : $"Benar. {entry.question.explanation}";
        }
        else
        {
            AudioManager.Instance?.PlaySfx(SfxId.Wrong);
            wrongEntries.Add(entry);
            if (choice < optionsRoot.childCount)
            {
                optionsRoot.GetChild(choice).GetComponent<Image>().color =
                    new Color(0.9f, 0.45f, 0.4f, 1f);
            }

            int correctIndex = entry.question.correctIndex;
            if (correctIndex < optionsRoot.childCount)
            {
                optionsRoot.GetChild(correctIndex).GetComponent<Image>().color =
                    new Color(0.55f, 0.85f, 0.5f, 1f);
            }

            HealthService.Instance?.LoseHeart("Jawaban kuis salah");
            feedbackLabel.text = string.IsNullOrEmpty(entry.question.explanation)
                ? "Salah. Hati berkurang 1."
                : $"Salah. Hati berkurang 1. {entry.question.explanation}";
        }

        nextButton.gameObject.SetActive(true);
        nextLabel.text = "Lanjut";
        nextButton.interactable = HealthService.Instance == null || !HealthService.Instance.IsEmpty;

        if (HealthService.Instance != null && HealthService.Instance.IsEmpty)
        {
            GameUi.Instance?.ShowRecovery();
        }

        RefreshHearts();
    }

    private void Next()
    {
        if (!answered || finished)
        {
            return;
        }

        if (HealthService.Instance != null && HealthService.Instance.IsEmpty)
        {
            return;
        }

        index++;
        if (index >= questions.Count)
        {
            Finish();
            return;
        }

        ShowQuestion();
    }

    private void Finish()
    {
        finished = true;
        int total = questions.Count;
        int score = total > 0 ? Mathf.RoundToInt(correctCount * 100f / total) : 0;
        bool passed = score >= 70;

        progressLabel.text = "Kuis selesai";
        ClearOptions();
        nextButton.onClick.RemoveAllListeners();
        nextButton.gameObject.SetActive(true);
        SetFeedbackLayout(true);

        Debug.Log($"[Kuis] scene={scene.sceneKey} benar={correctCount}/{total} skor={score} lulus={passed}");

        string review = BuildReview();

        if (passed)
        {
            AudioManager.Instance?.PlaySfx(SfxId.Correct);
            scene.CompleteCurrentScene();
            GamificationService.OnQuizPassed(scene.sceneKey, score, correctCount, total);

            string extra = scene.sceneKey == "Dapur"
                ? "\nRumah warga sekarang terbuka."
                : scene.sceneKey == "Rumah"
                    ? "\nMateri Rumah warga sudah selesai."
                    : "\nMateri Kebun sudah selesai.";

            questionLabel.text = "Lulus!";
            feedbackLabel.text =
                $"Nilai: {score}/100 · benar {correctCount}/{total} (syarat lulus 70).{extra}{review}";
            nextLabel.text = "Lanjut";
            nextButton.interactable = true;
            nextButton.onClick.AddListener(CloseAndPass);
            return;
        }

        questionLabel.text = "Belum lulus";
        feedbackLabel.text =
            $"Nilai: {score}/100 · benar {correctCount}/{total} (syarat lulus 70). Materi tetap tersimpan.{review}";
        nextLabel.text = "Ulangi";
        nextButton.interactable = HealthService.Instance == null || !HealthService.Instance.IsEmpty;
        nextButton.onClick.AddListener(Retry);
    }

    /// <summary>Daftar materi yang perlu dipelajari ulang (maks 3 + elipsis).</summary>
    private string BuildReview()
    {
        if (wrongEntries.Count == 0)
        {
            return string.Empty;
        }

        int limit = Mathf.Min(wrongEntries.Count, 3);
        string text = "\nPelajari ulang:\n";
        for (int i = 0; i < limit; i++)
        {
            Entry entry = wrongEntries[i];
            string answer = "-";
            if (entry.question.options != null &&
                entry.question.correctIndex >= 0 &&
                entry.question.correctIndex < entry.question.options.Length)
            {
                answer = entry.question.options[entry.question.correctIndex];
            }

            text += $"• {entry.definition.namaIndonesia} — {answer}\n";
        }

        if (wrongEntries.Count > limit)
        {
            text += "…\n";
        }

        return text;
    }

    /// <summary>Menutup panel hasil kuis setelah pemain membaca ringkasannya.</summary>
    private void CloseAndPass()
    {
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(Next);
        nextLabel.text = "Lanjut";

        Action callback = onPass;
        onPass = null;
        Close();
        callback?.Invoke();
    }

    /// <summary>Panel hasil memakai kotak umpan balik yang lebih tinggi agar tidak kosong.</summary>
    private void SetFeedbackLayout(bool result)
    {
        if (feedbackLabel == null)
        {
            return;
        }

        RectTransform rect = feedbackLabel.rectTransform;
        if (result)
        {
            rect.sizeDelta = new Vector2(-96f, 380f);
            rect.anchoredPosition = new Vector2(0f, 250f);
            feedbackLabel.fontSize = 28f;
        }
        else
        {
            rect.sizeDelta = new Vector2(-96f, 110f);
            rect.anchoredPosition = new Vector2(0f, 206f);
            feedbackLabel.fontSize = 32f;
        }
    }

    private void Retry()
    {
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(Next);
        nextLabel.text = "Lanjut";
        index = 0;
        correctCount = 0;
        wrongEntries.Clear();
        finished = false;
        BuildQuestions();
        ShowQuestion();
    }

    private void RefreshHearts()
    {
        if (heartsLabel != null && HealthService.Instance != null)
        {
            heartsLabel.text = $"Hati: {HealthService.Instance.Hearts}/{HealthService.Instance.Max}";
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

    public void Close()
    {
        if (root == null || !root.activeSelf)
        {
            return;
        }

        root.SetActive(false);
        UiModalState.Pop(this);
        ClearOptions();
        onPass = null;
    }

    private void Update()
    {
        if (root == null || !root.activeSelf || !ReferenceEquals(UiModalState.Top, this))
        {
            return;
        }

        RefreshHearts();

        // Setelah hati dipulihkan dari panel darurat, izinkan lanjut lagi.
        if (answered && !finished && nextButton != null && nextButton.gameObject.activeSelf)
        {
            nextButton.interactable = HealthService.Instance == null || !HealthService.Instance.IsEmpty;
        }
    }

    private void OnDestroy()
    {
        UiModalState.Pop(this);
    }
}
