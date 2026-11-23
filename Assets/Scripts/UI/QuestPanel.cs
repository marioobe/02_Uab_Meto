using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Indikator status lokasi/bab.</summary>
public class QuestPanel : MonoBehaviour
{
    private GameObject root;
    private TextMeshProUGUI body;

    public void Build(GameObject panelRoot, TextMeshProUGUI text, Button closeButton)
    {
        root = panelRoot;
        body = text;
        closeButton.onClick.AddListener(Close);
        root.SetActive(false);
    }

    public void Show()
    {
        if (root == null)
        {
            return;
        }

        Refresh();
        root.SetActive(true);
        UiModalState.Push(this);
    }

    public void Refresh()
    {
        if (body == null)
        {
            return;
        }

        ProgressService progress = ProgressService.Instance;
        string dapur = progress != null ? progress.StatusFor("Dapur") : "Sedang dipelajari";
        string rumah = progress != null ? progress.StatusFor("Rumah") : "Terkunci";
        string kebun = progress != null ? progress.StatusFor("Kebun") : "Terbuka";

        string chapter = "BAB 1 — Mengenal kampung";
        if (progress != null)
        {
            if (!progress.DapurCompleted)
            {
                chapter = "BAB 2 — Belajar di Dapur Bulat";
            }
            else if (!progress.RumahCompleted)
            {
                chapter = "BAB 3 — Menjelajahi Rumah Warga";
            }
            else
            {
                chapter = "BAB 4 — Menjelajahi Kebun";
            }
        }

        body.text =
            $"<b>{chapter}</b>\n\n" +
            $"Dapur Bulat : {dapur}\n" +
            $"Rumah Warga : {rumah}\n" +
            $"Kebun       : {kebun}\n\n" +
            "Kebun selalu terbuka. Gunakan buah untuk memulihkan hati.";
    }

    public void Close()
    {
        if (root == null || !root.activeSelf)
        {
            return;
        }

        root.SetActive(false);
        UiModalState.Pop(this);
    }

    private void OnDestroy()
    {
        UiModalState.Pop(this);
    }
}
