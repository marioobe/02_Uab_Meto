using UnityEngine;

public enum DoorLock
{
    None = 0,
    AfterDapur = 1
}

/// <summary>Pintu scene dengan kunci progres. Tidak pernah memanggil SceneManager langsung.</summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    public string targetScene;
    public string entryId = "Start";
    public DoorLock lockKind = DoorLock.None;
    [TextArea(2, 4)] public string lockedMessage = "Selesaikan pembelajaran di Dapur dahulu.";
    public string promptText = "Masuk";

    public string PromptText => promptText;
    public string PromptLabel => promptText;

    public void Bind(string scene, string entry, DoorLock doorLock = DoorLock.None, string message = null, string prompt = null)
    {
        targetScene = scene;
        entryId = entry;
        lockKind = doorLock;
        if (!string.IsNullOrEmpty(message))
        {
            lockedMessage = message;
        }
        if (!string.IsNullOrEmpty(prompt))
        {
            promptText = prompt;
        }
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (lockKind == DoorLock.AfterDapur &&
            (ProgressService.Instance == null || !ProgressService.Instance.RumahUnlocked))
        {
            AudioManager.Instance?.PlaySfx(SfxId.Wrong);
            GameUi.Instance?.ShowMessage("Terkunci", lockedMessage);
            return;
        }

        AudioManager.Instance?.PlaySfx(SfxId.Door);
        SceneRouter.Instance?.TransitionTo(targetScene, entryId);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1.1f, 0.8f, 0.1f));
    }
}
