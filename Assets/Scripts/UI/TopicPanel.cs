using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel "Buku" versi kurikulum: daftar 12 topik (2 kolom), lalu daftar materi
/// di dalam panel saat topik diketuk. Progres dihitung dari learnedObjectIds
/// hanya untuk materi Utama; Pendukung tidak dihitung kosakata inti.
/// </summary>
public class TopicPanel : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.26f, 0.19f, 0.11f, 1f);
    private static readonly Color Muted = new Color(0.42f, 0.33f, 0.22f, 0.75f);
    private static readonly Color CardColor = new Color(0.35f, 0.25f, 0.14f, 0.10f);
    private static readonly Color CardColorMuted = new Color(0.35f, 0.25f, 0.14f, 0.05f);
    private static readonly Color Learned = new Color(0.16f, 0.45f, 0.22f, 1f);

    private GameObject root;
    private RectTransform content;
    private Sprite panelSprite;

    private RectTransform listView;
    private RectTransform detailView;
    private RectTransform listRows;
    private RectTransform detailRows;
    private TextMeshProUGUI detailTitle;
    private TextMeshProUGUI detailSubtitle;

    private LearningDatabase database;
    private CurriculumService curriculum;

    public void Build(GameObject panelRoot, RectTransform panelContent, Sprite sprite)
    {
        root = panelRoot;
        content = panelContent;
        panelSprite = sprite;

        listView = CreateSection(content, "TopicList");
        TextMeshProUGUI heading = UiFactory.CreateLabel(listView, "Heading", "Buku — 12 Topik", 46f,
            Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -30f), new Vector2(1200f, 56f), true);
        heading.raycastTarget = false;

        UiFactory.CreateLabel(listView, "Subheading", "Pilih topik untuk melihat daftar materi.",
            24f, Muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -88f), new Vector2(1200f, 32f));

        listRows = CreateScrollContent(listView, "Rows", new Vector2(48f, -130f), 600f);

        detailView = CreateSection(content, "TopicDetail");
        detailTitle = UiFactory.CreateLabel(detailView, "DetailTitle", string.Empty, 42f,
            Ink, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -30f), new Vector2(1200f, 56f), true);
        detailSubtitle = UiFactory.CreateLabel(detailView, "DetailSubtitle", string.Empty, 23f,
            Muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -88f), new Vector2(1300f, 32f));

        detailRows = CreateScrollContent(detailView, "DetailRows", new Vector2(48f, -136f), 560f);

        Button back = UiFactory.CreateButton(detailView, "BackButton", "Kembali", null,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(48f, 42f), new Vector2(240f, 84f),
            new Color(0.35f, 0.25f, 0.14f, 0.9f), new Color(1f, 0.96f, 0.86f, 1f), 30f);
        back.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            ShowRootList();
        });

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

    public void Show(LearningDatabase learningDatabase)
    {
        if (root == null)
        {
            return;
        }

        database = learningDatabase;
        curriculum = CurriculumService.Instance;
        ShowRootList();
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

    // ---------------------------------------------------------------- views

    private void ShowRootList()
    {
        curriculum ??= CurriculumService.Instance;
        if (curriculum == null)
        {
            curriculum = FindFirstObjectByType<CurriculumService>();
        }

        if (curriculum != null && curriculum.CanRender)
        {
            curriculum.RefreshCompletion();
            ShowCurriculumList();
        }
        else
        {
            ShowList();
        }
    }

    private void ShowList()
    {
        listView.gameObject.SetActive(true);
        detailView.gameObject.SetActive(false);
        RebuildTopicRows();
    }

    private void ShowCurriculumList()
    {
        listView.gameObject.SetActive(true);
        detailView.gameObject.SetActive(false);
        Clear(listRows);

        IReadOnlyList<TopicData> topics = curriculum.Topics;
        if (topics == null || topics.Count == 0)
        {
            UiFactory.CreateLabel(listRows, "Empty", "Belum ada materi kurikulum.",
                28f, Muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f),
                new Vector2(1200f, 48f));
            return;
        }

        for (int i = 0; i < topics.Count; i++)
        {
            TopicData topic = topics[i];
            int lessonCount = topic != null && topic.lessons != null ? topic.lessons.Count : 0;
            int itemCount = CurriculumMapping.CountItems(topic);
            bool available = lessonCount > 0;
            float x = (i / 6) * 744f;
            float y = -(i % 6) * 98f;

            RectTransform card = CreateCard(listRows, new Vector2(x, y), new Vector2(720f, 90f),
                available, out Button button);

            UiFactory.CreateLabel(card, "Name",
                $"{i + 1}. {CurriculumMapping.TopicDisplayName(topic)}", 27f,
                available ? Ink : new Color(0.45f, 0.36f, 0.25f, 0.55f),
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -8f), new Vector2(680f, 40f), true);

            string subtitle = available
                ? $"{lessonCount} pelajaran · {itemCount} materi"
                : "Segera Hadir";
            UiFactory.CreateLabel(card, "Subtitle", subtitle, 21f,
                available ? Muted : new Color(0.45f, 0.36f, 0.25f, 0.45f),
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -48f), new Vector2(680f, 30f));

            if (available && button != null)
            {
                TopicData captured = topic;
                button.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySfx(SfxId.Click);
                    ShowCurriculumTopic(captured);
                });
            }
        }

        listRows.sizeDelta = new Vector2(0f,
            Mathf.Max(600f, Mathf.CeilToInt(topics.Count / 6f) * 98f + 10f));
        listRows.anchoredPosition = Vector2.zero;
    }

    private void ShowCurriculumTopic(TopicData topic)
    {
        if (topic == null)
        {
            return;
        }

        listView.gameObject.SetActive(false);
        detailView.gameObject.SetActive(true);

        int lessonCount = topic.lessons != null ? topic.lessons.Count : 0;
        int itemCount = CurriculumMapping.CountItems(topic);
        detailTitle.text = CurriculumMapping.TopicDisplayName(topic);
        detailSubtitle.text = $"{lessonCount} pelajaran · {itemCount} materi";

        Clear(detailRows);
        if (topic.lessons == null || topic.lessons.Count == 0)
        {
            UiFactory.CreateLabel(detailRows, "Empty", "Belum ada materi pada topik ini.",
                26f, Muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f),
                new Vector2(1200f, 44f));
            return;
        }

        float y = 0f;
        for (int l = 0; l < topic.lessons.Count; l++)
        {
            LessonData lesson = topic.lessons[l];
            if (lesson == null)
            {
                continue;
            }

            int vocabulary = CurriculumMapping.VocabularyCount(lesson);
            int learned = CurriculumMapping.LearnedVocabularyCount(lesson);
            string suffix = vocabulary > 0 ? $" · {learned}/{vocabulary} kosakata" : string.Empty;
            UiFactory.CreateLabel(detailRows, "Lesson_" + lesson.id,
                CurriculumMapping.LessonDisplayName(lesson) + suffix, 28f, Ink,
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(1400f, 40f), true);
            y -= 44f;

            if (lesson.items != null)
            {
                for (int i = 0; i < lesson.items.Count; i++)
                {
                    CreateCurriculumItemRow(lesson.items[i], new Vector2(0f, y));
                    y -= 64f;
                }
            }
        }

        detailRows.sizeDelta = new Vector2(0f, Mathf.Max(560f, -y + 20f));
        detailRows.anchoredPosition = Vector2.zero;
    }

    private void CreateCurriculumItemRow(LessonItemData item, Vector2 position)
    {
        if (item == null)
        {
            return;
        }

        RectTransform row = UiFactory.CreateRect(detailRows, "CurriculumItem_" + item.id,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            position, new Vector2(1400f, 58f));

        Image bg = UiFactory.CreateImage(row, "Bg", panelSprite, CardColor,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        UiFactory.StylePanel(bg);

        LearningObjectDefinition definition = null;
        bool hasAction = false;
        string label;
        string status;
        bool learned = false;

        if (item.type == "vocabulary")
        {
            if (curriculum.TryResolveVocabulary(item, out definition))
            {
                hasAction = GameUi.Instance != null;
                label = $"{definition.namaIndonesia} — {definition.DisplayDawan}";
                learned = GameState.Ensure().IsLearned(definition.id);
                status = learned ? "Dipelajari" : "Belum";
            }
            else
            {
                label = "Kosakata tidak tersedia: " + item.objectRef;
                status = "Tidak tersedia";
            }
        }
        else if (item.type == "exercise")
        {
            hasAction = GameUi.Instance != null;
            label = "Latihan: " + CurriculumMapping.ItemLabel(item);
            status = "Latihan";
        }
        else if (item.type == "dialogue")
        {
            hasAction = GameUi.Instance != null;
            label = "Dialog: " + CurriculumMapping.ItemLabel(item);
            status = "Dialog";
        }
        else
        {
            label = CurriculumMapping.ItemLabel(item);
            status = string.IsNullOrWhiteSpace(item.type) ? "Materi" : item.type;
        }

        TextMeshProUGUI nameLabel = UiFactory.CreateLabel(row, "Name", label, 23f,
            item.type == "vocabulary" && !hasAction ? Muted : Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 0.5f), new Vector2(16f, 29f), new Vector2(1120f, 38f));
        nameLabel.overflowMode = TextOverflowModes.Ellipsis;

        UiFactory.CreateLabel(row, "Status", status, 19f,
            learned ? Learned : Muted, TextAlignmentOptions.MidlineRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f),
            new Vector2(-16f, 29f), new Vector2(220f, 38f), learned);

        if (!hasAction)
        {
            bg.raycastTarget = false;
            return;
        }

        Button button = bg.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;
        button.transition = Selectable.Transition.None;

        LearningObjectDefinition captured = definition;
        LessonItemData capturedItem = item;
        button.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            if (capturedItem.type == "vocabulary")
            {
                if (captured != null)
                {
                    GameUi.Instance?.ShowInfo(captured);
                }
            }
            else if (capturedItem.type == "exercise")
            {
                GameUi.Instance?.ShowCurriculumExercise(capturedItem);
            }
            else if (capturedItem.type == "dialogue")
            {
                GameUi.Instance?.ShowCurriculumDialogue(capturedItem);
            }
        });
    }

    private void ShowTopic(LearningTopic topic)
    {
        if (database == null || !database.HasContent(topic))
        {
            return;
        }

        listView.gameObject.SetActive(false);
        detailView.gameObject.SetActive(true);

        List<LearningObjectDefinition> primary = database.ForTopic(topic, false);
        List<LearningObjectDefinition> supporting = database.ForTopic(topic, true);
        supporting.RemoveAll(item => primary.Contains(item));

        detailTitle.text = $"{((int)topic)}. {LearningTopicCatalog.NamaOf(topic)}";
        detailSubtitle.text =
            $"{primary.Count} materi utama · {supporting.Count} pendukung · " +
            $"{LearnedCount(primary)}/{primary.Count} dipelajari";

        Clear(detailRows);

        List<LearningObjectDefinition> ordered = new List<LearningObjectDefinition>();
        ordered.AddRange(primary);
        ordered.AddRange(supporting);

        for (int i = 0; i < ordered.Count; i++)
        {
            LearningObjectDefinition item = ordered[i];
            bool isSupporting = item.role != LearningRole.Utama;
            float x = (i / 7) * 744f;
            float y = -(i % 7) * 68f;
            CreateDetailRow(item, isSupporting, new Vector2(x, y));
        }

        detailRows.sizeDelta = new Vector2(0f,
            Mathf.Max(560f, Mathf.CeilToInt(ordered.Count / 7f) * 68f + 10f));
        detailRows.anchoredPosition = Vector2.zero;
    }

    // ---------------------------------------------------------------- rows

    private void RebuildTopicRows()
    {
        Clear(listRows);

        for (int i = 0; i < LearningTopicCatalog.All.Length; i++)
        {
            LearningTopicCatalog.Entry entry = LearningTopicCatalog.All[i];
            LearningTopic topic = entry.Topic;
            int primaryCount = database != null ? database.PrimaryCount(topic) : 0;
            int supportingCount = database != null ? database.SupportingCount(topic) : 0;
            bool available = primaryCount + supportingCount > 0;

            float x = (i / 6) * 744f;
            float y = -(i % 6) * 98f;

            RectTransform card = CreateCard(listRows, new Vector2(x, y), new Vector2(720f, 90f),
                available, out Button button);

            string label = $"{i + 1}. {entry.Nama}";
            UiFactory.CreateLabel(card, "Name", label, 27f,
                available ? Ink : new Color(0.45f, 0.36f, 0.25f, 0.55f),
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -8f), new Vector2(680f, 40f), true);

            string subtitle;
            if (!available)
            {
                subtitle = "Segera Hadir";
            }
            else
            {
                int learned = LearnedCount(database.ForTopic(topic, false));
                subtitle = $"{primaryCount} materi utama";
                if (supportingCount > 0)
                {
                    subtitle += $" · {supportingCount} pendukung";
                }

                subtitle += $" · {learned}/{primaryCount} dipelajari";
            }

            UiFactory.CreateLabel(card, "Subtitle", subtitle, 21f,
                available ? Muted : new Color(0.45f, 0.36f, 0.25f, 0.45f),
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -48f), new Vector2(680f, 30f));

            if (available && button != null)
            {
                LearningTopic captured = topic;
                button.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySfx(SfxId.Click);
                    ShowTopic(captured);
                });
            }
        }

        listRows.sizeDelta = new Vector2(0f,
            Mathf.Max(600f, Mathf.CeilToInt(LearningTopicCatalog.All.Length / 6f) * 98f + 10f));
        listRows.anchoredPosition = Vector2.zero;
    }

    private void CreateDetailRow(LearningObjectDefinition item, bool supporting, Vector2 position)
    {
        RectTransform row = UiFactory.CreateRect(detailRows, "Detail_" + item.id,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            position, new Vector2(720f, 60f));

        Image bg = UiFactory.CreateImage(row, "Bg", panelSprite, CardColor,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        UiFactory.StylePanel(bg);
        bg.raycastTarget = false;

        string dawan = string.IsNullOrWhiteSpace(item.namaDawan) ? "—" : item.DisplayDawan;
        string name = $"{item.namaIndonesia} — {dawan}";
        if (supporting)
        {
            name += "  · pendukung";
        }

        bool learned = GameState.Ensure().IsLearned(item.id);
        TextMeshProUGUI nameLabel = UiFactory.CreateLabel(row, "Name", name, 24f,
            supporting ? Muted : Ink, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0.5f),
            new Vector2(16f, 30f), new Vector2(520f, 40f));
        nameLabel.overflowMode = TextOverflowModes.Ellipsis;

        UiFactory.CreateLabel(row, "Status",
            learned ? "Dipelajari" : "Belum", 20f,
            learned ? Learned : Muted, TextAlignmentOptions.MidlineRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f),
            new Vector2(-16f, 30f), new Vector2(130f, 40f), learned);
    }

    private RectTransform CreateCard(RectTransform parent, Vector2 position, Vector2 size,
        bool available, out Button button)
    {
        RectTransform rect = UiFactory.CreateRect(parent, "Card", new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);

        Image bg = UiFactory.CreateImage(rect, "Bg", panelSprite,
            available ? CardColor : CardColorMuted,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        UiFactory.StylePanel(bg);

        button = null;
        if (available)
        {
            button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
        }
        else
        {
            bg.raycastTarget = false;
        }

        return rect;
    }

    // ---------------------------------------------------------------- util

    private static int LearnedCount(List<LearningObjectDefinition> items)
    {
        if (items == null || items.Count == 0)
        {
            return 0;
        }

        GameState state = GameState.Ensure();
        int learned = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (state.IsLearned(items[i].id))
            {
                learned++;
            }
        }

        return learned;
    }

    private static RectTransform CreateScrollContent(RectTransform parent, string name,
        Vector2 position, float height)
    {
        RectTransform viewport = UiFactory.CreateRect(parent, name + "Viewport",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            position, new Vector2(-96f, height));
        Image image = viewport.gameObject.AddComponent<Image>();
        image.sprite = UiFactory.WhiteSprite;
        image.color = new Color(1f, 1f, 1f, 0.01f);
        image.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32f;
        scroll.inertia = true;

        RectTransform content = UiFactory.CreateRect(viewport, name,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(0f, height));
        scroll.content = content;
        return content;
    }

    private static RectTransform CreateSection(RectTransform parent, string name)
    {
        RectTransform rect = UiFactory.CreateRect(parent, name, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void Clear(RectTransform container)
    {
        if (container == null)
        {
            return;
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}
