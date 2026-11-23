using UnityEngine;

/// <summary>
/// Urutan gambar sprite berdasarkan posisi Y (kaki) agar pemain bisa lewat di
/// depan/belakang objek. Semakin tinggi Y (semakin ke belakang layar), semakin
/// rendah sortingOrder sehingga tertutup oleh yang di depannya.
/// </summary>
public class YSort : MonoBehaviour
{
    [Tooltip("Tinggi peta dalam unit dunia; dipakai untuk menormalkan rentang order.")]
    public float mapHeight = 15.36f;

    [Tooltip("Geser titik acuan kaki (mis. negatif untuk objek rata di lantai).")]
    public float feetOffset = 0f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        float feetY = transform.position.y + feetOffset;
        spriteRenderer.sortingOrder = OrderFor(feetY, mapHeight);
    }

    /// <summary>Order 1..~32 untuk peta 15.36 unit: belakang (atas) lebih rendah.</summary>
    public static int OrderFor(float feetY, float mapHeight)
    {
        return Mathf.RoundToInt((mapHeight - feetY) * 2f) + 1;
    }
}
