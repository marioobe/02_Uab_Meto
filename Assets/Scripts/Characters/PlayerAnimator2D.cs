using UnityEngine;

/// <summary>
/// Frame by frame animation for the village player.
/// Uses plain sprite arrays instead of an AnimatorController so the state is
/// fully controlled from code (no transition assets to maintain).
/// </summary>
public class PlayerAnimator2D : MonoBehaviour
{
    public enum Facing
    {
        Down = 0,
        Side = 1,
        Up = 2
    }

    [Header("Referensi")]
    [SerializeField] private SpriteRenderer target;

    [Header("Kecepatan")]
    [SerializeField] private float walkFps = 10f;
    [SerializeField] private float idleFps = 3.5f;

    [Header("Frame")]
    [SerializeField] private Sprite[] idleDown = new Sprite[0];
    [SerializeField] private Sprite[] walkDown = new Sprite[0];
    [SerializeField] private Sprite[] idleSide = new Sprite[0];
    [SerializeField] private Sprite[] walkSide = new Sprite[0];
    [SerializeField] private Sprite[] idleUp = new Sprite[0];
    [SerializeField] private Sprite[] walkUp = new Sprite[0];

    private Facing facing = Facing.Down;
    private bool moving;
    private bool initialized;
    private float timer;
    private int frame;

    public void Bind(SpriteRenderer renderer, Sprite[] idleDownFrames, Sprite[] walkDownFrames,
        Sprite[] idleSideFrames, Sprite[] walkSideFrames, Sprite[] idleUpFrames, Sprite[] walkUpFrames)
    {
        target = renderer;
        idleDown = idleDownFrames;
        walkDown = walkDownFrames;
        idleSide = idleSideFrames;
        walkSide = walkSideFrames;
        idleUp = idleUpFrames;
        walkUp = walkUpFrames;

        ApplyFrame();
        initialized = true;
    }

    public void SetState(Facing requestedFacing, bool isMoving)
    {
        if (initialized && requestedFacing == facing && isMoving == moving)
        {
            return;
        }

        facing = requestedFacing;
        moving = isMoving;
        frame = 0;
        timer = 0f;
        ApplyFrame();
        initialized = true;
    }

    private void Update()
    {
        Sprite[] frames = CurrentFrames();
        if (frames == null || frames.Length < 2)
        {
            return;
        }

        float fps = moving ? walkFps : idleFps;
        timer += Time.deltaTime;

        float frameDuration = 1f / Mathf.Max(fps, 0.01f);
        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            frame = (frame + 1) % frames.Length;
            ApplyFrame();
        }
    }

    private Sprite[] CurrentFrames()
    {
        Sprite[] walking = facing switch
        {
            Facing.Side => walkSide,
            Facing.Up => walkUp,
            _ => walkDown
        };

        if (moving)
        {
            return walking;
        }

        Sprite[] idle = facing switch
        {
            Facing.Side => idleSide,
            Facing.Up => idleUp,
            _ => idleDown
        };

        // Fallback: no dedicated idle frames for this direction, use the first walk frame.
        return idle != null && idle.Length > 0 ? idle : walking;
    }

    private void ApplyFrame()
    {
        if (target == null)
        {
            return;
        }

        Sprite[] frames = CurrentFrames();
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        frame = Mathf.Clamp(frame, 0, frames.Length - 1);
        target.sprite = frames[frame];
    }
}
