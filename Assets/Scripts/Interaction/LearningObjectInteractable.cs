using UnityEngine;

/// <summary>Objek belajar di scene. Membuka panel informasi TMP.</summary>
public class LearningObjectInteractable : MonoBehaviour, IInteractable
{
    public string objectId;
    public LearningDatabase database;

    public string PromptText => "Baca";
    public string PromptLabel => "Baca";

    public void Bind(LearningDatabase learningDatabase, string id)
    {
        database = learningDatabase;
        objectId = id;
    }

    public void Interact(PlayerInteractor interactor)
    {
        LearningObjectDefinition definition = database != null ? database.Find(objectId) : null;
        if (definition == null)
        {
            ToastView.Instance?.Show("Materi objek ini belum tersedia.");
            return;
        }

        if (GameUi.Instance != null)
        {
            GameUi.Instance.ShowInfo(definition);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.45f);
    }
}
