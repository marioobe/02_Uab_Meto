using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD kiri-atas: hati, jumlah buah, dan tombol pengaturan/status.
/// Semua angka mengikuti GameState, bukan salinan lokal.
/// </summary>
public class HudView : MonoBehaviour
{
    private Image[] hearts;
    private Sprite heartFull;
    private Sprite heartEmpty;
    private TextMeshProUGUI fruitLabel;
    private TextMeshProUGUI heartsLabel;
    private TextMeshProUGUI fruitHint;
    private TextMeshProUGUI levelLabel;
    private Button fruitButton;

    private bool subscribed;

    private static readonly Color PillColor = new Color(0.11f, 0.08f, 0.05f, 0.66f);
    private static readonly Color CreamText = new Color(1f, 0.96f, 0.86f, 1f);

    public void Build(RectTransform parent, Sprite panel, Sprite full, Sprite empty, Sprite fruitIcon,
        Sprite settingsIcon, Action onSettings, Action onQuest, Action onProfile, Action onFruit,
        Action onHearts)
    {
        heartFull = full;
        heartEmpty = empty;

        // ---------------------------------------------------------- baris hati
        Image heartsPill = UiFactory.CreateImage(parent, "HeartsPill", panel, PillColor,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(28f, -24f), new Vector2(330f, 100f));
        UiFactory.StylePanel(heartsPill);
        heartsPill.raycastTarget = true;

        Button heartsButton = heartsPill.gameObject.AddComponent<Button>();
        heartsButton.targetGraphic = heartsPill;
        heartsButton.transition = Selectable.Transition.None;
        heartsButton.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            onHearts?.Invoke();
        });

        hearts = new Image[GameState.MaxHearts];
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i] = UiFactory.CreateImage(heartsPill.transform, $"Heart_{i}", heartFull, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(14f + i * 78f, 0f), new Vector2(72f, 72f), false);
            hearts[i].preserveAspect = true;
        }

        heartsLabel = UiFactory.CreateLabel(heartsPill.transform, "HeartsText", string.Empty, 30f,
            CreamText, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(252f, 0f), new Vector2(72f, 40f), true);
        UiFactory.AddOutline(heartsLabel);

        // ------------------------------------------------------- baris buah
        Image fruitPill = UiFactory.CreateImage(parent, "FruitPill", panel, PillColor,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(28f, -136f), new Vector2(330f, 100f));
        UiFactory.StylePanel(fruitPill);
        fruitPill.raycastTarget = true;

        fruitButton = fruitPill.gameObject.AddComponent<Button>();
        fruitButton.targetGraphic = fruitPill;
        fruitButton.transition = Selectable.Transition.None;
        if (onFruit != null)
        {
            fruitButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlaySfx(SfxId.Click);
                onFruit();
            });
        }

        Image icon = UiFactory.CreateImage(fruitPill.transform, "Icon", fruitIcon, Color.white,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(16f, 0f), new Vector2(64f, 64f), false);
        icon.preserveAspect = true;
        icon.enabled = fruitIcon != null;

        fruitLabel = UiFactory.CreateLabel(fruitPill.transform, "Count", "0", 44f,
            CreamText, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(96f, 4f), new Vector2(90f, 56f), true);
        UiFactory.AddOutline(fruitLabel);

        fruitHint = UiFactory.CreateLabel(fruitPill.transform, "UseHint", "pakai", 24f,
            new Color(1f, 1f, 1f, 0.62f), TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(178f, 4f), new Vector2(130f, 40f));
        UiFactory.AddOutline(fruitHint, 0.12f);

        // ------------------------------------------------------ level / XP
        Image levelPill = UiFactory.CreateImage(parent, "LevelPill", panel, PillColor,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(28f, -248f), new Vector2(330f, 76f));
        UiFactory.StylePanel(levelPill);
        levelPill.raycastTarget = false;

        levelLabel = UiFactory.CreateLabel(levelPill.transform, "LevelText", "Lv 1", 26f,
            CreamText, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, 0f), new Vector2(300f, 40f), true);
        UiFactory.AddOutline(levelLabel);

        // --------------------------------------------------------- tombol kanan
        Button settings = UiFactory.CreateButton(parent, "SettingsButton", "Atur", panel,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-28f, -24f), new Vector2(112f, 92f),
            PillColor, CreamText, 30f);
        if (settingsIcon != null)
        {
            TextMeshProUGUI label = settings.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = string.Empty;
            }

            Image settingsIconImage = UiFactory.CreateImage(settings.transform, "Icon", settingsIcon, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(56f, 56f), false);
            settingsIconImage.preserveAspect = true;
        }
        settings.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            onSettings?.Invoke();
        });

        Button quest = UiFactory.CreateButton(parent, "QuestButton", "Buku", panel,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-152f, -24f), new Vector2(140f, 92f),
            PillColor, CreamText, 30f);
        quest.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            onQuest?.Invoke();
        });

        Button profile = UiFactory.CreateButton(parent, "ProfileButton", "Profil", panel,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-296f, -24f), new Vector2(140f, 92f),
            PillColor, CreamText, 30f);
        profile.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxId.Click);
            onProfile?.Invoke();
        });

        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        subscribed = true;
        GameState.Changed += Refresh;
        if (HealthService.Instance != null)
        {
            HealthService.Instance.Changed += OnHeartsChanged;
        }

        if (InventoryService.Instance != null)
        {
            InventoryService.Instance.Changed += OnFruitChanged;
        }
    }

    private void OnDestroy()
    {
        GameState.Changed -= Refresh;
        if (HealthService.Instance != null)
        {
            HealthService.Instance.Changed -= OnHeartsChanged;
        }

        if (InventoryService.Instance != null)
        {
            InventoryService.Instance.Changed -= OnFruitChanged;
        }
    }

    private void OnHeartsChanged(int current, int max)
    {
        Refresh();
    }

    private void OnFruitChanged(int amount)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (hearts == null)
        {
            return;
        }

        int current = HealthService.Instance != null
            ? HealthService.Instance.Hearts
            : GameState.Ensure().Data.hearts;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null)
            {
                continue;
            }

            bool filled = i < current;
            hearts[i].sprite = filled ? heartFull : heartEmpty;
            hearts[i].enabled = heartFull != null || heartEmpty != null;
            hearts[i].color = filled ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }

        if (heartsLabel != null)
        {
            heartsLabel.text = $"{current}/{GameState.MaxHearts}";
        }

        int fruit = InventoryService.Instance != null
            ? InventoryService.Instance.Fruit
            : GameState.Ensure().Data.fruit;

        if (fruitLabel != null)
        {
            fruitLabel.text = fruit.ToString();
        }

        if (fruitHint != null)
        {
            bool hasFruit = fruit > 0;
            bool full = HealthService.Instance != null && HealthService.Instance.IsFull;
            // Buah habis lebih penting daripada status hati: tampilkan "kosong" dahulu.
            fruitHint.text = !hasFruit ? "kosong" : (full ? "penuh" : "pakai");
            fruitHint.color = new Color(1f, 1f, 1f, hasFruit && !full ? 0.8f : 0.45f);
        }

        if (levelLabel != null)
        {
            SaveData data = GameState.Ensure().Data;
            levelLabel.text =
                $"Lv {Mathf.Max(1, data.level)} · " +
                $"{GamificationService.XpIntoLevel(data.xp)}/{GamificationService.XpPerLevel} XP";
        }

        if (fruitButton != null)
        {
            // Tetap aktif supaya pemain mendapat pesan saat hati penuh.
            fruitButton.interactable = true;
        }
    }
}
