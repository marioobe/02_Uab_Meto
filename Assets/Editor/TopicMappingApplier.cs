using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fase A — menerapkan metadata topik ke 32 aset LearningObjectDefinition.
///
/// HANYA menulis 4 field: topicId, role, crossTopics, order.
/// Tidak menyentuh id, nama Indonesia, nama Dawan, cara baca, status validasi,
/// validator, catatan, penjelasan, gambar, audio, maupun questions.
///
/// Alur: verifikasi 32 aset -> backup folder -> snapshot konten -> tulis metadata
/// -> diff sebelum/sesudah -> rollback otomatis bila ada konten berubah.
/// Tidak menjalankan LearningDataBuilder.BuildAll.
/// </summary>
public static class TopicMappingApplier
{
    private const string DataFolder = "Assets/Data/Learning";
    private const string BackupRoot = "Backups";

    [MenuItem("UAB Meto/0a - Apply Topic Mapping (Fase A)", priority = 1)]
    public static void Apply()
    {
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string backupDir = Path.Combine(projectRoot, BackupRoot, "FaseA_Mapping_" + stamp);
        string dataFolderFull = Path.Combine(Application.dataPath, "Data", "Learning");

        Debug.Log($"[FaseA] Mulai. Backup -> {backupDir}");

        // ---------------------------------------------------------- 1. kumpulkan aset
        Dictionary<string, LearningObjectDefinition> byId =
            new Dictionary<string, LearningObjectDefinition>();
        Dictionary<string, string> pathById = new Dictionary<string, string>();

        string[] guids = AssetDatabase.FindAssets("t:LearningObjectDefinition", new[] { DataFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            LearningObjectDefinition def = AssetDatabase.LoadAssetAtPath<LearningObjectDefinition>(path);
            if (def == null || string.IsNullOrEmpty(def.id))
            {
                continue;
            }

            byId[def.id] = def;
            pathById[def.id] = path;
        }

        if (byId.Count != TopicMapping.Entries.Length)
        {
            Debug.LogError($"[FaseA] DIBATALKAN: aset ditemukan {byId.Count}, " +
                           $"pemetaan {TopicMapping.Entries.Length}. Tidak ada yang ditulis.");
            return;
        }

        for (int i = 0; i < TopicMapping.Entries.Length; i++)
        {
            string id = TopicMapping.Entries[i].Id;
            if (!byId.ContainsKey(id))
            {
                Debug.LogError($"[FaseA] DIBATALKAN: aset '{id}' tidak ditemukan. Tidak ada yang ditulis.");
                return;
            }
        }

        // ---------------------------------------------------------- 2. backup
        try
        {
            CopyDirectory(dataFolderFull, backupDir);
        }
        catch (Exception error)
        {
            Debug.LogError($"[FaseA] DIBATALKAN: backup gagal: {error.Message}");
            return;
        }

        // ---------------------------------------------------------- 3. snapshot
        Dictionary<string, string> contentHashBefore = new Dictionary<string, string>();
        Dictionary<string, string> metaBefore = new Dictionary<string, string>();
        int questionsBefore = 0;

        for (int i = 0; i < TopicMapping.Entries.Length; i++)
        {
            LearningObjectDefinition def = byId[TopicMapping.Entries[i].Id];
            contentHashBefore[def.id] = Hash(ContentSignature(def));
            metaBefore[def.id] = MetadataSignature(def);
            questionsBefore += CountQuestions(def);
        }

        // ---------------------------------------------------------- 4. tulis metadata
        for (int i = 0; i < TopicMapping.Entries.Length; i++)
        {
            TopicMapping.Entry entry = TopicMapping.Entries[i];
            LearningObjectDefinition def = byId[entry.Id];

            def.topicId = entry.Topic;
            def.role = entry.Role;
            def.crossTopics = new List<string>(entry.CrossTopicCodes ?? new string[0]);
            def.order = entry.Order;
            EditorUtility.SetDirty(def);
        }

        AssetDatabase.SaveAssets();

        // ---------------------------------------------------------- 5. diff sebelum/sesudah
        bool ok = true;
        int questionsAfter = 0;
        int changedContent = 0;
        StringBuilder report = new StringBuilder();

        report.AppendLine("FASE A - TOPIC MAPPING REPORT");
        report.AppendLine("dibuat: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("LearningDataBuilder.BuildAll dijalankan: TIDAK");
        report.AppendLine("aset: " + byId.Count + " | entri pemetaan: " + TopicMapping.Entries.Length);
        report.AppendLine();
        report.AppendLine("id | topik | peran | cross | order | soal | hash_before | hash_after");
        report.AppendLine("---|---|---|---|---|---|---|---");

        for (int i = 0; i < TopicMapping.Entries.Length; i++)
        {
            TopicMapping.Entry entry = TopicMapping.Entries[i];
            LearningObjectDefinition def = byId[entry.Id];

            string hashAfter = Hash(ContentSignature(def));
            bool contentSame = hashAfter == contentHashBefore[def.id];
            if (!contentSame)
            {
                changedContent++;
                ok = false;
                Debug.LogError($"[FaseA] KONTEN BERUBAH pada '{def.id}'! Rollback.");
            }

            if (!MetadataMatches(def, entry))
            {
                ok = false;
                Debug.LogError($"[FaseA] Metadata tidak sesuai pada '{def.id}'.");
            }

            questionsAfter += CountQuestions(def);

            report.AppendLine(string.Join(" | ", new[]
            {
                def.id,
                def.topicId.ToString(),
                def.role.ToString(),
                string.Join(",", def.crossTopics.ToArray()),
                def.order.ToString(),
                CountQuestions(def).ToString(),
                hashAfter == contentHashBefore[def.id] ? contentHashBefore[def.id].Substring(0, 12) : "BERUBAH",
                hashAfter.Substring(0, 12)
            }));
        }

        if (questionsAfter != questionsBefore)
        {
            ok = false;
            Debug.LogError($"[FaseA] Jumlah soal berubah: {questionsBefore} -> {questionsAfter}. Rollback.");
        }

        report.AppendLine();
        report.AppendLine("total soal sebelum: " + questionsBefore);
        report.AppendLine("total soal sesudah: " + questionsAfter);
        report.AppendLine("konten berubah: " + changedContent + " / " + byId.Count);
        report.AppendLine("hasil: " + (ok ? "LULUS" : "GAGAL (rollback)"));

        try
        {
            File.WriteAllText(Path.Combine(backupDir, "mapping_report.txt"), report.ToString());
            File.WriteAllText(Path.Combine(backupDir, "mapping_meta_before.txt"), DumpMeta(metaBefore));
        }
        catch (Exception error)
        {
            Debug.LogWarning($"[FaseA] Gagal menulis laporan: {error.Message}");
        }

        // ---------------------------------------------------------- 6. rollback bila perlu
        if (!ok)
        {
            RestoreAssets(backupDir, dataFolderFull);
            Debug.LogError("[FaseA] GAGAL. Backup dipulihkan; tidak ada perubahan yang dipertahankan.");
            return;
        }

        Debug.Log($"[FaseA] LULUS. 32 aset dipetakan, 0 konten berubah, soal {questionsAfter}. " +
                  $"Laporan: {Path.Combine(backupDir, "mapping_report.txt")}");
    }

    // ---------------------------------------------------------------- helpers

    private static bool MetadataMatches(LearningObjectDefinition def, TopicMapping.Entry entry)
    {
        if (def.topicId != entry.Topic || def.role != entry.Role || def.order != entry.Order)
        {
            return false;
        }

        string[] expected = entry.CrossTopicCodes ?? new string[0];
        if (def.crossTopics == null || def.crossTopics.Count != expected.Length)
        {
            return false;
        }

        for (int i = 0; i < expected.Length; i++)
        {
            if (def.crossTopics[i] != expected[i])
            {
                return false;
            }
        }

        return true;
    }

    private static string ContentSignature(LearningObjectDefinition def)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(def.id).Append('|')
          .Append(def.sceneKey).Append('|')
          .Append(def.category).Append('|')
          .Append(def.namaIndonesia).Append('|')
          .Append(def.namaDawan).Append('|')
          .Append(def.caraBaca).Append('|')
          .Append((int)def.validationStatus).Append('|')
          .Append(def.validator).Append('|')
          .Append(def.validationDate).Append('|')
          .Append(def.validationNotes).Append('|')
          .Append(def.explanation).Append('|')
          .Append(def.image != null ? AssetDatabase.GetAssetPath(def.image) : "-").Append('|')
          .Append(def.pronunciation != null ? AssetDatabase.GetAssetPath(def.pronunciation) : "-")
          .Append('|');

        if (def.questions != null)
        {
            for (int i = 0; i < def.questions.Count; i++)
            {
                QuizQuestion question = def.questions[i];
                sb.Append("Q|").Append(question.question).Append('¦')
                  .Append(string.Join("§", question.options ?? new string[0])).Append('¦')
                  .Append(question.correctIndex).Append('¦')
                  .Append(question.explanation).Append('¦')
                  .Append(question.usesDawan).Append('|');
            }
        }

        return sb.ToString();
    }

    private static string MetadataSignature(LearningObjectDefinition def)
    {
        return def.topicId + "/" + def.role + "/" +
               string.Join(",", def.crossTopics != null ? def.crossTopics.ToArray() : new string[0]) +
               "/" + def.order;
    }

    private static string DumpMeta(Dictionary<string, string> meta)
    {
        StringBuilder sb = new StringBuilder();
        foreach (KeyValuePair<string, string> pair in meta)
        {
            sb.AppendLine(pair.Key + " = " + pair.Value);
        }

        return sb.ToString();
    }

    private static int CountQuestions(LearningObjectDefinition def)
    {
        return def.questions != null ? def.questions.Count : 0;
    }

    private static string Hash(string value)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (string file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }

        foreach (string directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    private static void RestoreAssets(string backupDir, string dataFolderFull)
    {
        foreach (string file in Directory.GetFiles(backupDir, "*", SearchOption.AllDirectories))
        {
            string extension = Path.GetExtension(file);
            if (extension != ".asset" && extension != ".meta")
            {
                continue;
            }

            string relative = file.Substring(backupDir.Length).TrimStart('\\', '/');
            string target = Path.Combine(dataFolderFull, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target) ?? dataFolderFull);
            File.Copy(file, target, true);
        }

        AssetDatabase.Refresh();
    }
}
