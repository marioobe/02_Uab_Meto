using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simpan lokal dengan versi, backup, karantina file rusak, dan pemulihan.
/// Lokasi: Application.persistentDataPath/save.json
/// </summary>
public class SaveService : MonoBehaviour
{
    public const int CurrentVersion = 5;

    public static SaveService Instance { get; private set; }

    private const string FileName = "save.json";
    private const string BackupName = "save.bak";

    private bool dirty;
    private float lastChangeAt;
    private float lastSaveAt;

    public string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    public string BackupPath => Path.Combine(Application.persistentDataPath, BackupName);

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

    public void Load()
    {
        Directory.CreateDirectory(Application.persistentDataPath);

        SaveData data = null;

        if (File.Exists(SavePath))
        {
            data = TryLoad(SavePath, out string error);
            if (data == null)
            {
                Debug.LogWarning($"Save utama rusak: {error}");
                Quarantine(SavePath);
            }
        }

        if (data == null && File.Exists(BackupPath))
        {
            data = TryLoad(BackupPath, out string backupError);
            if (data == null)
            {
                Debug.LogWarning($"Backup save rusak: {backupError}");
                Quarantine(BackupPath);
            }
            else
            {
                Debug.Log("Save dipulihkan dari backup.");
            }
        }

        if (data == null)
        {
            data = new SaveData();
        }

        GameState.Ensure().Apply(data);
        Debug.Log($"Save dimuat: scene={data.lastScene}, hati={data.hearts}, buah={data.fruit}, dapur={data.dapurCompleted}");
    }

    public void RequestSave()
    {
        dirty = true;
        lastChangeAt = Time.unscaledTime;
    }

    public void SaveNow()
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);

            SaveData data = GameState.Ensure().Snapshot();
            data.saveVersion = CurrentVersion;
            data.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            string tmpPath = SavePath + ".tmp";
            File.WriteAllText(tmpPath, json, new UTF8Encoding(false));

            if (File.Exists(SavePath) && TryLoad(SavePath, out _) != null)
            {
                File.Copy(SavePath, BackupPath, true);
            }

            try
            {
                if (File.Exists(SavePath))
                {
                    File.Replace(tmpPath, SavePath, null);
                }
                else
                {
                    File.Move(tmpPath, SavePath);
                }
            }
            catch (Exception replaceError)
            {
                Debug.LogWarning($"File.Replace gagal, memakai fallback: {replaceError.Message}");
                File.Copy(tmpPath, SavePath, true);
                File.Delete(tmpPath);
            }

            dirty = false;
            lastSaveAt = Time.unscaledTime;
        }
        catch (Exception error)
        {
            Debug.LogError($"Gagal menyimpan: {error}");
        }
    }

    public void ResetProgress()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }

            if (File.Exists(BackupPath))
            {
                File.Delete(BackupPath);
            }

            string tmp = SavePath + ".tmp";
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
        }
        catch (Exception error)
        {
            Debug.LogWarning($"Gagal menghapus save: {error.Message}");
        }

        GameState.Ensure().Apply(new SaveData());
        SettingsService.Instance?.ApplyFromState();
        SaveNow();
    }

    private SaveData TryLoad(string path, out string error)
    {
        error = null;

        try
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "file kosong";
                return null;
            }

            SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
            if (data == null)
            {
                error = "JSON null";
                return null;
            }

            if (data.saveVersion > CurrentVersion)
            {
                error = $"versi save {data.saveVersion} lebih baru dari {CurrentVersion}";
                return null;
            }

            if (data.saveVersion < CurrentVersion)
            {
                data = Migrate(data);
            }

            if (data.hearts < 0 || data.hearts > GameState.MaxHearts)
            {
                data.hearts = Mathf.Clamp(data.hearts, 0, GameState.MaxHearts);
            }

            if (data.fruit < 0)
            {
                data.fruit = 0;
            }

            data.learnedObjectIds ??= new System.Collections.Generic.List<string>();
            data.collectedPickupIds ??= new System.Collections.Generic.List<string>();
            data.completedTopicIds ??= new System.Collections.Generic.List<string>();
            EnsureV3Defaults(data);
            EnsureV4Defaults(data);
            EnsureV5Defaults(data);
            data.controlScheme = string.IsNullOrEmpty(data.controlScheme) ? "dpad" : data.controlScheme;
            return data;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return null;
        }
    }

    /// <summary>Tempat migrasi versi lama. v0 → v1 → v2 → v3 → v4 (aditif, data lama tidak diubah).</summary>
    private SaveData Migrate(SaveData data)
    {
        // v0 -> v1: field baru sudah punya nilai default di kelas SaveData.
        if (data.saveVersion < 1)
        {
            data.saveVersion = 1;
        }

        // v1 -> v2: hanya menambah daftar topik selesai (kosong); progres lama utuh.
        if (data.saveVersion < 2)
        {
            data.completedTopicIds ??= new System.Collections.Generic.List<string>();
            data.saveVersion = 2;
        }

        // v2 -> v3: gamifikasi; semua field lama dipertahankan apa adanya.
        if (data.saveVersion < 3)
        {
            EnsureV3Defaults(data);
            data.saveVersion = 3;
        }

        // v3 -> v4: hanya menambah daftar XP latihan menulis (kosong).
        if (data.saveVersion < 4)
        {
            EnsureV4Defaults(data);
            data.saveVersion = 4;
        }

        // v4 -> v5: progres lesson/item kurikulum (aditif, kosong).
        if (data.saveVersion < 5)
        {
            EnsureV5Defaults(data);
            data.saveVersion = 5;
        }

        return data;
    }

    /// <summary>Isi default field v3 tanpa menyentuh data lama (idempoten).</summary>
    private static void EnsureV3Defaults(SaveData data)
    {
        data.earnedBadges ??= new System.Collections.Generic.List<string>();
        data.bestScores ??= new System.Collections.Generic.Dictionary<string, int>();
        data.leaderboard ??= new System.Collections.Generic.List<QuizScoreEntry>();
        data.xpQuestions ??= new System.Collections.Generic.List<string>();
        data.xpPassScenes ??= new System.Collections.Generic.List<string>();
        data.lastActivityDay ??= string.Empty;
        data.xp = Mathf.Max(0, data.xp);
        data.level = Mathf.Max(1, data.level);
        data.streak = Mathf.Max(0, data.streak);
    }

    /// <summary>Isi default field v4 (latihan menulis) tanpa menyentuh data lama.</summary>
    private static void EnsureV4Defaults(SaveData data)
    {
        data.xpWritings ??= new System.Collections.Generic.List<string>();
    }

    /// <summary>Isi default field v5 (progres kurikulum) tanpa menyentuh data lama.</summary>
    private static void EnsureV5Defaults(SaveData data)
    {
        data.completedLessonIds ??= new System.Collections.Generic.List<string>();
        data.completedItemIds ??= new System.Collections.Generic.List<string>();
    }

    private void Quarantine(string path)
    {
        try
        {
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string target = Path.Combine(Application.persistentDataPath, $"save.corrupt-{stamp}.json");
            File.Copy(path, target, true);
            Debug.LogWarning($"File save rusak disimpan: {target}");
        }
        catch (Exception error)
        {
            Debug.LogWarning($"Gagal mengarantina save: {error.Message}");
        }
    }

    private void Update()
    {
        if (dirty && Time.unscaledTime - Mathf.Max(lastChangeAt, lastSaveAt) >= 0.35f)
        {
            SaveNow();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveNow();
        }
    }

    private void OnApplicationQuit()
    {
        SaveNow();
    }
}
