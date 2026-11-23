using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pengaturan: volume, skema kontrol, reset progres.</summary>
public class SettingsPanel : MonoBehaviour
{
    private GameObject root;
    private Slider musicSlider;
    private Slider sfxSlider;
    private TextMeshProUGUI schemeLabel;
    private TextMeshProUGUI resetLabel;
    private bool resetArmed;

    public void Build(GameObject panelRoot, Slider music, Slider sfx, TextMeshProUGUI scheme,
        TextMeshProUGUI reset, Button schemeButton, Button resetButton, Button closeButton)
    {
        root = panelRoot;
        musicSlider = music;
        sfxSlider = sfx;
        schemeLabel = scheme;
        resetLabel = reset;

        musicSlider.onValueChanged.AddListener(value => SettingsService.Instance?.SetMusicVolume(value));
        sfxSlider.onValueChanged.AddListener(value => SettingsService.Instance?.SetSfxVolume(value));
        schemeButton.onClick.AddListener(ToggleScheme);
        resetButton.onClick.AddListener(ResetPressed);
        closeButton.onClick.AddListener(Close);
        root.SetActive(false);
    }

    public void Show()
    {
        if (root == null)
        {
            return;
        }

        resetArmed = false;
        resetLabel.text = "Reset progres";
        musicSlider.SetValueWithoutNotify(SettingsService.Instance != null
            ? SettingsService.Instance.MusicVolume
            : 0.55f);
        sfxSlider.SetValueWithoutNotify(SettingsService.Instance != null
            ? SettingsService.Instance.SfxVolume
            : 0.8f);
        UpdateSchemeLabel();
        root.SetActive(true);
        UiModalState.Push(this);
    }

    public void Close()
    {
        if (root == null || !root.activeSelf)
        {
            return;
        }

        resetArmed = false;
        root.SetActive(false);
        UiModalState.Pop(this);
        SaveService.Instance?.SaveNow();
    }

    private void ToggleScheme()
    {
        SettingsService service = SettingsService.Instance;
        if (service == null)
        {
            return;
        }

        service.SetScheme(service.Scheme == ControlScheme.DPad
            ? ControlScheme.Joystick
            : ControlScheme.DPad);
        UpdateSchemeLabel();
        AudioManager.Instance?.PlaySfx(SfxId.Click);
    }

    private void UpdateSchemeLabel()
    {
        if (schemeLabel == null)
        {
            return;
        }

        ControlScheme scheme = SettingsService.Instance != null
            ? SettingsService.Instance.Scheme
            : ControlScheme.DPad;
        schemeLabel.text = scheme == ControlScheme.DPad
            ? "Kontrol: D-Pad (4 tombol)"
            : "Kontrol: Joystick analog (360°)";
    }

    private void ResetPressed()
    {
        if (!resetArmed)
        {
            resetArmed = true;
            resetLabel.text = "Yakin? Tekan lagi untuk reset";
            return;
        }

        resetArmed = false;
        SaveService.Instance?.ResetProgress();
        Close();
        SceneRouter.Instance?.ReturnToBaseMap("Start");
    }

    private void OnDestroy()
    {
        UiModalState.Pop(this);
    }
}
