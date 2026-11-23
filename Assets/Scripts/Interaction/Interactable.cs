using UnityEngine;

/// <summary>
/// Anything the player can interact with (round houses, NPCs, sign posts).
/// Register a trigger collider on the same object so PlayerInteractor can find it.
/// </summary>
public class Interactable : MonoBehaviour
{
    [Header("Konten")]
    [SerializeField] private string title = "Interaksi";
    [SerializeField, TextArea(3, 8)] private string body = "Teks belum diisi.";
    [SerializeField] private string promptText = "Tekan E";

    public string Title => title;
    public string Body => body;
    public string PromptText => promptText;

    public void Bind(string newTitle, string newBody, string newPrompt = "Tekan E")
    {
        title = newTitle;
        body = newBody;
        promptText = newPrompt;
    }

    public virtual void Interact(PlayerInteractor interactor)
    {
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.Show(title, body);
        }
        else
        {
            Debug.Log($"{title}: {body}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawCube(transform.position, Vector3.one * 0.6f);
    }
}
