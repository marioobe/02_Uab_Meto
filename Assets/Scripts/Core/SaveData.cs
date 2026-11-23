using System;
using System.Collections.Generic;

/// <summary>Data yang disimpan ke disk. Jangan ganti nama field tanpa migrasi.</summary>
[Serializable]
public class SaveData
{
    public int saveVersion = 1;

    public string lastScene = "Base_Map";
    public string lastEntryId = "Start";

    public int hearts = GameState.MaxHearts;
    public int fruit;

    public bool guideTalked;
    public bool dapurCompleted;
    public bool rumahUnlocked;
    public bool rumahCompleted;
    public bool kebunCompleted;
    public bool kebunAdvancedCompleted;

    public List<string> learnedObjectIds = new List<string>();
    public List<string> collectedPickupIds = new List<string>();

    // v2: disiapkan untuk aturan penyelesaian topik (belum dipakai di Fase A).
    public List<string> completedTopicIds = new List<string>();

    // v3: gamifikasi dasar + gerbang anti-eksploit XP (aditif).
    public int xp;
    public int level = 1;
    public int streak;
    public string lastActivityDay = string.Empty;
    public List<string> earnedBadges = new List<string>();
    public Dictionary<string, int> bestScores = new Dictionary<string, int>();
    public List<QuizScoreEntry> leaderboard = new List<QuizScoreEntry>();
    public List<string> xpQuestions = new List<string>();
    public List<string> xpPassScenes = new List<string>();

    // v4: XP latihan menulis (sekali per latihan unik).
    public List<string> xpWritings = new List<string>();

    // v5: progres kurikulum per lesson/item (aditif).
    public List<string> completedLessonIds = new List<string>();
    public List<string> completedItemIds = new List<string>();

    public float musicVolume = 0.55f;
    public float sfxVolume = 0.8f;
    public string controlScheme = "dpad";

    public long lastRescuePickupUnix;
    public long savedAtUnix;
}

/// <summary>Satu catatan skor kuis untuk leaderboard lokal.</summary>
[Serializable]
public class QuizScoreEntry
{
    public string sceneKey;
    public int score;
    public int correct;
    public int total;
    public string date;
}
