using System;
using UnityEngine;

/// <summary>
/// Satu-satunya sumber data runtime. SaveService mengisi/mengambil snapshot dari sini.
/// Script lama (GameSession) tetap ada hanya untuk scene backup.
/// </summary>
public class GameState
{
    public const int MaxHearts = 3;

    public static GameState Current { get; private set; }

    public static event Action Changed;

    public SaveData Data { get; private set; } = new SaveData();
    public bool IsLoaded { get; private set; }

    public static GameState Ensure()
    {
        if (Current == null)
        {
            Current = new GameState();
        }

        return Current;
    }

    public void Apply(SaveData data)
    {
        Data = data ?? new SaveData();
        Data.hearts = Mathf.Clamp(Data.hearts, 0, MaxHearts);
        Data.fruit = Mathf.Max(0, Data.fruit);
        Data.learnedObjectIds ??= new System.Collections.Generic.List<string>();
        Data.collectedPickupIds ??= new System.Collections.Generic.List<string>();
        Data.completedTopicIds ??= new System.Collections.Generic.List<string>();
        Data.earnedBadges ??= new System.Collections.Generic.List<string>();
        Data.bestScores ??= new System.Collections.Generic.Dictionary<string, int>();
        Data.leaderboard ??= new System.Collections.Generic.List<QuizScoreEntry>();
        Data.xpQuestions ??= new System.Collections.Generic.List<string>();
        Data.xpPassScenes ??= new System.Collections.Generic.List<string>();
        Data.xpWritings ??= new System.Collections.Generic.List<string>();
        Data.completedLessonIds ??= new System.Collections.Generic.List<string>();
        Data.completedItemIds ??= new System.Collections.Generic.List<string>();
        Data.lastActivityDay ??= string.Empty;
        Data.xp = Mathf.Max(0, Data.xp);
        Data.level = Mathf.Max(1, Data.level);
        Data.streak = Mathf.Max(0, Data.streak);
        if (string.IsNullOrEmpty(Data.controlScheme))
        {
            Data.controlScheme = "dpad";
        }

        IsLoaded = true;
        NotifyChanged();
    }

    public SaveData Snapshot()
    {
        Data.hearts = Mathf.Clamp(Data.hearts, 0, MaxHearts);
        Data.fruit = Mathf.Max(0, Data.fruit);
        return Data;
    }

    public void NotifyChanged()
    {
        Changed?.Invoke();
    }

    public bool IsLearned(string id)
    {
        return !string.IsNullOrEmpty(id) && Data.learnedObjectIds.Contains(id);
    }

    public bool MarkLearned(string id)
    {
        if (string.IsNullOrEmpty(id) || Data.learnedObjectIds.Contains(id))
        {
            return false;
        }

        Data.learnedObjectIds.Add(id);
        NotifyChanged();
        return true;
    }

    public bool IsCollected(string id)
    {
        return !string.IsNullOrEmpty(id) && Data.collectedPickupIds.Contains(id);
    }

    public bool MarkCollected(string id)
    {
        if (string.IsNullOrEmpty(id) || Data.collectedPickupIds.Contains(id))
        {
            return false;
        }

        Data.collectedPickupIds.Add(id);
        NotifyChanged();
        return true;
    }
}
