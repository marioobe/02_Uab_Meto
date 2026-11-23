using UnityEngine;

/// <summary>Empat tombol arah. Diagonal = dua tombol; berlawanan saling meniadakan.</summary>
public class DPadView : MonoBehaviour
{
    public HoldButton up;
    public HoldButton down;
    public HoldButton left;
    public HoldButton right;

    public Vector2 Value
    {
        get
        {
            float x = (right != null && right.IsHeld ? 1f : 0f) - (left != null && left.IsHeld ? 1f : 0f);
            float y = (up != null && up.IsHeld ? 1f : 0f) - (down != null && down.IsHeld ? 1f : 0f);
            Vector2 value = new Vector2(x, y);
            return value.sqrMagnitude > 1f ? value.normalized : value;
        }
    }

    public void Bind(HoldButton upButton, HoldButton downButton, HoldButton leftButton, HoldButton rightButton)
    {
        up = upButton;
        down = downButton;
        left = leftButton;
        right = rightButton;
    }

    public void ReleaseAll()
    {
        up?.Release();
        down?.Release();
        left?.Release();
        right?.Release();
    }

    private void Update()
    {
        if (UiModalState.IsBlocking ||
            (SceneRouter.Instance != null && SceneRouter.Instance.IsTransitioning))
        {
            ReleaseAll();
        }
    }
}
