using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Satu tombol arah yang ditahan. Pointer ID mencegah konflik multi-touch.</summary>
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Vector2 direction = Vector2.up;
    public bool IsHeld { get; private set; }

    private int pointerId = int.MinValue;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsHeld)
        {
            return;
        }

        IsHeld = true;
        pointerId = eventData.pointerId;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId)
        {
            Release();
        }
    }

    public void Release()
    {
        IsHeld = false;
        pointerId = int.MinValue;
    }

    private void OnDisable()
    {
        Release();
    }
}
