using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gamifikasi dasar (Fase B): XP, level, streak, badge, dan leaderboard lokal.
///
/// Semua XP dijaga anti-eksploit:
/// - baca objek: hanya saat MarkLearned() pertama kali (learnedObjectIds).
/// - jawaban benar: hanya sekali per soal, memakai identitas soal stabil.
/// - bonus lulus: sekali per scene (xpPassScenes).
/// - buah: sekali per buah biasa (collectedPickupIds); buah darurat tidak memberi XP.
/// - badge: sekali per badge (earnedBadges).
/// </summary>
public static class GamificationService
{
    public const int XpPerLevel = 100;
    public const int XpRead = 10;
    public const int XpCorrect = 5;
    public const int XpPass = 30;
    public const int XpFruit = 5;
    public const int XpBadge = 15;
    public const int XpWriting = 5;

    private const string DateFormat = "yyyy-MM-dd";

    // ---------------------------------------------------------------- XP / level

    public static int LevelFor(int xp)
    {
        return Mathf.Max(0, xp) / XpPerLevel + 1;
    }

    public static int XpIntoLevel(int xp)
    {
        return Mathf.Max(0, xp) % XpPerLevel;
    }

    /// <summary>Tambah XP + catat aktivitas harian. Dipanggil hanya oleh pemicu yang terjaga.</summary>
    public static void AddXp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        GameState state = GameState.Ensure();
        int oldLevel = state.Data.level;
        state.Data.xp = Mathf.Max(0, state.Data.xp + amount);
        state.Data.level = LevelFor(state.Data.xp);

        RegisterActivity();
        SaveAndNotify();

        if (state.Data.level > oldLevel)
        {
            ToastView.Instance?.Show($"Naik ke Level {state.Data.level}!");
        }
    }

    // ---------------------------------------------------------------- pemicu

    /// <summary>Dipanggil hanya ketika objek baru pertama kali ditandai dipelajari.</summary>
    public static void OnObjectRead(LearningObjectDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        AddXp(XpRead);

        // Badge "baca_<scene>" ketika semua objek scene sudah dibaca.
        if (GameUi.Instance != null && GameUi.Instance.Scene != null &&
            GameUi.Instance.Scene.AllObjectsLearned())
        {
            AwardBadge("baca_" + SceneCode(definition.sceneKey));
        }
    }

    /// <summary>XP untuk satu soal benar. Mengembalikan false bila soal itu sudah pernah diberi XP.</summary>
    public static bool OnQuizAnswer(LearningObjectDefinition definition, QuizQuestion question)
    {
        if (definition == null || question == null)
        {
            return false;
        }

        GameState state = GameState.Ensure();
        string key = QuestionKey(definition, question);
        if (state.Data.xpQuestions.Contains(key))
        {
            return false;
        }

        state.Data.xpQuestions.Add(key);
        AddXp(XpCorrect);
        return true;
    }

    /// <summary>Dipanggil sekali setiap kuis lulus (skor >= 70).</summary>
    public static void OnQuizPassed(string sceneKey, int score, int correct, int total)
    {
        if (string.IsNullOrEmpty(sceneKey))
        {
            return;
        }

        GameState state = GameState.Ensure();

        // Skor terbaik per scene (terpisah dari leaderboard).
        if (!state.Data.bestScores.TryGetValue(sceneKey, out int best) || score > best)
        {
            state.Data.bestScores[sceneKey] = score;
        }

        // Leaderboard lokal: maksimum 20, urut skor tertinggi.
        state.Data.leaderboard.Add(new QuizScoreEntry
        {
            sceneKey = sceneKey,
            score = score,
            correct = correct,
            total = total,
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        });
        state.Data.leaderboard.Sort((a, b) =>
        {
            if (a == null || b == null)
            {
                return 0;
            }

            if (a.score != b.score)
            {
                return b.score.CompareTo(a.score);
            }

            return string.CompareOrdinal(a.date, b.date);
        });
        if (state.Data.leaderboard.Count > 20)
        {
            state.Data.leaderboard.RemoveRange(20, state.Data.leaderboard.Count - 20);
        }

        // Bonus kelulusan hanya sekali per scene.
        if (!state.Data.xpPassScenes.Contains(sceneKey))
        {
            state.Data.xpPassScenes.Add(sceneKey);
            AddXp(XpPass);
        }

        AwardBadge("lulus_" + SceneCode(sceneKey));
        if (score >= 100)
        {
            AwardBadge("kuis_sempurna");
        }

        RegisterActivity();
        SaveAndNotify();
    }

    /// <summary>Dipanggil hanya ketika buah biasa berhasil dikumpulkan (bukan buah darurat).</summary>
    public static void OnFruitCollected(string pickupId)
    {
        AddXp(XpFruit);

        GameState state = GameState.Ensure();
        int collected = 0;
        for (int i = 1; i <= 5; i++)
        {
            if (state.Data.collectedPickupIds.Contains("kebun_buah_" + i))
            {
                collected++;
            }
        }

        if (collected >= 5)
        {
            AwardBadge("kolektor_buah");
        }
    }

    /// <summary>
    /// XP latihan menulis: hanya untuk jawaban benar, sekali per identitas latihan.
    /// Mengembalikan true bila XP baru diberikan.
    /// </summary>
    public static bool OnWritingSolved(string exerciseKey)
    {
        if (string.IsNullOrEmpty(exerciseKey))
        {
            return false;
        }

        GameState state = GameState.Ensure();
        if (state.Data.xpWritings.Contains(exerciseKey))
        {
            return false;
        }

        state.Data.xpWritings.Add(exerciseKey);
        AddXp(XpWriting);
        return true;
    }

    // ---------------------------------------------------------------- streak

    /// <summary>Tanggal lokal perangkat. Hari sama tidak menambah streak.</summary>
    public static void RegisterActivity()
    {
        GameState state = GameState.Ensure();
        string today = DateTime.Now.ToString(DateFormat);
        if (state.Data.lastActivityDay == today)
        {
            return;
        }

        bool isYesterday = false;
        if (DateTime.TryParseExact(state.Data.lastActivityDay, DateFormat,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime last))
        {
            isYesterday = (DateTime.Today - last.Date).TotalDays == 1d;
        }

        state.Data.streak = isYesterday ? state.Data.streak + 1 : 1;
        state.Data.lastActivityDay = today;

        if (state.Data.streak >= 3)
        {
            AwardBadge("streak_3");
        }

        SaveAndNotify();
    }

    // ---------------------------------------------------------------- badge

    /// <summary>Berikan badge sekali saja. XP badge juga hanya sekali.</summary>
    public static bool AwardBadge(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        GameState state = GameState.Ensure();
        if (state.Data.earnedBadges.Contains(id))
        {
            return false;
        }

        state.Data.earnedBadges.Add(id);
        SaveAndNotify();
        ToastView.Instance?.Show("Badge: " + BadgeCatalog.NameOf(id));
        AddXp(XpBadge);
        return true;
    }

    // ---------------------------------------------------------------- soal

    /// <summary>
    /// Identitas soal stabil: bukan indeks, tetapi hash dari id objek + teks soal +
    /// opsi + jawaban benar. Perubahan urutan soal tidak mengubah kunci.
    /// </summary>
    public static string QuestionKey(LearningObjectDefinition definition, QuizQuestion question)
    {
        if (definition == null || question == null)
        {
            return string.Empty;
        }

        string options = question.options != null ? string.Join("\u00a6", question.options) : string.Empty;
        string payload = definition.id + "\u00a7" + question.question + "\u00a7" + options + "\u00a7" +
                         question.correctIndex;
        return definition.id + "#" + Fnv1a64(payload);
    }

    private static string Fnv1a64(string value)
    {
        ulong hash = 14695981039346656037UL;
        for (int i = 0; i < value.Length; i++)
        {
            hash ^= value[i];
            hash *= 1099511628211UL;
        }

        return hash.ToString("x16");
    }

    /// <summary>Hash stabil untuk identitas latihan/soal (FNV-1a 64-bit, 16 hex).</summary>
    public static string Hash64(string value)
    {
        return Fnv1a64(value ?? string.Empty);
    }

    // ---------------------------------------------------------------- util

    public static string SceneCode(string sceneKey)
    {
        if (string.IsNullOrEmpty(sceneKey))
        {
            return "kampung";
        }

        switch (sceneKey)
        {
            case "Dapur":
                return "dapur";
            case "Rumah":
                return "rumah";
            case "Kebun":
                return "kebun";
            default:
                return sceneKey.ToLowerInvariant();
        }
    }

    private static void SaveAndNotify()
    {
        GameState.Ensure().NotifyChanged();
        SaveService.Instance?.RequestSave();
    }
}

