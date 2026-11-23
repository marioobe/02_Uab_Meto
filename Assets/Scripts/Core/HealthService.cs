using System;
using UnityEngine;

/// <summary>Hati hanya berkurang dari jawaban kuis yang salah.</summary>
public class HealthService : MonoBehaviour
{
    public static HealthService Instance { get; private set; }

    public event Action<int, int> Changed;

    public int Hearts => GameState.Ensure().Data.hearts;
    public int Max => GameState.MaxHearts;
    public bool IsEmpty => Hearts <= 0;
    public bool IsFull => Hearts >= Max;

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

    public bool LoseHeart(string reason)
    {
        GameState state = GameState.Ensure();
        if (state.Data.hearts <= 0)
        {
            return false;
        }

        state.Data.hearts--;
        Debug.Log($"Hati berkurang ({reason}). Sisa: {state.Data.hearts}");
        Changed?.Invoke(state.Data.hearts, Max);
        state.NotifyChanged();
        SaveService.Instance?.RequestSave();
        return true;
    }

    public bool Heal(int amount = 1)
    {
        GameState state = GameState.Ensure();
        if (state.Data.hearts >= Max || amount <= 0)
        {
            return false;
        }

        state.Data.hearts = Mathf.Min(Max, state.Data.hearts + amount);
        Changed?.Invoke(state.Data.hearts, Max);
        state.NotifyChanged();
        SaveService.Instance?.RequestSave();
        return true;
    }
}
