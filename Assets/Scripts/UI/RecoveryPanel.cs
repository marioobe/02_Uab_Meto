using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Panel saat hati habis. Tidak boleh mengunci pemain dari Kebun.</summary>
public class RecoveryPanel : MonoBehaviour
{
    private GameObject root;
    private TextMeshProUGUI body;
    private Button useFruitButton;
    private Button goKebunButton;

    public void Build(GameObject panelRoot, TextMeshProUGUI text, Button useFruit, Button goKebun,
        Button close)
    {
        root = panelRoot;
        body = text;
        useFruitButton = useFruit;
        goKebunButton = goKebun;

        useFruitButton.onClick.AddListener(UseFruit);
        goKebunButton.onClick.AddListener(GoKebun);
        close.onClick.AddListener(Close);
        root.SetActive(false);
    }

    public void Show()
    {
        if (root == null)
        {
            return;
        }

        if (root.activeSelf)
        {
            Refresh();
            return;
        }

        Refresh();
        root.SetActive(true);
        UiModalState.Push(this);
    }

    private void Refresh()
    {
        int hearts = HealthService.Instance != null ? HealthService.Instance.Hearts : 0;
        int fruit = InventoryService.Instance != null ? InventoryService.Instance.Fruit : 0;
        bool inKebun = SceneController.Instance != null && SceneController.Instance.sceneKey == "Kebun";

        body.text = hearts > 0
            ? "Hati sudah pulih. Lanjutkan petualangan."
            : fruit > 0
                ? $"Hati habis. Kamu punya {fruit} buah. Pakai satu untuk +1 hati."
                : inKebun
                    ? "Hati habis dan buah kosong. Cari keranjang buah darurat di Kebun."
                    : "Hati habis dan buah kosong. Pergi ke Kebun untuk mencari buah.";

        useFruitButton.interactable = fruit > 0 && hearts < GameState.MaxHearts;
        goKebunButton.interactable = !inKebun;
    }

    private void UseFruit()
    {
        if (InventoryService.Instance == null)
        {
            return;
        }

        if (InventoryService.Instance.TryUseFruit(out string message))
        {
            AudioManager.Instance?.PlaySfx(SfxId.Heart);
        }

        ToastView.Instance?.Show(message);
        Refresh();

        if (HealthService.Instance != null && HealthService.Instance.Hearts > 0)
        {
            Close();
        }
    }

    private void GoKebun()
    {
        Close();
        SceneRouter.Instance?.TransitionTo("Kebun", "Start");
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
