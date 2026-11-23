using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pintu keluar di dalam gubuk; kembali ke desa di depan pintu gubuk tadi.
/// </summary>
public class InteriorExit : Interactable
{
    public override void Interact(PlayerInteractor interactor)
    {
        SceneManager.LoadScene("VillageScene");
    }
}
