using UnityEngine;

/// <summary>Titik spawn saat masuk/keluar scene.</summary>
public class EntryPoint : MonoBehaviour
{
    public string id = "Start";

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawRay(transform.position, Vector3.up * 0.8f);
    }
}
