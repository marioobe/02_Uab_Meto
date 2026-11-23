using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Perakitan UI runtime satu scene: HUD, kontrol, prompt, dan semua panel modal.
/// Scene builder cukup menambahkan komponen ini dan mengisi sprite.
/// </summary>
public class GameUi : MonoBehaviour
{
    public static GameUi Instance { get; private set; }

    [Header("Sprite UI")]
    public Sprite panelSprite;
    public Sprite circleSprite;
    public Sprite heartFull;
    public Sprite heartEmpty;
    public Sprite fruitIcon;
    public Sprite dialogPanel;
    public Sprite settingsIcon;
    public Sprite dpadUp;
    public Sprite dpadDown;
    public Sprite dpadLeft;
    public Sprite dpadRight;

    public SceneController Scene { get; private set; }
    public HudView Hud { get; private set; }
    public DialoguePanel Dialogue { get; private set; }
    public InfoPanel Info { get; private set; }
    public QuizPanel Quiz { get; private set; }
    public CurriculumExercisePanel CurriculumExercise { get; private set; }
    public CurriculumDialoguePanel CurriculumDialogue { get; private set; }
    public SettingsPanel Settings { get; private set; }
    public QuestPanel Quest { get; private set; }
    public TopicPanel Topics { get; private set; }
    public ProfilePanel Profile { get; private set; }
    public WritingPanel Writing { get; private set; }
    public SpeechPracticePanel Speaking { get; private set; }
    public RecoveryPanel Recovery { get; private set; }

    private Canvas hudCanvas;
    private Canvas modalCanvas;
    private RectTransform safeArea;
    private GameObject promptRoot;
    private TextMeshProUGUI promptLabel;
    private TextMeshProUGUI actionLabel;
    private GameObject dpadRoot;
    private GameObject joystickRoot;
    private DPadView dpad;
    private MobileJoystick joystick;
    private InputRouter router;
    private PlayerInteractor interactor;
    private bool built;

