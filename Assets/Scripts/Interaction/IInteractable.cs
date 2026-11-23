using UnityEngine;

/// <summary>Kontrak interaksi baru. Implementasi lama (Interactable) tetap didukung.</summary>
public interface IInteractable
{
    string PromptText { get; }
    string PromptLabel { get; }
    void Interact(PlayerInteractor interactor);
}
