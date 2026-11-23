using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD kiri-atas: tiga hati (nyawa) dan jumlah hasil kebun yang sudah dikumpulkan.
/// Ikon diambil dari pak aset (Assets/Art/UI/Hud).
/// </summary>
public class HudController : MonoBehaviour
{
    public static HudController Instance { get; private set; }

    [SerializeField] private Image[] hearts = new Image[0];
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartEmpty;
    [SerializeField] private Text foodLabel;

    private void Awake()
    {
        Instance = this;
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Bind(Image[] heartImages, Sprite full, Sprite empty, Text foodText)
    {
        hearts = heartImages;
        heartFull = full;
        heartEmpty = empty;
        foodLabel = foodText;
        Refresh();
    }

    public void Refresh()
    {
        if (hearts != null)
        {
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null)
                {
                    continue;
                }

                bool filled = i < GameSession.Hearts;
                hearts[i].sprite = filled ? heartFull : heartEmpty;
                hearts[i].color = filled ? Color.white : new Color(1f, 1f, 1f, 0.85f);
            }
        }

        if (foodLabel != null)
        {
            foodLabel.text = GameSession.FoodCollected.ToString();
        }
    }

    public static void RefreshAll()
    {
        if (Instance != null)
        {
            Instance.Refresh();
        }
    }
}
