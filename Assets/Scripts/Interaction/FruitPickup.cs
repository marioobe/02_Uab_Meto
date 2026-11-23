using UnityEngine;

/// <summary>
/// Buah yang bisa diambil. rescueOnly = keranjang darurat di Kebun yang muncul
/// hanya saat hati 0 dan buah 0 supaya pemain tidak softlock.
/// </summary>
public class FruitPickup : MonoBehaviour, IInteractable
{
    public string pickupId;
    public int amount = 1;
    public bool rescueOnly;
    public float rescueCooldownSeconds = 30f;
    [SerializeField] private SpriteRenderer icon;

    private float nextRefresh;
    private bool eligible;

    public string PromptText => rescueOnly ? "Ambil buah darurat" : "Ambil";
    public string PromptLabel => "Ambil";

    public void Bind(string id, bool rescue = false, int fruitAmount = 1)
    {
        pickupId = id;
        rescueOnly = rescue;
        amount = Mathf.Max(1, fruitAmount);
        if (icon == null)
        {
            icon = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + 0.25f;
        Refresh();
    }

    private void Refresh()
    {
        GameState state = GameState.Ensure();
        bool collected = !string.IsNullOrEmpty(pickupId) && state.IsCollected(pickupId);

        if (rescueOnly)
        {
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool cooldownDone = now - state.Data.lastRescuePickupUnix >= (long)rescueCooldownSeconds;
            eligible = !collected && state.Data.hearts <= 0 && state.Data.fruit <= 0 && cooldownDone;
        }
        else
        {
            eligible = !collected;
        }

        if (icon != null)
        {
            icon.enabled = eligible;
        }

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.enabled = eligible;
        }
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!eligible)
        {
            return;
        }

        GameState state = GameState.Ensure();

        if (rescueOnly)
        {
            if (state.Data.hearts > 0 || state.Data.fruit > 0)
            {
                return;
            }

            state.Data.lastRescuePickupUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        else
        {
            // XP buah hanya saat buah biasa benar-benar berhasil dikumpulkan (sekali).
            if (state.MarkCollected(pickupId))
            {
                GamificationService.OnFruitCollected(pickupId);
            }
        }

        InventoryService.Instance?.AddFruit(amount);
        AudioManager.Instance?.PlaySfx(SfxId.Pickup);
        ToastView.Instance?.Show($"+{amount} buah. Buka HUD untuk memakai.");
        SaveService.Instance?.SaveNow();
        Refresh();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = rescueOnly ? new Color(1f, 0.5f, 0.2f, 0.8f) : new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
