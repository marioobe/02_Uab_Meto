using UnityEngine;

/// <summary>
/// Eight-direction movement for the village player.
/// Uses the legacy Input Manager axes (Horizontal/Vertical) plus an optional
/// on-screen joystick so the same component works on desktop and touch devices.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Gerak")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Referensi")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private MobileJoystick joystick;
    [SerializeField] private InputRouter router;
    [SerializeField] private PlayerAnimator2D animator;

    private Rigidbody2D body;
    private Vector2 input;

    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    /// <summary>Set to true to freeze the player (cutscenes, transitions).</summary>
    public bool MovementLocked { get; set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<PlayerAnimator2D>();
        }
    }

    public void Bind(MobileJoystick joystickInput, SpriteRenderer renderer = null)
    {
        joystick = joystickInput;
        if (renderer != null)
        {
            spriteRenderer = renderer;
        }
    }

    public void BindAnimator(PlayerAnimator2D playerAnimator)
    {
        animator = playerAnimator;
    }

    /// <summary>Dipakai scene baru: input datang dari D-Pad atau joystick aktif.</summary>
    public void BindRouter(InputRouter inputRouter)
    {
        router = inputRouter;
    }

    private void Update()
    {
        input = ReadInput();

        bool moving = input.sqrMagnitude > 0.0001f;
        if (moving)
        {
            FacingDirection = input.normalized;

            if (spriteRenderer != null && Mathf.Abs(input.x) > 0.05f)
            {
                spriteRenderer.flipX = input.x < 0f;
            }
        }

        UpdateAnimation(moving);
    }

    private void UpdateAnimation(bool moving)
    {
        if (animator == null)
        {
            return;
        }

        PlayerAnimator2D.Facing facing;

        // The art only has down / side / up rows, so diagonal input is snapped:
        // horizontal wins when the X part is clearly larger.
        if (Mathf.Abs(FacingDirection.x) > Mathf.Abs(FacingDirection.y) + 0.05f)
        {
            facing = PlayerAnimator2D.Facing.Side;
        }
        else if (FacingDirection.y > 0f)
        {
            facing = PlayerAnimator2D.Facing.Up;
        }
        else
        {
            facing = PlayerAnimator2D.Facing.Down;
        }

        animator.SetState(facing, moving);
    }

    private Vector2 ReadInput()
    {
        if (MovementLocked)
        {
            return Vector2.zero;
        }

        if (UiModalState.IsBlocking)
        {
            return Vector2.zero;
        }

        if (router != null)
        {
            return router.Value;
        }

        if (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen)
        {
            return Vector2.zero;
        }

        if (joystick != null && joystick.Value.sqrMagnitude > 0.04f)
        {
            return joystick.Value;
        }

        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    private void FixedUpdate()
    {
        Vector2 move = Vector2.ClampMagnitude(input, 1f) * moveSpeed;
        body.linearVelocity = move;
    }
}
