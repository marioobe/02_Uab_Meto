using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Titik interaksi pintu gubuk buku. Kalau interior siap, pemain masuk ke scene
/// gubuk; kalau belum, materi ditampilkan langsung seperti sebelumnya.
/// </summary>
public class RoundHouseInteraction : Interactable
{
    [Header("Interior")]
    [SerializeField] private string interiorSceneName = "RoundHouseInterior";
    [SerializeField] private bool interiorReady;

    [Header("Materi")]
    [SerializeField] private int spotIndex;

    public void BindInterior(string sceneName, bool ready = false)
    {
        interiorSceneName = sceneName;
        interiorReady = ready;
    }

    public void BindSpot(int index)
    {
        spotIndex = index;
        HutMaterial material = HutMaterials.Get(spotIndex);
        Bind(material.Title, material.Body, "Tekan E / Baca untuk masuk");
    }

    public override void Interact(PlayerInteractor interactor)
    {
        if (interiorReady && !string.IsNullOrEmpty(interiorSceneName))
        {
            GameSession.PendingHutIndex = spotIndex;

            // Kembali ke posisi berdiri pemain saat masuk: posisi itu pasti bebas
            // collider (offset tetap dari pintu bisa jatuh di celah pagar/bedeng).
            GameSession.VillageReturnPosition = interactor != null
                ? (Vector2)interactor.transform.position
                : (Vector2)transform.position + new Vector2(0f, -0.35f);
            GameSession.HasReturnPosition = true;
            SceneManager.LoadScene(interiorSceneName);
            return;
        }

        HutMaterial material = HutMaterials.Get(spotIndex);
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