/// <summary>Daftar 9 badge gamifikasi dasar.</summary>
public static class BadgeCatalog
{
    public sealed class Entry
    {
        public string Id;
        public string Nama;
        public string Deskripsi;
    }

    public static readonly Entry[] All =
    {
        new Entry { Id = "baca_dapur", Nama = "Pembaca Dapur", Deskripsi = "Baca semua objek Dapur." },
        new Entry { Id = "baca_rumah", Nama = "Pembaca Rumah", Deskripsi = "Baca semua objek Rumah." },
        new Entry { Id = "baca_kebun", Nama = "Pembaca Kebun", Deskripsi = "Baca semua objek Kebun." },
        new Entry { Id = "lulus_dapur", Nama = "Lulus Dapur", Deskripsi = "Lulus kuis Dapur." },
        new Entry { Id = "lulus_rumah", Nama = "Lulus Rumah", Deskripsi = "Lulus kuis Rumah." },
        new Entry { Id = "lulus_kebun", Nama = "Lulus Kebun", Deskripsi = "Lulus kuis Kebun." },
        new Entry { Id = "kuis_sempurna", Nama = "Sempurna", Deskripsi = "Dapat nilai 100 pada kuis." },
        new Entry { Id = "kolektor_buah", Nama = "Kolektor Buah", Deskripsi = "Kumpulkan lima buah di Kebun." },
        new Entry { Id = "streak_3", Nama = "Rajin Belajar", Deskripsi = "Belajar tiga hari berturut-turut." }
    };

    public static string NameOf(string id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
            {
                return All[i].Nama;
            }
        }

        return id;
    }

    public static string DescriptionOf(string id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
            {
                return All[i].Deskripsi;
            }
        }

        return string.Empty;
    }

    public static bool Contains(string id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
            {
                return true;
            }
        }

        return false;
    }
}
