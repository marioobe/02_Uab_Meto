using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple on-screen analogue stick. Works with mouse and touch through the
/// legacy StandaloneInputModule, so no Input System actions are required.
/// </summary>
public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 90f;
    [SerializeField] private float deadZone = 0.15f;

    private Canvas canvas;

    /// <summary>Normalized direction, magnitude 0..1.</summary>
    public Vector2 Value { get; private set; }

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (background == null)
        {
            background = transform as RectTransform;
        }
        ResetHandle();
    }

    public void Bind(RectTransform backgroundRect, RectTransform handleRect, float range = 90f)
    {
        background = backgroundRect;
        handle = handleRect;
        handleRange = range;
        ResetHandle();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null)
        {
            return;
        }

        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, handleRange);

        if (handle != null)
        {
            handle.anchoredPosition = clamped;
        }

        Vector2 raw = clamped / handleRange;
        Value = raw.magnitude < deadZone ? Vector2.zero : raw;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Value = Vector2.zero;
        ResetHandle();
    }

    private void OnDisable()
    {
        Value = Vector2.zero;
        ResetHandle();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Value = Vector2.zero;
            ResetHandle();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            Value = Vector2.zero;
            ResetHandle();
        }
    }

    private void Update()
    {
        // Safety net: if the touch/mouse was released without a pointer-up event
        // (screen lock, app pause, OS gesture), stop the movement.
        if (Value != Vector2.zero && !Input.GetMouseButton(0) && Input.touchCount == 0)
        {
            Value = Vector2.zero;
            ResetHandle();
        }
    }

    private void ResetHandle()
    {
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
