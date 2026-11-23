using System;
using UnityEngine;

public enum ControlScheme
{
    DPad = 0,
    Joystick = 1
}

/// <summary>Preferensi kontrol dan audio. Disimpan bersama save.</summary>
public class SettingsService : MonoBehaviour
{
    public static SettingsService Instance { get; private set; }

    public event Action SchemeChanged;

    public ControlScheme Scheme
    {
        get
        {
            string value = GameState.Ensure().Data.controlScheme;
            return value == "joystick" ? ControlScheme.Joystick : ControlScheme.DPad;
        }
    }

    public float MusicVolume => GameState.Ensure().Data.musicVolume;
    public float SfxVolume => GameState.Ensure().Data.sfxVolume;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ApplyFromState()
    {
        AudioManager.Instance?.ApplyVolumes(MusicVolume, SfxVolume);
        SchemeChanged?.Invoke();
    }

    public void SetScheme(ControlScheme scheme)
    {
        GameState.Ensure().Data.controlScheme = scheme == ControlScheme.Joystick ? "joystick" : "dpad";
        SaveService.Instance?.RequestSave();
        SchemeChanged?.Invoke();
    }

    public void SetMusicVolume(float value)
    {
        GameState.Ensure().Data.musicVolume = Mathf.Clamp01(value);
        AudioManager.Instance?.ApplyVolumes(MusicVolume, SfxVolume);
        SaveService.Instance?.RequestSave();
    }

    public void SetSfxVolume(float value)
    {
        GameState.Ensure().Data.sfxVolume = Mathf.Clamp01(value);
        AudioManager.Instance?.ApplyVolumes(MusicVolume, SfxVolume);
        SaveService.Instance?.RequestSave();
    }
}