    private static readonly Color PanelTint = new Color(0.96f, 0.91f, 0.79f, 0.98f);

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (SettingsService.Instance != null)
        {
            SettingsService.Instance.SchemeChanged -= ApplyScheme;
        }
    }

    public void Initialize(SceneController controller)
    {
        Scene = controller;

        if (!built)
        {
            Build();
            built = true;
        }

        interactor = controller != null ? controller.interactor : null;
        if (interactor == null)
        {
            interactor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (interactor != null)
        {
            interactor.BindTmp(promptRoot, promptLabel, actionLabel);
        }

        if (SettingsService.Instance != null)
        {
            SettingsService.Instance.SchemeChanged -= ApplyScheme;
            SettingsService.Instance.SchemeChanged += ApplyScheme;
        }

        ApplyScheme();
        Hud?.Refresh();
    }

    private void Build()
    {
        hudCanvas = UiFactory.CreateCanvas("HudCanvas", 100, transform);
        safeArea = UiFactory.CreateSafeArea(hudCanvas.transform);

        Hud = gameObject.AddComponent<HudView>();
        Hud.Build(safeArea, panelSprite, heartFull, heartEmpty, fruitIcon, settingsIcon,
            ShowSettings, ShowTopics, ShowProfile, UseFruit, HeartsPressed);

        BuildControls();
        BuildPrompt();

        modalCanvas = UiFactory.CreateCanvas("ModalCanvas", 200, transform);
        BuildDialogue();
        BuildInfo();
        BuildQuiz();
        BuildCurriculumExercise();
        BuildCurriculumDialogue();
        BuildSettings();
        BuildQuest();
        BuildTopics();
        BuildProfile();
        BuildWriting();
        BuildSpeaking();
        BuildRecovery();
    }

    // ---------------------------------------------------------------- kontrol

    private void BuildControls()
    {
        RectTransform padRect = UiFactory.CreateRect(safeArea, "DPad", Vector2.zero, Vector2.zero,
            Vector2.zero, new Vector2(40f, 40f), new Vector2(400f, 400f));
        dpadRoot = padRect.gameObject;

        DPadView view = padRect.gameObject.AddComponent<DPadView>();
        HoldButton up = CreateHoldButton(padRect, "Up", new Vector2(200f, 318f), "▲", dpadUp);
        HoldButton down = CreateHoldButton(padRect, "Down", new Vector2(200f, 82f), "▼", dpadDown);
        HoldButton left = CreateHoldButton(padRect, "Left", new Vector2(82f, 200f), "<", dpadLeft);
        HoldButton right = CreateHoldButton(padRect, "Right", new Vector2(318f, 200f), ">", dpadRight);
        view.Bind(up, down, left, right);
        dpad = view;

        RectTransform stickRect = UiFactory.CreateRect(safeArea, "Joystick", Vector2.zero, Vector2.zero,
            Vector2.zero, new Vector2(40f, 40f), new Vector2(400f, 400f));
        joystickRoot = stickRect.gameObject;

        Image background = UiFactory.CreateImage(stickRect, "Background", circleSprite,
            new Color(1f, 0.95f, 0.82f, 0.50f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 360f));
        background.raycastTarget = true;
        // Cincin gelap di dalam lingkaran terang supaya batas joystick terlihat di atas seni terang.
        Image ring = UiFactory.CreateImage(background.transform, "Ring", circleSprite,
            new Color(0.13f, 0.10f, 0.06f, 0.40f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f), false);
        Image handle = UiFactory.CreateImage(background.transform, "Handle", circleSprite,
            new Color(1f, 0.96f, 0.86f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 140f), false);
        joystick = background.gameObject.AddComponent<MobileJoystick>();
        joystick.Bind(background.rectTransform, handle.rectTransform, 140f);

        GameObject routerObject = new GameObject("InputRouter");
        routerObject.transform.SetParent(transform, false);
        router = routerObject.AddComponent<InputRouter>();
        router.Bind(dpad, joystick);

        PlayerMovement2D movement = FindFirstObjectByType<PlayerMovement2D>();
        movement?.BindRouter(router);
    }

    private HoldButton CreateHoldButton(Transform parent, string name, Vector2 position,
        string glyph, Sprite icon)
    {
        // Tombol terang dengan glyph gelap: tetap terbaca di atas rumput/pasir yang terang.
        Image image = UiFactory.CreateImage(parent, name, circleSprite,
            new Color(1f, 0.95f, 0.82f, 0.66f), Vector2.zero, Vector2.zero,
            new Vector2(0.5f, 0.5f), position, new Vector2(124f, 124f));

        if (icon != null)
        {
            Image iconImage = UiFactory.CreateImage(image.transform, "Icon", icon, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(64f, 64f), false);
            iconImage.preserveAspect = true;
        }
        else
        {
            UiFactory.CreateLabel(image.transform, "Glyph", glyph, 50f,
                new Color(0.24f, 0.16f, 0.09f, 0.95f), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
        }

        return image.gameObject.AddComponent<HoldButton>();
    }

    private void BuildPrompt()
    {
        RectTransform root = UiFactory.CreateRect(safeArea, "PromptRoot", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        promptRoot = root.gameObject;

        Image pill = UiFactory.CreateImage(root, "PromptPill", panelSprite,
            new Color(0.12f, 0.09f, 0.06f, 0.9f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(480f, 78f));
        UiFactory.StylePanel(pill);

        promptLabel = UiFactory.CreateLabel(pill.transform, "PromptText", "Baca", 32f,
            new Color(1f, 0.95f, 0.82f, 1f), TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);

        Button action = UiFactory.CreateButton(root, "InteractButton", "Baca", circleSprite,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-42f, 42f), new Vector2(176f, 176f),
            new Color(1f, 0.93f, 0.7f, 0.94f), new Color(0.22f, 0.15f, 0.08f, 1f), 36f);
        actionLabel = action.GetComponentInChildren<TextMeshProUGUI>();
        action.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            interactor?.InteractWithCurrent();
        });

        promptRoot.SetActive(false);
    }

    private void ApplyScheme()
    {
        bool useDpad = SettingsService.Instance == null ||
                       SettingsService.Instance.Scheme == ControlScheme.DPad;

        if (dpadRoot != null)
        {
            dpadRoot.SetActive(useDpad);
        }

        if (joystickRoot != null)
        {
            joystickRoot.SetActive(!useDpad);
        }

        router?.SetUseDPad(useDpad);
    }

    // ---------------------------------------------------------------- panel

    private GameObject CreateModal(string name, Vector2 size, out RectTransform content)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(modalCanvas.transform, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UiFactory.CreateImage(root.transform, "Dim", null, new Color(0f, 0f, 0f, 0.64f),
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        Image panel = UiFactory.CreateImage(root.transform, "Panel", panelSprite, PanelTint,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, size);
        UiFactory.StylePanel(panel);

        content = (RectTransform)panel.transform;
        root.SetActive(false);
        return root;
    }

    private TextMeshProUGUI CreateHeading(Transform parent, string text, float top = -34f)
    {
        return UiFactory.CreateLabel(parent, "Title", text, 50f, UiFactory.Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, top), new Vector2(1000f, 62f), true);
    }

    private void BuildDialogue()
    {
        GameObject root = CreateModal("DialoguePanel", new Vector2(1560f, 878f), out RectTransform content);

        // Ilustrasi ketua adat + bingkai sebagai latar dialog (bila tersedia).
        if (dialogPanel != null)
        {
            Image panelImage = content.GetComponent<Image>();
            panelImage.sprite = dialogPanel;
            panelImage.color = Color.white;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = true;
        }

        Button advance = content.gameObject.AddComponent<Button>();
        advance.targetGraphic = content.GetComponent<Image>();
        advance.transition = Selectable.Transition.None;

        TextMeshProUGUI title;
        TextMeshProUGUI body;
        TextMeshProUGUI hint;

        if (dialogPanel != null)
        {
            // Teks diletakkan di area perkamen (kanan ilustrasi): nama pemandu di
            // plakat kayu atas, isi dialog di bawahnya, petunjuk di kiri tombol panah.
            title = UiFactory.CreateLabel(content, "Title", string.Empty, 32f,
                new Color(0.98f, 0.93f, 0.82f, 1f), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(145f, 25f), new Vector2(330f, 66f), true);

            body = UiFactory.CreateLabel(content, "Body", string.Empty, 30f,
                UiFactory.Ink, TextAlignmentOptions.TopLeft,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(300f, -140f), new Vector2(560f, 200f));

            hint = UiFactory.CreateLabel(content, "Hint", string.Empty, 20f,
                new Color(0.35f, 0.25f, 0.15f, 0.9f), TextAlignmentOptions.MidlineRight,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(415f, -275f), new Vector2(300f, 26f));
        }
        else
        {
            title = CreateHeading(content, string.Empty, -36f);

            body = UiFactory.CreateLabel(content, "Body", string.Empty, 36f,
                UiFactory.Ink, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            body.rectTransform.offsetMin = new Vector2(52f, 96f);
            body.rectTransform.offsetMax = new Vector2(-52f, -104f);

            hint = UiFactory.CreateLabel(content, "Hint", string.Empty, 26f,
                new Color(0.35f, 0.25f, 0.15f, 0.85f), TextAlignmentOptions.MidlineRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-48f, 28f), new Vector2(640f, 40f));
        }

        Dialogue = gameObject.AddComponent<DialoguePanel>();
        Dialogue.Build(root, title, body, hint);
        advance.onClick.AddListener(Dialogue.Advance);
    }

    private void BuildInfo()
    {
        GameObject root = CreateModal("InfoPanel", new Vector2(1400f, 800f), out RectTransform content);

        Image frame = UiFactory.CreateImage(content, "ImageFrame", panelSprite,
            new Color(0.2f, 0.14f, 0.08f, 0.22f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -120f), new Vector2(380f, 380f));
        UiFactory.StylePanel(frame);

        Image objectImage = UiFactory.CreateImage(frame.transform, "ObjectImage", null, Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        objectImage.rectTransform.offsetMin = new Vector2(12f, 12f);
        objectImage.rectTransform.offsetMax = new Vector2(-12f, -12f);
        objectImage.preserveAspect = true;

        TextMeshProUGUI title = UiFactory.CreateLabel(content, "ObjTitle", string.Empty, 50f,
            UiFactory.Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(464f, -52f), new Vector2(880f, 64f), true);
        TextMeshProUGUI category = UiFactory.CreateLabel(content, "Category", string.Empty, 30f,
            new Color(0.45f, 0.3f, 0.15f, 1f), TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(464f, -118f), new Vector2(880f, 36f));
        TextMeshProUGUI indonesia = UiFactory.CreateLabel(content, "Indonesia", string.Empty, 32f,
            UiFactory.Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(464f, -168f), new Vector2(880f, 40f));
        TextMeshProUGUI dawan = UiFactory.CreateLabel(content, "Dawan", string.Empty, 32f,
            UiFactory.Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(464f, -218f), new Vector2(880f, 40f));

        Image badgeBackground = UiFactory.CreateImage(content, "Badge", null,
            new Color(0.72f, 0.42f, 0.12f, 0.95f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(464f, -280f), new Vector2(480f, 48f));
        TextMeshProUGUI badge = UiFactory.CreateLabel(badgeBackground.transform, "BadgeText",
            "DRAF", 26f, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);

        TextMeshProUGUI explanation = UiFactory.CreateLabel(content, "Explanation", string.Empty, 33f,
            UiFactory.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(464f, -352f), new Vector2(880f, 200f));

        Button audio = UiFactory.CreateButton(content, "AudioButton", "Dengarkan", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(48f, 138f), new Vector2(320f, 84f),
            new Color(0.35f, 0.25f, 0.14f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 32f);
        Button quiz = UiFactory.CreateButton(content, "QuizButton", "Kuis", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(392f, 138f), new Vector2(300f, 84f),
            new Color(0.22f, 0.45f, 0.24f, 0.95f), new Color(1f, 0.98f, 0.9f, 1f), 32f);
        Button close = UiFactory.CreateButton(content, "CloseButton", "Tutup", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-48f, 138f), new Vector2(240f, 84f),
            new Color(0.3f, 0.2f, 0.12f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);
        close.onClick.AddListener(() =>
        {
            Info?.MarkLearnedAndClose();
        });

        // Baris kedua (Fase C): Latihan menulis + Bicara.
        Button writing = UiFactory.CreateButton(content, "WritingButton", "Latihan", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(48f, 42f), new Vector2(320f, 84f),
            new Color(0.38f, 0.3f, 0.16f, 0.92f), new Color(1f, 0.96f, 0.86f, 1f), 30f);
        writing.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            Info?.OpenWriting();
        });

        Button speaking = UiFactory.CreateButton(content, "SpeakingButton", "Bicara", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(392f, 42f), new Vector2(300f, 84f),
            new Color(0.3f, 0.34f, 0.44f, 0.92f), new Color(1f, 0.96f, 0.86f, 1f), 30f);
        speaking.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            Info?.OpenSpeaking();
        });

        Info = gameObject.AddComponent<InfoPanel>();
        Info.Build(root, objectImage, title, category, indonesia, dawan, badge, badgeBackground,
            explanation, audio, quiz);
    }

    private void BuildQuiz()
    {
        GameObject root = CreateModal("QuizPanel", new Vector2(1560f, 860f), out RectTransform content);

        TextMeshProUGUI progress = UiFactory.CreateLabel(content, "Progress", string.Empty, 36f,
            UiFactory.Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -34f), new Vector2(600f, 48f), true);
        TextMeshProUGUI hearts = UiFactory.CreateLabel(content, "Hearts", string.Empty, 36f,
            new Color(0.6f, 0.16f, 0.14f, 1f), TextAlignmentOptions.MidlineRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-48f, -34f), new Vector2(320f, 48f), true);
        TextMeshProUGUI question = UiFactory.CreateLabel(content, "Question", string.Empty, 46f,
            UiFactory.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -100f), new Vector2(1464f, 130f), true);

        RectTransform optionsRoot = UiFactory.CreateRect(content, "Options",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -238f), new Vector2(1240f, 430f));

        TextMeshProUGUI feedback = UiFactory.CreateLabel(content, "Feedback", string.Empty, 32f,
            UiFactory.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 206f), new Vector2(-96f, 110f));

        Button next = UiFactory.CreateButton(content, "NextButton", "Lanjut", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-48f, 42f), new Vector2(300f, 88f),
            new Color(0.22f, 0.45f, 0.24f, 0.95f), new Color(1f, 0.98f, 0.9f, 1f), 34f);
        Button close = UiFactory.CreateButton(content, "CloseButton", "Tutup", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(48f, 42f), new Vector2(220f, 80f),
            new Color(0.3f, 0.2f, 0.12f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);

        TextMeshProUGUI nextLabel = next.GetComponentInChildren<TextMeshProUGUI>();

        Quiz = gameObject.AddComponent<QuizPanel>();
        Quiz.Build(root, progress, hearts, question, optionsRoot, feedback, next, nextLabel, close);
    }

    private void BuildCurriculumExercise()
    {
        GameObject root = CreateModal("CurriculumExercisePanel", new Vector2(1400f, 760f),
            out RectTransform content);
        TextMeshProUGUI title = CreateHeading(content, "Latihan", -34f);
        TextMeshProUGUI question = UiFactory.CreateLabel(content, "Question", string.Empty, 38f,
            UiFactory.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -112f), new Vector2(1300f, 92f), true);
        RectTransform options = UiFactory.CreateRect(content, "Options", new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -218f),
            new Vector2(1180f, 360f));
        TextMeshProUGUI feedback = UiFactory.CreateLabel(content, "Feedback", string.Empty, 28f,
            UiFactory.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 156f), new Vector2(-96f, 80f));
        Button close = UiFactory.CreateButton(content, "CloseButton", "Tutup", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-48f, 42f), new Vector2(240f, 84f),
            new Color(0.3f, 0.2f, 0.12f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);

        CurriculumExercise = gameObject.AddComponent<CurriculumExercisePanel>();
        CurriculumExercise.Build(root, title, question, options, feedback, close);
    }

    private void BuildCurriculumDialogue()
    {
        GameObject root = CreateModal("CurriculumDialoguePanel", new Vector2(1400f, 760f),
            out RectTransform content);
        TextMeshProUGUI title = CreateHeading(content, "Dialog", -34f);
        TextMeshProUGUI body = UiFactory.CreateLabel(content, "Body", string.Empty, 34f,
            UiFactory.Ink, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        body.rectTransform.offsetMin = new Vector2(52f, 196f);
        body.rectTransform.offsetMax = new Vector2(-52f, -116f);
        TextMeshProUGUI hint = UiFactory.CreateLabel(content, "Hint", string.Empty, 24f,
            new Color(0.35f, 0.25f, 0.15f, 0.85f), TextAlignmentOptions.MidlineRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-48f, 124f), new Vector2(420f, 36f));
        Button audio = UiFactory.CreateButton(content, "AudioButton", "Putar audio", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(48f, 42f), new Vector2(320f, 80f),
            new Color(0.35f, 0.25f, 0.14f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 28f);
        Button next = UiFactory.CreateButton(content, "NextButton", "Lanjut", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-392f, 42f), new Vector2(300f, 80f),
            new Color(0.22f, 0.45f, 0.24f, 0.95f), new Color(1f, 0.98f, 0.9f, 1f), 30f);
        Button close = UiFactory.CreateButton(content, "CloseButton", "Tutup", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-48f, 42f), new Vector2(240f, 80f),
            new Color(0.3f, 0.2f, 0.12f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);

        CurriculumDialogue = gameObject.AddComponent<CurriculumDialoguePanel>();
        CurriculumDialogue.Build(root, title, body, hint, next, close, audio);
    }

    private void BuildSettings()    {
        GameObject root = CreateModal("SettingsPanel", new Vector2(1040f, 730f), out RectTransform content);
        CreateHeading(content, "Pengaturan", -34f);

        UiFactory.CreateLabel(content, "MusicLabel", "Volume musik", 32f, UiFactory.Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -116f), new Vector2(600f, 44f));
        Slider music = CreateSlider(content, "MusicSlider", new Vector2(48f, -162f), new Vector2(620f, 48f));

        UiFactory.CreateLabel(content, "SfxLabel", "Volume efek suara", 32f, UiFactory.Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -240f), new Vector2(600f, 44f));
        Slider sfx = CreateSlider(content, "SfxSlider", new Vector2(48f, -286f), new Vector2(620f, 48f));

        TextMeshProUGUI scheme = UiFactory.CreateLabel(content, "SchemeLabel", string.Empty, 32f,
            UiFactory.Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -374f), new Vector2(940f, 44f), true);
        Button schemeButton = UiFactory.CreateButton(content, "SchemeButton", "Ganti kontrol", null,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(48f, -436f), new Vector2(560f, 84f),
            new Color(0.35f, 0.25f, 0.14f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 32f);

        Button reset = UiFactory.CreateButton(content, "ResetButton", "Reset progres", null,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(48f, -546f), new Vector2(560f, 84f),
            new Color(0.55f, 0.22f, 0.18f, 0.92f), new Color(1f, 0.96f, 0.9f, 1f), 30f);
        TextMeshProUGUI resetLabel = reset.GetComponentInChildren<TextMeshProUGUI>();

        Button close = UiFactory.CreateButton(content, "CloseButton", "Tutup", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-48f, 42f), new Vector2(240f, 84f),
            new Color(0.3f, 0.2f, 0.12f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);

        Settings = gameObject.AddComponent<SettingsPanel>();
        Settings.Build(root, music, sfx, scheme, resetLabel, schemeButton, reset, close);
    }

    private void BuildQuest()
    {
        GameObject root = CreateModal("QuestPanel", new Vector2(1000f, 700f), out RectTransform content);
        CreateHeading(content, "Status Perjalanan", -34f);

        TextMeshProUGUI body = UiFactory.CreateLabel(content, "Body", string.Empty, 36f,
            UiFactory.Ink, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        body.rectTransform.offsetMin = new Vector2(48f, 110f);
        body.rectTransform.offsetMax = new Vector2(-48f, -118f);

        Button close = UiFactory.CreateButton(content, "CloseButton", "Tutup", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-48f, 42f), new Vector2(220f, 80f),
            new Color(0.3f, 0.2f, 0.12f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);

        Quest = gameObject.AddComponent<QuestPanel>();
        Quest.Build(root, body, close);
    }

    private void BuildTopics()
    {
        GameObject root = CreateModal("TopicPanel", new Vector2(1560f, 878f), out RectTransform content);
        Topics = gameObject.AddComponent<TopicPanel>();
        Topics.Build(root, content, panelSprite);
    }

    private void BuildProfile()
    {
        GameObject root = CreateModal("ProfilePanel", new Vector2(1400f, 860f), out RectTransform content);
        Profile = gameObject.AddComponent<ProfilePanel>();
        Profile.Build(root, content, panelSprite);
    }

    private void BuildWriting()
    {
        GameObject root = CreateModal("WritingPanel", new Vector2(1400f, 800f), out RectTransform content);
        Writing = gameObject.AddComponent<WritingPanel>();
        Writing.Build(root, content, panelSprite);
    }

    private void BuildSpeaking()
    {
        GameObject root = CreateModal("SpeechPanel", new Vector2(1200f, 720f), out RectTransform content);
        Speaking = gameObject.AddComponent<SpeechPracticePanel>();
        Speaking.Build(root, content, panelSprite);
    }

    private void BuildRecovery()
    {
        GameObject root = CreateModal("RecoveryPanel", new Vector2(940f, 600f), out RectTransform content);
        CreateHeading(content, "Hati Habis", -34f);

        TextMeshProUGUI body = UiFactory.CreateLabel(content, "Body", string.Empty, 36f,
            UiFactory.Ink, TextAlignmentOptions.TopLeft, new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        body.rectTransform.offsetMin = new Vector2(48f, 190f);
        body.rectTransform.offsetMax = new Vector2(-48f, -110f);

        Button useFruit = UiFactory.CreateButton(content, "UseFruitButton", "Pakai buah", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(48f, 46f), new Vector2(320f, 84f),
            new Color(0.72f, 0.42f, 0.12f, 0.95f), new Color(1f, 0.98f, 0.92f, 1f), 30f);
        Button goKebun = UiFactory.CreateButton(content, "GoKebunButton", "Ke Kebun", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(392f, 46f), new Vector2(300f, 84f),
            new Color(0.22f, 0.45f, 0.24f, 0.95f), new Color(1f, 0.98f, 0.9f, 1f), 30f);
        Button close = UiFactory.CreateButton(content, "CloseButton", "Tutup", null,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-48f, 46f), new Vector2(220f, 80f),
            new Color(0.3f, 0.2f, 0.12f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);

        Recovery = gameObject.AddComponent<RecoveryPanel>();
        Recovery.Build(root, body, useFruit, goKebun, close);
    }

    private Slider CreateSlider(Transform parent, string name, Vector2 position, Vector2 size)
    {
        RectTransform root = UiFactory.CreateRect(parent, name, new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        Slider slider = root.gameObject.AddComponent<Slider>();

        // Track: kotak beige membulat (bukan lingkaran) agar terlihat seperti bar.
        Image track = UiFactory.CreateImage(root, "Track", panelSprite,
            new Color(0.28f, 0.2f, 0.12f, 0.5f), Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, -18f));
        UiFactory.StylePanel(track);

        RectTransform fillArea = UiFactory.CreateRect(root, "FillArea", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -30f));
        Image fill = UiFactory.CreateImage(fillArea, "Fill", UiFactory.WhiteSprite,
            new Color(0.38f, 0.62f, 0.27f, 1f), Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform handleArea = UiFactory.CreateRect(root, "HandleArea", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, 0f));
        Image handle = UiFactory.CreateImage(handleArea, "Handle", circleSprite,
            new Color(0.99f, 0.93f, 0.78f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    // ---------------------------------------------------------------- API

    public void ShowInfo(LearningObjectDefinition definition)
    {
        Info?.Show(definition);
    }

    public void ShowDialogue(string title, string[] pages, Action closed = null)
    {
        Dialogue?.Show(title, pages, closed);
    }

    public void ShowMessage(string title, string body, Action closed = null)
    {
        Dialogue?.ShowMessage(title, body, closed);
    }

    public void ShowQuiz()
    {
        if (HealthService.Instance != null && HealthService.Instance.IsEmpty)
        {
            ToastView.Instance?.Show("Hati habis. Pulihkan dulu sebelum kuis.");
            ShowRecovery();
            return;
        }

        Quiz?.StartQuiz(Scene);
    }

    public void ShowCurriculumExercise(LessonItemData item, Action completed = null)
    {
        CurriculumExercise?.Show(item, completed);
    }

    public void ShowCurriculumDialogue(LessonItemData item)
    {
        CurriculumDialogue?.Show(item);
    }

    public void ShowSettings()
    {
        Settings?.Show();
    }

    public void ShowQuest()
    {
        Quest?.Show();
    }

    public void ShowTopics()
    {
        Topics?.Show(Scene != null ? Scene.database : null);
    }

    public void ShowProfile()
    {
        Profile?.Show();
    }

    public void ShowWriting(LearningObjectDefinition definition)
    {
        Writing?.Show(definition, Scene != null ? Scene.database : null);
    }

    public void ShowSpeaking(LearningObjectDefinition definition)
    {
        Speaking?.Show(definition);
    }

    public void ShowRecovery()
    {
        Recovery?.Show();
    }

    private void HeartsPressed()
    {
        if (HealthService.Instance != null && HealthService.Instance.IsEmpty)
        {
            ShowRecovery();
            return;
        }

        int hearts = HealthService.Instance != null ? HealthService.Instance.Hearts : 0;
        ToastView.Instance?.Show($"Hati {hearts}/{GameState.MaxHearts}. Hati hanya berkurang saat jawaban kuis salah.");
    }

    public void UseFruit()
    {
        if (InventoryService.Instance == null)
        {
            return;
        }
        bool used = InventoryService.Instance.TryUseFruit(out string message);
        if (used)
        {
            AudioManager.Instance?.PlaySfx(SfxId.Heart);
        }
        else
        {
            AudioManager.Instance?.PlaySfx(SfxId.Wrong);
        }

        ToastView.Instance?.Show(message);
        Hud?.Refresh();

        if (used && HealthService.Instance != null && HealthService.Instance.Hearts > 0)
        {
            Recovery?.Close();
        }
    }
}
