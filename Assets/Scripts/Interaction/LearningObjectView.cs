using UnityEngine;

/// <summary>
/// Tampilan visual objek belajar (sprite transparan) yang dipasang pada trigger
/// LearningObjectInteractable. Posisi/scale datang dari scene builder; komponen
/// ini hanya mengurus renderer dan Y-sorting.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LearningObjectView : MonoBehaviour
{
    public void Bind(Sprite sprite, float mapHeight, float feetOffset = 0f)
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        YSort sort = gameObject.AddComponent<YSort>();
        sort.mapHeight = mapHeight;
        sort.feetOffset = feetOffset;
    }
}
