using UnityEngine;

/// <summary>NPC pemandu. Hanya satu karakter tambahan di Base_Map.</summary>
public class NpcInteractable : MonoBehaviour, IInteractable
{
    public string npcName = "Pemandu Desa";
    [TextArea(2, 6)] public string[] pages = new string[0];
    public bool markGuideTalked;

    public string PromptText => "Bicara";
    public string PromptLabel => "Bicara";

    public void Bind(string name, string[] dialoguePages, bool markGuide)
    {
        npcName = name;
        pages = dialoguePages;
        markGuideTalked = markGuide;
    }

    public void Interact(PlayerInteractor interactor)
    {
        string[] selected = DialogueLibrary.PagesFor(ProgressService.Instance);
        if (selected == null || selected.Length == 0)
        {
            selected = pages;
        }

        GameUi.Instance?.ShowDialogue(npcName, selected, () =>
        {
            if (markGuideTalked)
            {
                ProgressService.Instance?.MarkGuideTalked();
            }
        });
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.6f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.55f);
    }
}
