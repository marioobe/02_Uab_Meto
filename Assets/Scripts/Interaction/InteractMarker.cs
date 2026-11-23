using UnityEngine;

/// <summary>Denyut lembut untuk penanda objek yang bisa diinteraksi.</summary>
public class InteractMarker : MonoBehaviour
{
    public float pulseSpeed = 2.1f;
    public float minAlpha = 0.16f;
    public float maxAlpha = 0.46f;

    [Header("Fade jarak")]
    public float visibleDistance = 3.2f;
    public float fullAlphaDistance = 1.5f;

    private SpriteRenderer sprite;
    private Color baseColor;
    private Transform player;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            baseColor = sprite.color;
        }
    }

    private void Start()
    {
        // Pemain tidak memakai tag khusus; cari komponen geraknya sekali saja.
        PlayerMovement2D movement = FindFirstObjectByType<PlayerMovement2D>();
        if (movement != null)
        {
            player = movement.transform;
        }
    }

    private void Update()
    {
        if (sprite == null)
        {
            return;
        }

        float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        Color color = baseColor;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, wave) * Proximity();
        sprite.color = color;
    }

    /// <summary>Halo hanya terlihat saat pemain dekat, supaya scene tetap bersih.</summary>
    private float Proximity()
    {
        if (player == null)
        {
            return 1f;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        float range = Mathf.Max(0.01f, visibleDistance - fullAlphaDistance);
        return Mathf.Clamp01((visibleDistance - distance) / range);
    }
}
