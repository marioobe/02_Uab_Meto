using System;
using UnityEngine;

/// <summary>Status bab dan kunci lokasi. Kebun selalu terbuka.</summary>
public class ProgressService : MonoBehaviour
{
    public static ProgressService Instance { get; private set; }

    public event Action Changed;

    public bool DapurCompleted => GameState.Ensure().Data.dapurCompleted;
    public bool RumahUnlocked => GameState.Ensure().Data.rumahUnlocked;
    public bool RumahCompleted => GameState.Ensure().Data.rumahCompleted;
    public bool KebunCompleted => GameState.Ensure().Data.kebunCompleted;
    public bool GuideTalked => GameState.Ensure().Data.guideTalked;

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

    public void MarkGuideTalked()
    {
        if (GameState.Ensure().Data.guideTalked)
        {
            return;
        }

        GameState.Ensure().Data.guideTalked = true;
        Changed?.Invoke();
        SaveService.Instance?.RequestSave();
    }

    public void CompleteScene(string sceneKey)
    {
        GameState state = GameState.Ensure();
        bool changed = false;

        switch (sceneKey)
        {
            case "Dapur":
                if (!state.Data.dapurCompleted)
                {
                    state.Data.dapurCompleted = true;
                    changed = true;
                }

                if (!state.Data.rumahUnlocked)
                {
                    state.Data.rumahUnlocked = true;
                    changed = true;
                }

                break;
            case "Rumah":
                if (!state.Data.rumahCompleted)
                {
                    state.Data.rumahCompleted = true;
                    changed = true;
                }

                break;
            case "Kebun":
                if (!state.Data.kebunCompleted)
                {
                    state.Data.kebunCompleted = true;
                    changed = true;
                }

                break;
        }

        if (changed)
        {
            Changed?.Invoke();
            state.NotifyChanged();
            SaveService.Instance?.RequestSave();
        }
    }

    public string StatusFor(string sceneKey)
    {
        switch (sceneKey)
        {
            case "Dapur":
                return DapurCompleted ? "Selesai" : "Sedang dipelajari";
            case "Rumah":
                if (!RumahUnlocked)
                {
                    return "Terkunci";
                }

                return RumahCompleted ? "Selesai" : "Sedang dipelajari";
            case "Kebun":
                return KebunCompleted ? "Selesai" : "Terbuka";
            default:
                return "Terbuka";
        }
    }
}
