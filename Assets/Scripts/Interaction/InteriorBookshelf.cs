using UnityEngine;

/// <summary>
/// Rak buku di dalam gubuk. Menampilkan materi gubuk yang sedang dimasuki.
/// </summary>
public class InteriorBookshelf : Interactable
{
    public override void Interact(PlayerInteractor interactor)
    {
        HutMaterial material = HutMaterials.Get(GameSession.PendingHutIndex);

        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.Show(material.Title, material.Body);
        }
        else
        {
            Debug.Log($"{material.Title}: {material.Body}");
        }
    }
}
