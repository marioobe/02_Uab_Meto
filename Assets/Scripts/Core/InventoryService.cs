using System;
using UnityEngine;

/// <summary>Persediaan buah generik. 1 buah = +1 hati.</summary>
public class InventoryService : MonoBehaviour
{
    public static InventoryService Instance { get; private set; }

    public event Action<int> Changed;

    public int Fruit => GameState.Ensure().Data.fruit;

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

    public void AddFruit(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        GameState state = GameState.Ensure();
        state.Data.fruit += amount;
        Changed?.Invoke(state.Data.fruit);
        state.NotifyChanged();
        SaveService.Instance?.RequestSave();
    }

    /// <summary>Mengembalikan false + alasan bila hati penuh / buah habis.</summary>
    public bool TryUseFruit(out string message)
    {
        GameState state = GameState.Ensure();

        // Cek buah habis lebih dahulu supaya pesannya tepat saat HUD kosong.
        if (state.Data.fruit <= 0)
        {
            message = "Buah habis. Cari buah di Kebun.";
            return false;
        }

        if (state.Data.hearts >= GameState.MaxHearts)
        {
            message = "Hati masih penuh. Buah belum bisa dipakai.";
            return false;
        }

        state.Data.fruit--;
        bool healed = HealthService.Instance != null && HealthService.Instance.Heal(1);
        if (!healed)
        {
            state.Data.fruit++;
            message = "Gagal memakai buah.";
            return false;
        }

        message = "Satu hati pulih.";
        Changed?.Invoke(state.Data.fruit);
        state.NotifyChanged();
        SaveService.Instance?.RequestSave();
        return true;
    }
}
