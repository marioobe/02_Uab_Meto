using UnityEngine;

/// <summary>
/// Guide NPC that greets the player at the village entrance.
/// Extends Interactable so the same prompt/dialogue flow applies.
/// </summary>
public class NPCGuide : Interactable
{
    [SerializeField] private string npcName = "Pemandu Desa";

    public void BindNpc(string name)
    {
        npcName = name;
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.Show(npcName, Body);
        }
        else
        {
            Debug.Log($"{npcName}: {Body}");
        }
    }
}
