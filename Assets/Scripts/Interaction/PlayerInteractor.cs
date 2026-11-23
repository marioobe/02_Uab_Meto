using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mencari interaksi terdekat. Mendukung sistem baru (IInteractable) dan
/// sistem lama (Interactable) agar scene backup tetap kompatibel.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Deteksi")]
    [SerializeField] private float detectRadius = 1.25f;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI lama")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private Text promptLabel;

    [Header("UI baru")]
    [SerializeField] private TMP_Text promptTmp;
    [SerializeField] private TMP_Text actionTmp;

    private Interactable currentLegacy;
    private IInteractable currentNew;
    private bool dialogueWasOpen;
    private bool wasBlocked;
    private float interactLockUntil;

    public Interactable Current => currentLegacy;
    public IInteractable CurrentNew => currentNew;

    public void Bind(GameObject prompt, Text label)
    {
        promptRoot = prompt;
        promptLabel = label;
        RefreshPrompt();
    }

    public void BindTmp(GameObject prompt, TMP_Text label, TMP_Text actionLabel)
    {
        promptRoot = prompt;
        promptTmp = label;
        actionTmp = actionLabel;
        RefreshPrompt();
    }

    private void Start()
    {
        RefreshPrompt();
    }

    private void Update()
    {
        bool blocked = UiModalState.IsBlocking ||
                       (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen);

        if (blocked && !wasBlocked)
        {
            interactLockUntil = Time.unscaledTime + 0.15f;
        }

        wasBlocked = blocked;

        if (blocked)
        {
            if (currentLegacy != null || currentNew != null)
            {
                currentLegacy = null;
                currentNew = null;
            }

            RefreshPrompt();
            return;
        }

        FindNearest();

        bool justClosed = DialogueUI.Instance != null && DialogueUI.Instance.JustClosed;
        bool canInteract = Time.unscaledTime >= interactLockUntil &&
                           GameState.Ensure().Data.hearts >= 0;

        if (!justClosed && canInteract && Input.GetKeyDown(interactKey))
        {
            InteractWithCurrent();
        }

        RefreshPrompt();
    }

    /// <summary>Dipanggil tombol interaksi di layar.</summary>
    public void InteractWithCurrent()
    {
        if (UiModalState.IsBlocking ||
            (SceneRouter.Instance != null && SceneRouter.Instance.IsTransitioning))
        {
            return;
        }

        if (currentNew == null && currentLegacy == null)
        {
            return;
        }

        interactLockUntil = Time.unscaledTime + 0.25f;

        if (currentNew != null)
        {
            currentNew.Interact(this);
        }
        else
        {
            currentLegacy.Interact(this);
        }
    }

    private void FindNearest()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectRadius);
        IInteractable bestNew = null;
        Interactable bestLegacy = null;
        float bestSqrDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            IInteractable candidateNew = hit.GetComponentInParent<IInteractable>();
            if (candidateNew != null)
            {
                float sqr = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqr < bestSqrDistance)
                {
                    bestSqrDistance = sqr;
                    bestNew = candidateNew;
                    bestLegacy = null;
                }

                continue;
            }

            Interactable candidateLegacy = hit.GetComponentInParent<Interactable>();
            if (candidateLegacy == null)
            {
                continue;
            }

            float legacySqr = ((Vector2)candidateLegacy.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (legacySqr < bestSqrDistance)
            {
                bestSqrDistance = legacySqr;
                bestLegacy = candidateLegacy;
                bestNew = null;
            }
        }

        currentNew = bestNew;
        currentLegacy = bestLegacy;
    }

    private void RefreshPrompt()
    {
        bool blocked = UiModalState.IsBlocking ||
                       (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen);
        bool hasTarget = currentNew != null || currentLegacy != null;

        if (promptRoot != null)
        {
            promptRoot.SetActive(hasTarget && !blocked);
        }

        if (promptLabel != null)
        {
            promptLabel.text = currentLegacy != null ? currentLegacy.PromptText : string.Empty;
        }

        if (promptTmp != null)
        {
            promptTmp.text = currentNew != null ? currentNew.PromptText :
                currentLegacy != null ? currentLegacy.PromptText : string.Empty;
        }

        if (actionTmp != null)
        {
            actionTmp.text = currentNew != null ? currentNew.PromptLabel :
                currentLegacy != null ? "Baca" : "Baca";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
