using UnityEngine;

/// <summary>Meneruskan input dari skema aktif (D-Pad atau joystick) ke movement.</summary>
public class InputRouter : MonoBehaviour
{
    public DPadView dpad;
    public MobileJoystick joystick;
    public bool useDPad = true;

    public Vector2 Value
    {
        get
        {
            if (UiModalState.IsBlocking ||
                (SceneRouter.Instance != null && SceneRouter.Instance.IsTransitioning))
            {
                return Vector2.zero;
            }

            if (useDPad)
            {
                Vector2 pad = dpad != null ? dpad.Value : Vector2.zero;
                if (pad.sqrMagnitude > 0.0001f)
                {
                    return pad;
                }

                return Application.isEditor ? Keyboard() : Vector2.zero;
            }

            Vector2 stick = joystick != null ? joystick.Value : Vector2.zero;
            if (stick.sqrMagnitude > 0.04f)
            {
                return Vector2.ClampMagnitude(stick, 1f);
            }

            return Application.isEditor ? Keyboard() : Vector2.zero;
        }
    }

    public void Bind(DPadView pad, MobileJoystick stick)
    {
        dpad = pad;
        joystick = stick;
    }

    public void SetUseDPad(bool value)
    {
        useDPad = value;
        if (dpad != null)
        {
            dpad.ReleaseAll();
        }
    }

    private static Vector2 Keyboard()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
}
