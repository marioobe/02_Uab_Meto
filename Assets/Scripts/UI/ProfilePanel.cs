using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel Profil (Fase B): level, XP, streak, 9 badge, dan leaderboard lokal
/// (maksimum 20 catatan, ditampilkan 10 teratas).
/// </summary>
public class ProfilePanel : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.26f, 0.19f, 0.11f, 1f);
    private static readonly Color Muted = new Color(0.42f, 0.33f, 0.22f, 0.72f);
    private static readonly Color Gold = new Color(0.55f, 0.36f, 0.08f, 1f);
    private static readonly Color Locked = new Color(0.42f, 0.33f, 0.22f, 0.45f);
    private static readonly Color Learned = new Color(0.16f, 0.45f, 0.22f, 1f);

    private GameObject root;
    private TextMeshProUGUI levelLabel;
    private TextMeshProUGUI xpLabel;
    private TextMeshProUGUI streakLabel;
    private TextMeshProUGUI badgeHeader;
    private TextMeshProUGUI[] badgeLabels;
    private TextMeshProUGUI[] scoreLabels;
    private TextMeshProUGUI emptyLabel;

    public void Build(GameObject panelRoot, RectTransform content, Sprite panelSprite)
    {
        root = panelRoot;

        UiFactory.CreateLabel(content, "Title", "Profil Petualang", 40f, Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -28f), new Vector2(800f, 50f), true);

        levelLabel = UiFactory.CreateLabel(content, "Level", "Level 1", 36f, Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -92f), new Vector2(600f, 44f), true);

        xpLabel = UiFactory.CreateLabel(content, "Xp", string.Empty, 24f, Muted,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -140f), new Vector2(620f, 32f));

        streakLabel = UiFactory.CreateLabel(content, "Streak", string.Empty, 24f, Muted,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -180f), new Vector2(620f, 32f));

        badgeHeader = UiFactory.CreateLabel(content, "BadgeHeader", "Badge", 26f, Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(48f, -236f), new Vector2(620f, 34f), true);

        badgeLabels = new TextMeshProUGUI[BadgeCatalog.All.Length];
        for (int i = 0; i < BadgeCatalog.All.Length; i++)
        {
            float x = 48f + (i / 5) * 330f;
            float y = -278f - (i % 5) * 56f;
            badgeLabels[i] = UiFactory.CreateLabel(content, "Badge_" + i, string.Empty, 21f, Muted,
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(x, y), new Vector2(310f, 48f));
        }

        UiFactory.CreateLabel(content, "ScoreHeader", "Papan Skor (lokal)", 26f, Ink,
            TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(742f, -92f), new Vector2(600f, 34f), true);

        scoreLabels = new TextMeshProUGUI[10];
        for (int i = 0; i < scoreLabels.Length; i++)
        {
            scoreLabels[i] = UiFactory.CreateLabel(content, "Score_" + i, string.Empty, 21f, Muted,
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(742f, -140f - i * 54f), new Vector2(600f, 46f));
        }

        emptyLabel = UiFactory.CreateLabel(content, "ScoreEmpty", "Belum ada skor kuis.",
            22f, Muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(742f, -140f), new Vector2(600f, 34f));

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

    public void Show()
    {
        if (root == null)
        {
            return;
        }

        Refresh();
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

    private void Refresh()
    {
        SaveData data = GameState.Ensure().Data;
        int level = Mathf.Max(1, data.level);
        int into = GamificationService.XpIntoLevel(data.xp);

        levelLabel.text = $"Level {level}";
        xpLabel.text = $"XP {data.xp} · {into}/{GamificationService.XpPerLevel} menuju Level {level + 1}";
        streakLabel.text = $"Streak: {Mathf.Max(0, data.streak)} hari";
        badgeHeader.text = $"Badge ({data.earnedBadges.Count}/{BadgeCatalog.All.Length})";

        for (int i = 0; i < BadgeCatalog.All.Length; i++)
        {
            BadgeCatalog.Entry entry = BadgeCatalog.All[i];
            bool earned = data.earnedBadges.Contains(entry.Id);
            badgeLabels[i].text = earned ? entry.Nama : entry.Nama + " (terkunci)";
            badgeLabels[i].color = earned ? Gold : Locked;
            badgeLabels[i].fontStyle = earned ? FontStyles.Bold : FontStyles.Normal;
        }

        int shown = 0;
        for (int i = 0; i < scoreLabels.Length && i < data.leaderboard.Count; i++)
        {
            QuizScoreEntry entry = data.leaderboard[i];
            if (entry == null)
            {
                continue;
            }

            scoreLabels[i].text =
                $"#{i + 1}  {entry.score}/100  {entry.sceneKey}  {entry.date}";
            scoreLabels[i].color = i == 0 ? Learned : Muted;
            scoreLabels[i].gameObject.SetActive(true);
            shown++;
        }

        for (int i = shown; i < scoreLabels.Length; i++)
        {
            scoreLabels[i].gameObject.SetActive(false);
        }

        emptyLabel.gameObject.SetActive(data.leaderboard.Count == 0);
    }
}
