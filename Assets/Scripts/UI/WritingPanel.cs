using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel latihan menulis (susun kata / lengkapi kalimat) untuk satu objek.
/// XP +5 sekali per identitas latihan (lihat WritingExercise.Key).
/// </summary>
public class WritingPanel : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.26f, 0.19f, 0.11f, 1f);
    private static readonly Color Muted = new Color(0.42f, 0.33f, 0.22f, 0.75f);
    private static readonly Color OptionColor = new Color(0.96f, 0.92f, 0.82f, 1f);
    private static readonly Color CorrectColor = new Color(0.55f, 0.85f, 0.5f, 1f);
    private static readonly Color WrongColor = new Color(0.9f, 0.45f, 0.4f, 1f);

    private GameObject root;
    private WritingExercise exercise;

    private TextMeshProUGUI instructionLabel;
    private TextMeshProUGUI questionLabel;
    private TextMeshProUGUI answerLabel;
    private TextMeshProUGUI answerText;
    private TextMeshProUGUI feedbackLabel;
    private TextMeshProUGUI scoreLabel;
    private RectTransform optionsRoot;
    private Button checkButton;
    private Button resetButton;

    private readonly List<string> chosen = new List<string>();
    private readonly List<Button> tileButtons = new List<Button>();
    private readonly List<Button> optionButtons = new List<Button>();
    private bool solved;

    public void Build(GameObject panelRoot, RectTransform content, Sprite panelSprite)
    {
        root = panelRoot;

        UiFactory.CreateLabel(content, "Title", "Latihan Menulis", 42f, Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -28f), new Vector2(800f, 52f), true);

        scoreLabel = UiFactory.CreateLabel(content, "Score", "Skor: 0/1", 26f, Ink,
            TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-48f, -30f), new Vector2(300f, 40f), true);

        instructionLabel = UiFactory.CreateLabel(content, "Instruction", string.Empty, 26f, Muted,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -92f), new Vector2(1300f, 36f));

        questionLabel = UiFactory.CreateLabel(content, "Question", string.Empty, 31f, Ink,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -126f), new Vector2(1300f, 56f), true);

        answerLabel = UiFactory.CreateLabel(content, "AnswerLabel", "Jawaban:", 24f, Muted,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -186f), new Vector2(300f, 28f));

        answerText = UiFactory.CreateLabel(content, "AnswerText", string.Empty, 34f, Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -216f), new Vector2(1300f, 44f), true);

        optionsRoot = UiFactory.CreateRect(content, "Options", new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -266f), new Vector2(1300f, 320f));

        feedbackLabel = UiFactory.CreateLabel(content, "Feedback", string.Empty, 24f, Ink,
            TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -576f), new Vector2(1300f, 70f));

        checkButton = UiFactory.CreateButton(content, "CheckButton", "Periksa", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(48f, 42f), new Vector2(300f, 84f),
            new Color(0.22f, 0.45f, 0.24f, 0.95f), new Color(1f, 0.98f, 0.9f, 1f), 30f);
        checkButton.onClick.AddListener(Check);

        resetButton = UiFactory.CreateButton(content, "ResetButton", "Susun Ulang", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(364f, 42f), new Vector2(300f, 84f),
            new Color(0.35f, 0.25f, 0.14f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 28f);
        resetButton.onClick.AddListener(ResetAnswer);

        Button close = UiFactory.CreateButton(content, "CloseButton", "Tutup", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-48f, 42f), new Vector2(240f, 84f),
            new Color(0.3f, 0.2f, 0.12f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);
        close.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            Close();
        });

        root.SetActive(false);
    }

    public void Show(LearningObjectDefinition definition, LearningDatabase learningDatabase)
    {
        if (root == null)
        {
            return;
        }

        exercise = WritingExerciseGenerator.BuildFor(definition, learningDatabase);
        if (exercise == null)
        {
            ToastView.Instance?.Show("Latihan menulis belum tersedia untuk objek ini.");
            return;
        }

        ClearOptions();
        chosen.Clear();
        tileButtons.Clear();
        optionButtons.Clear();
        solved = false;

        instructionLabel.text = exercise.Instruction;
        questionLabel.text = exercise.Question;
        answerLabel.text = exercise.Type == WritingExerciseType.SusunKata ? "Jawaban:" : "Pilihan:";
        answerText.text = string.Empty;
        feedbackLabel.text = string.Empty;
        scoreLabel.text = "Skor: 0/1";

        bool susun = exercise.Type == WritingExerciseType.SusunKata;
        checkButton.gameObject.SetActive(susun);
        resetButton.gameObject.SetActive(susun);
        checkButton.interactable = false;

        if (susun)
        {
            BuildTiles();
        }
        else
        {
            BuildOptions();
        }

        root.SetActive(true);
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

    // ---------------------------------------------------------------- render

    private void BuildTiles()
    {
        for (int i = 0; i < exercise.Tiles.Count; i++)
        {
            int index = i;
            string token = exercise.Tiles[i];
            float x = (i % 4) * 310f;
            float y = -(i / 4) * 80f;
            Button button = UiFactory.CreateButton(optionsRoot, "Tile_" + i, token, null,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(x, y), new Vector2(300f, 68f), OptionColor, Ink, 30f);
            button.onClick.AddListener(() => OnTile(index, token));
            tileButtons.Add(button);
        }
    }

    private void BuildOptions()
    {
        for (int i = 0; i < exercise.Options.Count; i++)
        {
            string option = exercise.Options[i];
            Button button = UiFactory.CreateButton(optionsRoot, "Option_" + i, option, null,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -i * 76f), new Vector2(1100f, 68f), OptionColor, Ink, 30f);
            button.onClick.AddListener(() => ChooseOption(option));
            optionButtons.Add(button);
        }
    }

    // ---------------------------------------------------------------- aksi

    private void OnTile(int index, string token)
    {
        if (solved || index < 0 || index >= tileButtons.Count || !tileButtons[index].interactable)
        {
            return;
        }

        chosen.Add(token);
        tileButtons[index].interactable = false;
        answerText.text = string.Join(" ", chosen.ToArray());
        checkButton.interactable = chosen.Count == exercise.Tiles.Count;
    }

    private void Check()
    {
        if (solved || exercise == null)
        {
            return;
        }

        string given = string.Join(" ", chosen.ToArray());
        if (WritingExerciseGenerator.Normalize(given) ==
            WritingExerciseGenerator.Normalize(exercise.Correct))
        {
            Solve(true, "Benar!");
            return;
        }

        feedbackLabel.text = "Belum tepat. Ketuk 'Susun Ulang' lalu coba lagi.";
        feedbackLabel.color = WrongColor;
    }

    private void ChooseOption(string option)
    {
        if (solved || exercise == null)
        {
            return;
        }

        bool correct = WritingExerciseGenerator.Normalize(option) ==
                       WritingExerciseGenerator.Normalize(exercise.Correct);

        for (int i = 0; i < optionButtons.Count; i++)
        {
            Button button = optionButtons[i];
            button.interactable = false;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            string text = label != null ? label.text : string.Empty;
            bool isCorrectOption = WritingExerciseGenerator.Normalize(text) ==
                                   WritingExerciseGenerator.Normalize(exercise.Correct);
            if (isCorrectOption)
            {
                button.GetComponent<Image>().color = CorrectColor;
            }
            else if (!correct && WritingExerciseGenerator.Normalize(text) ==
                     WritingExerciseGenerator.Normalize(option))
            {
                button.GetComponent<Image>().color = WrongColor;
            }
        }

        if (correct)
        {
            Solve(true, "Benar!");
        }
        else
        {
            answerText.text = option;
            feedbackLabel.text = $"Belum tepat. Jawaban yang benar: {exercise.Correct}";
            feedbackLabel.color = WrongColor;
            scoreLabel.text = "Skor: 0/1";
        }
    }

    private void Solve(bool correct, string prefix)
    {
        solved = true;
        bool awarded = GamificationService.OnWritingSolved(exercise.Key);

        scoreLabel.text = correct ? "Skor: 1/1" : "Skor: 0/1";
        feedbackLabel.color = correct ? new Color(0.16f, 0.45f, 0.22f, 1f) : Ink;

        string xp = awarded ? " +5 XP" : " (XP sudah pernah diberikan)";
        string explanation = string.IsNullOrWhiteSpace(exercise.Explanation)
            ? string.Empty
            : "\n" + exercise.Explanation;
        feedbackLabel.text = $"{prefix}{xp}{explanation}";

        if (checkButton != null)
        {
            checkButton.interactable = false;
        }
    }

    private void ResetAnswer()
    {
        if (solved)
        {
            return;
        }

        chosen.Clear();
        answerText.text = string.Empty;
        feedbackLabel.text = string.Empty;
        for (int i = 0; i < tileButtons.Count; i++)
        {
            tileButtons[i].interactable = true;
        }

        checkButton.interactable = false;
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
