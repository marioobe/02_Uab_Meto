using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

/// <summary>
/// CurriculumImporter (Stage 2, editor-only).
///
/// Pipeline: validasi sumber -> lock -> recovery -> staging + validasi ulang ->
/// backup terverifikasi -> journal PREPARED -> temp -> journal PROMOTING ->
/// promosi File.Replace/File.Move -> verifikasi -> journal COMMITTED -> retensi.
/// Sumber absen = NO-OP tanpa membuat folder/output.
/// </summary>
public static class CurriculumImporter
{
    public enum ImportOutcome
    {
        NoOp,
        UpToDate,
        Imported,
        Locked,
        RecoveryRequired,
        Failed
    }

    public sealed class Config
    {
        public string ProjectRoot;
        public string ActiveDir;
        public string StateDir;
        public string StagingDir;
        public string AudioRoot;
        public string SourcePath;
        public LearningDatabase Database;
    }

    public static Config ProductionConfig()
    {
        string assets = Application.dataPath;
        string root = Directory.GetParent(assets)?.FullName ?? assets;
        return new Config
        {
            ProjectRoot = root,
            ActiveDir = Path.Combine(assets, "Resources", "Curriculum"),
            StateDir = Path.Combine(root, "Library", "UabMetoStage2"),
            StagingDir = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_Staging"),
            AudioRoot = Path.Combine(assets, "StreamingAssets", "Dawan"),
            SourcePath = Path.Combine(assets, "Curriculum", "source", "curriculum.json"),
            Database = AssetDatabase.LoadAssetAtPath<LearningDatabase>("Assets/Data/Learning/LearningDatabase.asset")
        };
    }

    /// <summary>
    /// Jalankan importer produksi. true = NO-OP / UP-TO-DATE / IMPORTED.
    /// false = lock aktif, perlu recovery, atau validasi/IO gagal.
    /// </summary>
    public static bool ImportProduction(out ImportOutcome outcome, out string message)
    {
        return Import(ProductionConfig(), out outcome, out message);
    }

    public static bool Import(Config config, out ImportOutcome outcome, out string message)
    {
        outcome = ImportOutcome.Failed;
        message = null;

        string error;
        if (!ValidateConfig(config, out error))
        {
            message = error;
            return false;
        }

        string sourcePath = NormalizePath(config.SourcePath);
        string activeDir = NormalizePath(config.ActiveDir);
        string stateDir = NormalizePath(config.StateDir);
        string stagingRoot = NormalizePath(config.StagingDir);
        string activePath = Path.Combine(activeDir, "curriculum.json");

        // Sumber absen: NO-OP tanpa membuat state/staging/output.
        if (!File.Exists(sourcePath))
        {
            outcome = ImportOutcome.NoOp;
            message = "Sumber kurikulum tidak ada; tidak ada perubahan.";
            return true;
        }

        if (!VerifyAncestors(sourcePath, out error))
        {
            message = error;
            return false;
        }

        byte[] sourceBytes;
        try
        {
            sourceBytes = ReadAllBytesShared(sourcePath);
        }
        catch (Exception exception)
        {
            message = "Gagal membaca sumber: " + exception.Message;
            return false;
        }

        List<string> validationErrors;
        if (!CurriculumValidator.ValidateBytes(sourceBytes, config.Database, config.AudioRoot,
                out validationErrors))
        {
            message = "Validasi sumber gagal:\n" + string.Join("\n", validationErrors);
            return false;
        }

        ImportLock held;
        bool heldByOther;
        if (!TryAcquireLock(stateDir, out held, out heldByOther, out error))
        {
            outcome = heldByOther ? ImportOutcome.Locked : ImportOutcome.Failed;
            message = error;
            return false;
        }

        using (held)
        {
            RecoveryOutcome recoveryOutcome;
            if (!RecoverLocked(config.ProjectRoot, stateDir, activeDir, stagingRoot, out recoveryOutcome,
                    out error))
            {
                outcome = ImportOutcome.RecoveryRequired;
                message = "Recovery menolak transaksi sebelumnya: " + error;
                return false;
            }

            if (!VerifyWritePath(activePath, config.ProjectRoot, out error))
            {
                message = error;
                return false;
            }

            if (File.Exists(activePath) && IsSameContent(activePath, sourceBytes))
            {
                outcome = ImportOutcome.UpToDate;
                message = "Kurikulum aktif sudah sama dengan sumber.";
                return true;
            }

            bool oldExists = File.Exists(activePath);
            string oldSha = oldExists ? Sha256File(activePath) : string.Empty;
            string transactionId = BuildTransactionId();
            string backupPath = BackupPath(stateDir, transactionId);
            string backupSha = string.Empty;

            string stagingPath = Path.Combine(stagingRoot, transactionId + ".json");
            if (!VerifyWritePath(stagingPath, stagingRoot, out error))
            {
                message = error;
                return false;
            }

            string newSha = Sha256Bytes(sourceBytes);
            try
            {
                Directory.CreateDirectory(stagingRoot);
                if (!VerifyWritePath(stagingPath, stagingRoot, out error))
                {
                    message = error;
                    return false;
                }

                using (FileStream stream = new FileStream(stagingPath, FileMode.CreateNew, FileAccess.Write,
                           FileShare.None))
                {
                    stream.Write(sourceBytes, 0, sourceBytes.Length);
                    stream.Flush(true);
                }

                byte[] stagedBytes = ReadAllBytesShared(stagingPath);
                if (!string.Equals(Sha256Bytes(stagedBytes), newSha, StringComparison.OrdinalIgnoreCase))
                {
                    message = "Staging tidak identik dengan sumber (SHA256 beda).";
                    return false;
                }

                if (!CurriculumValidator.ValidateBytes(stagedBytes, config.Database, config.AudioRoot,
                        out validationErrors))
                {
                    message = "Validasi ulang staging gagal:\n" + string.Join("\n", validationErrors);
                    return false;
                }
            }
            catch (Exception exception)
            {
                message = "Gagal menulis staging: " + exception.Message;
                return false;
            }

            if (oldExists)
            {
                if (!CreateBackup(stateDir, transactionId, activePath, out backupPath, out backupSha,
                        out error))
                {
                    message = error;
                    return false;
                }

                if (!IsBackupValid(backupPath, backupSha, out error) ||
                    !FilesIdentical(activePath, backupPath))
                {
                    message = "Backup aktif tidak identik byte-per-byte: " + error;
                    return false;
                }
            }

            string plannedTempPath = Path.Combine(activeDir,
                ".curriculum." + Guid.NewGuid().ToString("N") + ".tmp");
            if (!VerifyWritePath(plannedTempPath, config.ProjectRoot, out error))
            {
                message = error;
                return false;
            }

            string plannedParent;
            try
            {
                plannedParent = NormalizePath(Path.GetDirectoryName(plannedTempPath) ?? string.Empty);
            }
            catch (Exception exception)
            {
                message = "Direktori temp tidak valid: " + exception.Message;
                return false;
            }

            if (!string.Equals(plannedParent, NormalizePath(activeDir), StringComparison.OrdinalIgnoreCase))
            {
                message = "Temp promosi harus berada di direktori aktif (se-volume).";
                return false;
            }

            Journal journal = new Journal
            {
                transactionId = transactionId,
                sourcePath = sourcePath,
                activePath = activePath,
                stagingPath = stagingPath,
                promotionTempPath = plannedTempPath,
                backupPath = backupPath,
                oldActiveExists = oldExists,
                oldActiveSha256 = oldSha,
                newContentSha256 = newSha,
                backupSha256 = backupSha,
                state = StatePrepared,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            if (!WriteJournalAtomic(stateDir, journal, out error))
            {
                message = error;
                return false;
            }

            string tempPath;
            if (!PreparePromotionTempAt(config.ProjectRoot, activeDir, sourceBytes, plannedTempPath,
                    out tempPath, out _, out error))
            {
                message = "Temp promosi gagal disiapkan; journal PREPARED tertinggal untuk recovery. " + error;
                return false;
            }

            journal.state = StatePromoting;
            if (!WriteJournalAtomic(stateDir, journal, out error))
            {
                message = "Gagal menulis journal PROMOTING: " + error;
                outcome = ImportOutcome.RecoveryRequired;
                return false;
            }

            if (!Promote(config.ProjectRoot, activeDir, activePath, tempPath, oldExists, oldSha, newSha,
                    out error))
            {
                message = error;
                outcome = ImportOutcome.RecoveryRequired;
                return false;
            }

            journal.state = StateCommitted;
            if (!WriteJournalAtomic(stateDir, journal, out error))
            {
                message = "Promosi berhasil tetapi journal COMMITTED gagal: " + error;
                outcome = ImportOutcome.RecoveryRequired;
                return false;
            }

            if (oldExists)
            {
                if (!RecordCommittedBackup(stateDir, transactionId, backupSha, out error))
                {
                    message = "Impor berhasil; backup index gagal dicatat: " + error;
                }
                else
                {
                    string cleanupError;
                    CleanupBackups(stateDir, transactionId, true, out cleanupError);
                }
            }

            // Staging hanya dihapus setelah transaksi COMMITTED.
            TryDeleteFile(stagingPath);
            if (IsWithin(Application.dataPath, activeDir))
            {
                AssetDatabase.Refresh();
            }

            outcome = ImportOutcome.Imported;
            if (string.IsNullOrEmpty(message))
            {
                message = "Kurikulum berhasil diimpor (transaksi " + transactionId + ").";
            }

            return true;
        }
    }

    private static bool ValidateConfig(Config config, out string error)
    {
        error = null;
        if (config == null)
        {
            error = "Config null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.ProjectRoot) ||
            string.IsNullOrWhiteSpace(config.ActiveDir) ||
            string.IsNullOrWhiteSpace(config.StateDir) ||
            string.IsNullOrWhiteSpace(config.StagingDir) ||
            string.IsNullOrWhiteSpace(config.SourcePath))
        {
            error = "Config path belum lengkap.";
            return false;
        }

        if (config.Database == null)
        {
            error = "LearningDatabase tidak tersedia.";
            return false;
        }

        try
        {
            string project = NormalizePath(config.ProjectRoot);
            string active = NormalizePath(config.ActiveDir);
            string state = NormalizePath(config.StateDir);
            string staging = NormalizePath(config.StagingDir);
            string source = NormalizePath(config.SourcePath);

            if (!IsWithin(project, active) || string.Equals(project, active, StringComparison.OrdinalIgnoreCase))
            {
                error = "ActiveDir harus di dalam ProjectRoot (bukan root itu sendiri).";
                return false;
            }

            if (!IsWithin(project, state) || IsWithin(active, state) || IsWithin(state, active))
            {
                error = "StateDir harus di dalam ProjectRoot dan terpisah dari ActiveDir.";
                return false;
            }

            if (!IsWithin(project, source) || IsWithin(active, source) || IsWithin(source, active))
            {
                error = "SourcePath harus di dalam ProjectRoot dan terpisah dari ActiveDir.";
                return false;
            }

            if (IsWithin(active, staging) || IsWithin(staging, active))
            {
                error = "StagingDir tidak boleh beririsan dengan ActiveDir.";
                return false;
            }

            if (IsWithin(state, staging) || IsWithin(staging, state))
            {
                error = "StagingDir tidak boleh beririsan dengan StateDir.";
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            error = "Config path tidak valid: " + exception.Message;
            return false;
        }
    }

    private static string BuildTransactionId()
    {
        return DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" +
               Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    private static byte[] ReadAllBytesShared(string path)
    {
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            long length = stream.Length;
            if (length <= 0 || length > int.MaxValue)
            {
                throw new IOException("Ukuran file tidak valid: " + length);
            }

            byte[] bytes = new byte[length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int read = stream.Read(bytes, offset, bytes.Length - offset);
                if (read <= 0)
                {
                    throw new IOException("File berubah/terpotong saat dibaca.");
                }

                offset += read;
            }

            return bytes;
        }
    }

    private static bool IsSameContent(string path, byte[] expected)
    {
        try
        {
            if (!File.Exists(path) || new FileInfo(path).Length != expected.Length)
            {
                return false;
            }

            return string.Equals(Sha256File(path), Sha256Bytes(expected), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    // ---------------------------------------------------------------- path guard

    public static string NormalizePath(string path)
    {
        string full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// True bila candidate berada di dalam root (perbandingan batas direktori,
    /// bukan StartsWith naif). Root dan candidate di-canonical-kan.
    /// </summary>
    public static bool IsWithin(string root, string candidate)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        try
        {
            string r = NormalizePath(root);
            string c = NormalizePath(candidate);
            if (r.Length == 0 || c.Length == 0)
            {
                return false;
            }

            if (string.Equals(c, r, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return c.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Validasi seluruh komponen direktori yang ada dari volume root sampai path.
    /// Reparse point / atribut tak terbaca / UNC -> gagal (fail closed).
    /// </summary>
    public static bool VerifyAncestors(string path, out string error)
    {
        error = null;

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception exception)
        {
            error = "Path tidak valid: " + exception.Message;
            return false;
        }

        if (full.StartsWith("\\\\", StringComparison.Ordinal))
        {
            error = "UNC path tidak didukung: " + full;
            return false;
        }

        string volumeRoot = Path.GetPathRoot(full);
        if (string.IsNullOrEmpty(volumeRoot) || volumeRoot.StartsWith("\\\\", StringComparison.Ordinal))
        {
            error = "Volume root tidak didukung: " + full;
            return false;
        }

        FileAttributes rootAttributes;
        bool rootExists;
        if (!TryGetExistingAttributes(volumeRoot, out rootExists, out rootAttributes, out error))
        {
            return false;
        }

        if (!rootExists)
        {
            error = "Volume root tidak dapat diverifikasi: " + volumeRoot;
            return false;
        }

        if ((rootAttributes & FileAttributes.ReparsePoint) != 0)
        {
            error = "Reparse point (symlink/junction) tidak aman: " + volumeRoot;
            return false;
        }

        string relative = full.Substring(volumeRoot.Length);
        string[] parts = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        string current = volumeRoot;
        for (int i = 0; i < parts.Length; i++)
        {
            current = Path.Combine(current, parts[i]);

            FileAttributes attributes;
            bool exists;
            if (!TryGetExistingAttributes(current, out exists, out attributes, out error))
            {
                return false;
            }

            if (!exists)
            {
                break;
            }

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                error = "Reparse point (symlink/junction) tidak aman: " + current;
                return false;
            }

            if (i < parts.Length - 1 && (attributes & FileAttributes.Directory) == 0)
            {
                error = "Komponen path bukan direktori: " + current;
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// File.GetAttributes tanpa menelan UnauthorizedAccess. Path yang tidak ada
    /// dikembalikan exists=false; path yang tidak dapat dibaca -> false + error.
    /// </summary>
    private static bool TryGetExistingAttributes(string path, out bool exists,
        out FileAttributes attributes, out string error)
    {
        exists = false;
        attributes = 0;
        error = null;

        try
        {
            attributes = File.GetAttributes(path);
            exists = true;
            return true;
        }
        catch (FileNotFoundException)
        {
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            return true;
        }
        catch (UnauthorizedAccessException exception)
        {
            error = "Atribut path tidak dapat diverifikasi: " + path + " (" + exception.Message + ")";
            return false;
        }
        catch (Exception exception)
        {
            error = "Atribut path tidak dapat diverifikasi: " + path + " (" + exception.Message + ")";
            return false;
        }
    }

    /// <summary>Path harus di dalam allowlist + lolos ancestor validation.</summary>
    public static bool VerifyWritePath(string path, string allowRoot, out string error)
    {
        error = null;
        if (!IsWithin(allowRoot, path))
        {
            error = "Path di luar allowlist: " + path;
            return false;
        }

        return VerifyAncestors(path, out error);
    }

    // ---------------------------------------------------------------- journal

    public const string StatePrepared = "PREPARED";
    public const string StatePromoting = "PROMOTING";
    public const string StateCommitted = "COMMITTED";
    public const string StateRecoveryRequired = "RECOVERY_REQUIRED";

    [Serializable]
    public class Journal
    {
        public string transactionId;
        public string sourcePath;
        public string activePath;
        public string stagingPath;
        public string promotionTempPath;
        public string backupPath;
        public bool oldActiveExists;
        public string oldActiveSha256;
        public string newContentSha256;
        public string backupSha256;
        public string state;
        public string timestamp;
        public int journalSchemaVersion = 1;
    }

    public static string JournalPath(string stateDir)
    {
        return Path.Combine(stateDir, "journal.json");
    }

    /// <summary>Journal ditulis atomik: journal.json.tmp -> File.Replace / File.Move. Tidak pernah langsung.</summary>
    public static bool WriteJournalAtomic(string stateDir, Journal journal, out string error)
    {
        error = null;
        if (journal == null)
        {
            error = "Journal null.";
            return false;
        }

        string journalPath = JournalPath(stateDir);
        string tmpPath = journalPath + ".tmp";

        if (!VerifyWritePath(journalPath, stateDir, out error))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(stateDir);
            string json = JsonConvert.SerializeObject(journal, Formatting.Indented);
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (FileStream stream = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            // Verifikasi isi .tmp sebelum dipromosikan.
            Journal check = JsonConvert.DeserializeObject<Journal>(File.ReadAllText(tmpPath));
            if (check == null || check.transactionId != journal.transactionId ||
                check.state != journal.state || check.newContentSha256 != journal.newContentSha256)
            {
                error = "Journal .tmp tidak valid; journal lama dipertahankan.";
                TryDeleteFile(tmpPath);
                return false;
            }

            if (File.Exists(journalPath))
            {
                File.Replace(tmpPath, journalPath, null);
            }
            else
            {
                File.Move(tmpPath, journalPath);
            }

            return true;
        }
        catch (Exception exception)
        {
            error = "Gagal menulis journal: " + exception.Message;
            TryDeleteFile(tmpPath);
            return false;
        }
    }

    /// <summary>Hanya journal.json yang authoritative; .tmp tidak pernah dibaca.</summary>
    public static bool TryReadJournal(string stateDir, out Journal journal, out string error)
    {
        journal = null;
        error = null;
        string path = JournalPath(stateDir);
        if (!File.Exists(path))
        {
            error = "Journal belum ada.";
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            journal = JsonConvert.DeserializeObject<Journal>(json);
            if (journal == null || string.IsNullOrEmpty(journal.state) ||
                string.IsNullOrEmpty(journal.transactionId))
            {
                error = "Journal tidak valid.";
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            error = "Journal korup: " + exception.Message;
            return false;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception)
        {
            // abaikan
        }
    }

    // ---------------------------------------------------------------- backup

    [Serializable]
    public class BackupIndexEntry
    {
        public string transactionId;
        public string sha256;
        public string committedAt;
    }

    [Serializable]
    public class BackupIndex
    {
        public List<BackupIndexEntry> entries = new List<BackupIndexEntry>();
    }

    public static string BackupDir(string stateDir, string transactionId)
    {
        return Path.Combine(stateDir, "Backup", transactionId);
    }

    public static string BackupPath(string stateDir, string transactionId)
    {
        return Path.Combine(BackupDir(stateDir, transactionId), "curriculum.json");
    }

    public static string Sha256File(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(stream);
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }

    public static bool FilesIdentical(string firstPath, string secondPath)
    {
        try
        {
            FileInfo firstInfo = new FileInfo(firstPath);
            FileInfo secondInfo = new FileInfo(secondPath);
            if (!firstInfo.Exists || !secondInfo.Exists || firstInfo.Length != secondInfo.Length)
            {
                return false;
            }

            using (FileStream first = new FileStream(firstPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream second = new FileStream(secondPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] firstBuffer = new byte[81920];
                byte[] secondBuffer = new byte[81920];
                while (true)
                {
                    int firstRead = first.Read(firstBuffer, 0, firstBuffer.Length);
                    int secondRead = second.Read(secondBuffer, 0, secondBuffer.Length);
                    if (firstRead != secondRead)
                    {
                        return false;
                    }

                    if (firstRead == 0)
                    {
                        return true;
                    }

                    for (int i = 0; i < firstRead; i++)
                    {
                        if (firstBuffer[i] != secondBuffer[i])
                        {
                            return false;
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool CreateBackup(string stateDir, string transactionId, string activePath,
        out string backupPath, out string sha256, out string error)
    {
        backupPath = BackupPath(stateDir, transactionId);
        sha256 = null;
        error = null;

        if (!IsSafeTransactionId(transactionId))
        {
            error = "transactionId backup tidak valid.";
            return false;
        }

        if (!VerifyWritePath(backupPath, stateDir, out error))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath) ?? stateDir);
            File.Copy(activePath, backupPath, true);

            string activeSha = Sha256File(activePath);
            string backupSha = Sha256File(backupPath);
            if (!string.Equals(activeSha, backupSha, StringComparison.OrdinalIgnoreCase) ||
                !FilesIdentical(activePath, backupPath))
            {
                error = "Backup tidak identik dengan aktif (SHA256/byte beda).";
                TryDeleteFile(backupPath);
                return false;
            }

            sha256 = backupSha;
            return true;
        }
        catch (Exception exception)
        {
            error = "Gagal membuat backup: " + exception.Message;
            return false;
        }
    }

    public static bool IsBackupValid(string backupPath, string expectedSha256, out string error)
    {
        error = null;
        if (!File.Exists(backupPath))
        {
            error = "Backup tidak ditemukan.";
            return false;
        }

        string actual = Sha256File(backupPath);
        if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            error = "Backup SHA256 tidak cocok.";
            return false;
        }

        return true;
    }

    private static string BackupIndexPath(string stateDir)
    {
        return Path.Combine(stateDir, "backup_index.json");
    }

    private static BackupIndex LoadBackupIndex(string stateDir)
    {
        string path = BackupIndexPath(stateDir);
        if (!File.Exists(path))
        {
            return new BackupIndex();
        }

        try
        {
            BackupIndex index = JsonConvert.DeserializeObject<BackupIndex>(File.ReadAllText(path));
            return index ?? new BackupIndex();
        }
        catch (Exception)
        {
            return new BackupIndex();
        }
    }

    /// <summary>Catat backup yang sudah COMMITTED + terverifikasi (dasar retensi aman).</summary>
    public static bool RecordCommittedBackup(string stateDir, string transactionId, string sha256,
        out string error)
    {
        error = null;
        if (!IsSafeTransactionId(transactionId) || !IsHexSha256(sha256))
        {
            error = "Data backup index tidak valid.";
            return false;
        }

        string path = BackupIndexPath(stateDir);
        if (!VerifyWritePath(path, stateDir, out error))
        {
            return false;
        }

        try
        {
            BackupIndex index = LoadBackupIndex(stateDir);
            BackupIndexEntry existing = index.entries.Find(entry => entry.transactionId == transactionId);
            if (existing == null)
            {
                existing = new BackupIndexEntry { transactionId = transactionId };
                index.entries.Add(existing);
            }

            existing.sha256 = sha256;
            existing.committedAt = DateTime.UtcNow.ToString("o");

            string json = JsonConvert.SerializeObject(index, Formatting.Indented);
            string tmp = path + ".tmp";
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (FileStream stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.Exists(path))
            {
                File.Replace(tmp, path, null);
            }
            else
            {
                File.Move(tmp, path);
            }

            return true;
        }
        catch (Exception exception)
        {
            error = "Gagal mencatat backup index: " + exception.Message;
            return false;
        }
    }

    /// <summary>
    /// Backup transaksi current wajib ada, tercatat di index, aman path-nya, dan
    /// SHA256-nya cocok. Gagal = cleanup harus dibatalkan seluruhnya.
    /// </summary>
    private static bool TryVerifyCurrentBackup(string stateDir, string transactionId, out string error)
    {
        error = null;
        if (!IsSafeTransactionId(transactionId))
        {
            error = "transactionId current tidak valid.";
            return false;
        }

        BackupIndex index = LoadBackupIndex(stateDir);
        BackupIndexEntry entry = index.entries.Find(item =>
            item != null && item.transactionId == transactionId);
        if (entry == null)
        {
            error = "Backup current tidak tercatat sebagai COMMITTED.";
            return false;
        }

        if (!IsHexSha256(entry.sha256) || string.IsNullOrEmpty(entry.committedAt))
        {
            error = "Entry backup current tidak valid.";
            return false;
        }

        string path = BackupPath(stateDir, transactionId);
        if (!VerifyWritePath(path, stateDir, out error))
        {
            return false;
        }

        if (!File.Exists(path))
        {
            error = "Backup current tidak ditemukan.";
            return false;
        }

        try
        {
            string actual = Sha256File(path);
            if (!string.Equals(actual, entry.sha256, StringComparison.OrdinalIgnoreCase))
            {
                error = "Backup current tidak cocok dengan SHA256 index.";
                return false;
            }
        }
        catch (Exception exception)
        {
            error = "Backup current tidak dapat dibaca: " + exception.Message;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Retensi: hanya hapus backup yang tercatat COMMITTED + hash cocok, lebih lama dari
    /// backup aktif, dan bukan transaksi saat ini. Orphan (tidak tercatat) tidak dihapus.
    /// Mengembalikan jumlah direktori yang dihapus.
    /// </summary>
    public static int CleanupBackups(string stateDir, string currentTransactionId, bool currentCommitted,
        out string error)
    {
        error = null;
        if (!currentCommitted)
        {
            return 0;
        }

        if (!TryVerifyCurrentBackup(stateDir, currentTransactionId, out error))
        {
            return 0;
        }

        string backupRoot = Path.Combine(stateDir, "Backup");
        if (!Directory.Exists(backupRoot))
        {
            return 0;
        }

        BackupIndex index = LoadBackupIndex(stateDir);
        List<BackupIndexEntry> candidates = index.entries.FindAll(entry =>
            entry != null && entry.transactionId != currentTransactionId &&
            !string.IsNullOrEmpty(entry.sha256) && !string.IsNullOrEmpty(entry.committedAt));

        candidates.Sort((a, b) => string.CompareOrdinal(b.committedAt, a.committedAt));

        int deleted = 0;
        // Semua kandidat non-current yang tercatat COMMITTED + hash cocok boleh dihapus;
        // backup transaksi saat ini tetap dipertahankan (minimal satu backup valid).
        for (int i = 0; i < candidates.Count; i++)
        {
            BackupIndexEntry entry = candidates[i];
            if (!IsSafeTransactionId(entry.transactionId))
            {
                // Index rusak/dimanipulasi: jangan hapus apa pun.
                continue;
            }

            string dir = BackupDir(stateDir, entry.transactionId);
            if (!IsWithin(backupRoot, dir) || !VerifyAncestors(dir, out _))
            {
                continue;
            }

            string file = Path.Combine(dir, "curriculum.json");
            if (!File.Exists(file))
            {
                continue;
            }

            string actual = Sha256File(file);
            if (!string.Equals(actual, entry.sha256, StringComparison.OrdinalIgnoreCase))
            {
                // Hash tidak cocok -> jangan hapus (bukti).
                continue;
            }

            try
            {
                Directory.Delete(dir, true);
                deleted++;
            }
            catch (Exception exception)
            {
                error = "Gagal menghapus backup lama: " + exception.Message;
            }
        }

        return deleted;
    }

    // ---------------------------------------------------------------- promotion

    /// <summary>
    /// Tulis konten baru ke file temp di direktori aktif (se-volume dengan target),
    /// Flush(true), lalu verifikasi SHA256. File aktif tidak disentuh.
    /// </summary>
    public static bool PreparePromotionTemp(string allowRoot, string activeDir, byte[] content,
        out string tempPath, out string sha256, out string error)
    {
        string candidate = Path.Combine(activeDir, ".curriculum." + Guid.NewGuid().ToString("N") + ".tmp");
        return PreparePromotionTempAt(allowRoot, activeDir, content, candidate, out tempPath, out sha256,
            out error);
    }

    /// <summary>
    /// Varian path eksplisit: dipakai importer agar path temp sudah tercatat di journal
    /// PREPARED sebelum file temp ditulis.
    /// </summary>
    public static bool PreparePromotionTempAt(string allowRoot, string activeDir, byte[] content,
        string candidatePath, out string tempPath, out string sha256, out string error)
    {
        tempPath = null;
        sha256 = null;
        error = null;

        if (content == null || content.Length == 0)
        {
            error = "Konten promosi kosong.";
            return false;
        }

        if (string.IsNullOrEmpty(candidatePath))
        {
            error = "Path temp promosi kosong.";
            return false;
        }

        if (!VerifyWritePath(activeDir, allowRoot, out error))
        {
            return false;
        }

        if (!VerifyWritePath(candidatePath, allowRoot, out error))
        {
            return false;
        }

        string candidateParent;
        try
        {
            candidateParent = NormalizePath(Path.GetDirectoryName(candidatePath) ?? string.Empty);
        }
        catch (Exception exception)
        {
            error = "Direktori temp tidak valid: " + exception.Message;
            return false;
        }

        if (!string.Equals(candidateParent, NormalizePath(activeDir), StringComparison.OrdinalIgnoreCase))
        {
            error = "Temp promosi harus berada di direktori aktif (se-volume).";
            return false;
        }

        try
        {
            Directory.CreateDirectory(activeDir);
            // Revalidasi setelah direktori dibuat (ancestor bisa berubah).
            if (!VerifyWritePath(activeDir, allowRoot, out error) ||
                !VerifyWritePath(candidatePath, allowRoot, out error))
            {
                return false;
            }

            try
            {
                using (FileStream stream = new FileStream(candidatePath, FileMode.CreateNew, FileAccess.Write,
                           FileShare.None))
                {
                    stream.Write(content, 0, content.Length);
                    stream.Flush(true);
                }
            }
            catch (Exception)
            {
                TryDeleteFile(candidatePath);
                throw;
            }

            string actual = Sha256File(candidatePath);
            string expected = Sha256Bytes(content);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                error = "Temp promosi tidak identik dengan konten baru (SHA256 beda).";
                TryDeleteFile(candidatePath);
                return false;
            }

            tempPath = candidatePath;
            sha256 = actual;
            return true;
        }
        catch (Exception exception)
        {
            error = "Gagal menyiapkan temp promosi: " + exception.Message;
            return false;
        }
    }

    /// <summary>
    /// Promosi tanpa copy+delete:
    /// - oldActiveExists=false: target wajib belum ada -> File.Move (first-run).
    /// - oldActiveExists=true : target wajib ada + hash lama cocok -> File.Replace.
    /// Target yang muncul tiba-tiba pada first-run membatalkan promosi.
    /// </summary>
    public static bool Promote(string allowRoot, string activeDir, string activePath, string tempPath,
        bool oldActiveExists, string oldSha256, string newSha256, out string error)
    {
        error = null;

        if (!VerifyWritePath(activePath, allowRoot, out error))
        {
            return false;
        }

        if (!VerifyWritePath(tempPath, allowRoot, out error))
        {
            return false;
        }

        string tempParent;
        try
        {
            tempParent = NormalizePath(Path.GetDirectoryName(tempPath) ?? string.Empty);
        }
        catch (Exception exception)
        {
            error = "Direktori temp tidak valid: " + exception.Message;
            return false;
        }

        if (!string.Equals(tempParent, NormalizePath(activeDir), StringComparison.OrdinalIgnoreCase))
        {
            error = "Temp promosi harus berada di direktori aktif (se-volume).";
            return false;
        }

        if (!File.Exists(tempPath))
        {
            error = "Temp promosi tidak ditemukan.";
            return false;
        }

        string tempSha = Sha256File(tempPath);
        if (!string.Equals(tempSha, newSha256, StringComparison.OrdinalIgnoreCase))
        {
            error = "Temp promosi tidak cocok dengan SHA256 konten baru.";
            return false;
        }

        try
        {
            if (oldActiveExists)
            {
                if (!File.Exists(activePath))
                {
                    error = "File aktif hilang padahal transaksi mengharapkan file lama.";
                    return false;
                }

                string currentSha = Sha256File(activePath);
                if (!string.Equals(currentSha, oldSha256, StringComparison.OrdinalIgnoreCase))
                {
                    error = "File aktif berubah sejak backup (SHA256 beda); promosi dibatalkan.";
                    return false;
                }

                File.Replace(tempPath, activePath, null);
            }
            else
            {
                if (File.Exists(activePath))
                {
                    error = "Target aktif muncul pada first-run; promosi dibatalkan.";
                    return false;
                }

                File.Move(tempPath, activePath);
            }

            string promotedSha = Sha256File(activePath);
            if (!string.Equals(promotedSha, newSha256, StringComparison.OrdinalIgnoreCase))
            {
                error = "Verifikasi pasca-promosi gagal: SHA256 target tidak cocok.";
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            error = "Promosi gagal: " + exception.Message;
            return false;
        }
    }

    public static string Sha256Bytes(byte[] content)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(content);
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }

    // ---------------------------------------------------------------- recovery

    public enum RecoveryOutcome
    {
        NoJournal,
        Committed,
        RolledBack,
        RecoveryRequired
    }

    /// <summary>
    /// Recovery state machine. WAJIB dipanggil saat import.lock dipegang (Tahap 7).
    /// Tidak pernah menebak: state/isi ambigu -> RECOVERY_REQUIRED, file aktif tidak disentuh.
    /// </summary>
    public static bool RecoverLocked(string allowRoot, string stateDir, string activeDir,
        string stagingRoot, out RecoveryOutcome outcome, out string error)
    {
        outcome = RecoveryOutcome.NoJournal;
        error = null;

        Journal journal;
        if (!TryReadJournal(stateDir, out journal, out error))
        {
            if (!File.Exists(JournalPath(stateDir)))
            {
                error = null;
                return true;
            }

            outcome = RecoveryOutcome.RecoveryRequired;
            return false;
        }

        try
        {
            if (!ValidateRecoveryJournal(journal, allowRoot, stateDir, activeDir, stagingRoot, out error))
            {
                outcome = RecoveryOutcome.RecoveryRequired;
                return false;
            }
        }
        catch (Exception exception)
        {
            error = "Journal recovery tidak dapat divalidasi: " + exception.Message;
            outcome = RecoveryOutcome.RecoveryRequired;
            return false;
        }

        string activePath = journal.activePath;

        if (journal.state == StateRecoveryRequired)
        {
            error = "Transaksi sebelumnya berstatus RECOVERY_REQUIRED; tindakan manual diperlukan.";
            outcome = RecoveryOutcome.RecoveryRequired;
            return false;
        }

        string activeSha = null;
        bool activeExists;
        try
        {
            activeExists = File.Exists(activePath);
            if (activeExists)
            {
                activeSha = Sha256File(activePath);
            }
        }
        catch (Exception exception)
        {
            error = "Tidak dapat membaca file aktif saat recovery: " + exception.Message;
            outcome = RecoveryOutcome.RecoveryRequired;
            return false;
        }

        bool isOldState = journal.oldActiveExists
            ? activeExists && string.Equals(activeSha, journal.oldActiveSha256, StringComparison.OrdinalIgnoreCase)
            : !activeExists;
        bool isNewState = activeExists &&
                          string.Equals(activeSha, journal.newContentSha256, StringComparison.OrdinalIgnoreCase);

        if (journal.state == StateCommitted)
        {
            if (!isNewState)
            {
                error = "Journal COMMITTED tetapi file aktif bukan konten baru.";
                outcome = RecoveryOutcome.RecoveryRequired;
                MarkRecoveryRequired(stateDir, journal);
                return false;
            }

            if (!TryCleanupPromotionTemp(journal, allowRoot, activeDir, out error))
            {
                outcome = RecoveryOutcome.RecoveryRequired;
                MarkRecoveryRequired(stateDir, journal);
                return false;
            }

            outcome = RecoveryOutcome.Committed;
            return true;
        }

        if (journal.state == StatePromoting && isNewState)
        {
            // Promosi sudah terjadi; selesaikan commit.
            if (!TryCleanupPromotionTemp(journal, allowRoot, activeDir, out error))
            {
                outcome = RecoveryOutcome.RecoveryRequired;
                MarkRecoveryRequired(stateDir, journal);
                return false;
            }

            journal.state = StateCommitted;
            if (!WriteJournalAtomic(stateDir, journal, out error))
            {
                outcome = RecoveryOutcome.RecoveryRequired;
                return false;
            }

            outcome = RecoveryOutcome.Committed;
            return true;
        }

        bool canRollback = journal.state == StatePrepared || journal.state == StatePromoting;
        if (canRollback && isOldState)
        {
            if (!TryCleanupPromotionTemp(journal, allowRoot, activeDir, out error))
            {
                outcome = RecoveryOutcome.RecoveryRequired;
                MarkRecoveryRequired(stateDir, journal);
                return false;
            }

            outcome = RecoveryOutcome.RolledBack;
            return true;
        }

        if (canRollback && journal.oldActiveExists && !activeExists)
        {
            // File aktif hilang: pulihkan dari backup yang sudah terverifikasi.
            if (!RestoreFromBackup(journal, allowRoot, activeDir, out error))
            {
                outcome = RecoveryOutcome.RecoveryRequired;
                MarkRecoveryRequired(stateDir, journal);
                return false;
            }

            if (!TryCleanupPromotionTemp(journal, allowRoot, activeDir, out error))
            {
                outcome = RecoveryOutcome.RecoveryRequired;
                MarkRecoveryRequired(stateDir, journal);
                return false;
            }

            outcome = RecoveryOutcome.RolledBack;
            return true;
        }

        error = "State recovery ambigu (state=" + journal.state + ", aktif ada=" + activeExists +
                "); file aktif tidak diubah.";
        outcome = RecoveryOutcome.RecoveryRequired;
        MarkRecoveryRequired(stateDir, journal);
        return false;
    }

    private static bool ValidateRecoveryJournal(Journal journal, string allowRoot, string stateDir,
        string activeDir, string stagingRoot, out string error)
    {
        error = null;

        if (!IsSafeTransactionId(journal.transactionId))
        {
            error = "transactionId journal tidak valid.";
            return false;
        }

        if (string.IsNullOrEmpty(journal.newContentSha256) || !IsHexSha256(journal.newContentSha256))
        {
            error = "newContentSha256 journal tidak valid.";
            return false;
        }

        if (string.IsNullOrEmpty(journal.activePath) || string.IsNullOrEmpty(journal.backupPath))
        {
            error = "Path wajib journal kosong.";
            return false;
        }

        string expectedActive = Path.Combine(activeDir, "curriculum.json");
        if (!string.Equals(NormalizePath(journal.activePath), NormalizePath(expectedActive),
                StringComparison.OrdinalIgnoreCase))
        {
            error = "activePath journal tidak sesuai direktori aktif.";
            return false;
        }

        if (!VerifyWritePath(journal.activePath, allowRoot, out error))
        {
            return false;
        }

        if (!string.Equals(NormalizePath(journal.backupPath), NormalizePath(BackupPath(stateDir, journal.transactionId)),
                StringComparison.OrdinalIgnoreCase))
        {
            error = "backupPath journal tidak sesuai state dir.";
            return false;
        }

        if (!string.IsNullOrEmpty(journal.promotionTempPath))
        {
            if (!VerifyWritePath(journal.promotionTempPath, allowRoot, out error))
            {
                return false;
            }

            string parent = Path.GetDirectoryName(journal.promotionTempPath) ?? string.Empty;
            if (!string.Equals(NormalizePath(parent), NormalizePath(activeDir), StringComparison.OrdinalIgnoreCase))
            {
                error = "promotionTempPath journal di luar direktori aktif.";
                return false;
            }
        }

        if (!string.IsNullOrEmpty(journal.stagingPath) &&
            !IsWithin(stagingRoot, journal.stagingPath))
        {
            error = "stagingPath journal di luar staging root.";
            return false;
        }

        if (journal.oldActiveExists)
        {
            if (!IsHexSha256(journal.oldActiveSha256))
            {
                error = "oldActiveSha256 journal tidak valid.";
                return false;
            }

            if (!IsHexSha256(journal.backupSha256))
            {
                error = "backupSha256 journal tidak valid.";
                return false;
            }

            if (!File.Exists(journal.backupPath))
            {
                error = "Backup transaksi tidak ditemukan.";
                return false;
            }

            string backupSha = Sha256File(journal.backupPath);
            if (!string.Equals(backupSha, journal.backupSha256, StringComparison.OrdinalIgnoreCase))
            {
                error = "Backup transaksi tidak cocok dengan SHA256 journal.";
                return false;
            }
        }
        else if (!string.IsNullOrEmpty(journal.backupSha256))
        {
            if (!IsHexSha256(journal.backupSha256) || !File.Exists(journal.backupPath) ||
                !string.Equals(Sha256File(journal.backupPath), journal.backupSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                error = "Backup first-run tidak konsisten.";
                return false;
            }
        }

        return true;
    }

    private static bool TryCleanupPromotionTemp(Journal journal, string allowRoot, string activeDir,
        out string error)
    {
        error = null;
        string temp = journal.promotionTempPath;
        if (string.IsNullOrEmpty(temp) || !File.Exists(temp))
        {
            return true;
        }

        if (!VerifyWritePath(temp, allowRoot, out error))
        {
            return false;
        }

        string parent = Path.GetDirectoryName(temp) ?? string.Empty;
        if (!string.Equals(NormalizePath(parent), NormalizePath(activeDir), StringComparison.OrdinalIgnoreCase))
        {
            error = "Temp promosi berada di luar direktori aktif.";
            return false;
        }

        string tempSha = Sha256File(temp);
        if (!string.Equals(tempSha, journal.newContentSha256, StringComparison.OrdinalIgnoreCase))
        {
            error = "Temp promosi berisi konten tak dikenal; tidak dihapus.";
            return false;
        }

        try
        {
            File.Delete(temp);
            return true;
        }
        catch (Exception exception)
        {
            error = "Gagal menghapus temp promosi: " + exception.Message;
            return false;
        }
    }

    private static bool RestoreFromBackup(Journal journal, string allowRoot, string activeDir, out string error)
    {
        error = null;
        string activePath = journal.activePath;
        string restoreTemp = Path.Combine(activeDir,
            ".curriculum.restore." + Guid.NewGuid().ToString("N") + ".tmp");

        if (!VerifyWritePath(restoreTemp, allowRoot, out error))
        {
            return false;
        }

        try
        {
            using (FileStream source = new FileStream(journal.backupPath, FileMode.Open, FileAccess.Read,
                       FileShare.Read))
            using (FileStream destination = new FileStream(restoreTemp, FileMode.CreateNew, FileAccess.Write,
                       FileShare.None))
            {
                source.CopyTo(destination);
                destination.Flush(true);
            }

            if (!string.Equals(Sha256File(restoreTemp), journal.oldActiveSha256, StringComparison.OrdinalIgnoreCase))
            {
                error = "Restore temp tidak cocok dengan hash lama.";
                TryDeleteFile(restoreTemp);
                return false;
            }

            if (!VerifyWritePath(activePath, allowRoot, out error))
            {
                TryDeleteFile(restoreTemp);
                return false;
            }

            if (File.Exists(activePath))
            {
                error = "Target aktif muncul kembali saat rollback; dibatalkan.";
                TryDeleteFile(restoreTemp);
                return false;
            }

            File.Move(restoreTemp, activePath);
            if (!string.Equals(Sha256File(activePath), journal.oldActiveSha256, StringComparison.OrdinalIgnoreCase))
            {
                error = "Rollback terpasang tetapi hash lama tidak cocok.";
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            error = "Restore dari backup gagal: " + exception.Message;
            TryDeleteFile(restoreTemp);
            return false;
        }
    }

    private static void MarkRecoveryRequired(string stateDir, Journal journal)
    {
        journal.state = StateRecoveryRequired;
        WriteJournalAtomic(stateDir, journal, out _);
    }

    private static bool IsHexSha256(string value)
    {
        if (value == null || value.Length != 64)
        {
            return false;
        }

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!hex)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSafeTransactionId(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 64)
        {
            return false;
        }

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool safe = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') ||
                        (c >= 'A' && c <= 'Z') || c == '-' || c == '_';
            if (!safe)
            {
                return false;
            }
        }

        return true;
    }

    // ---------------------------------------------------------------- locking

    public sealed class ImportLock : IDisposable
    {
        private FileStream stream;

        internal ImportLock(string path, FileStream stream)
        {
            Path = path;
            this.stream = stream;
        }

        public string Path { get; }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }

    public static string LockPath(string stateDir)
    {
        return Path.Combine(stateDir, "import.lock");
    }

    /// <summary>
    /// Ambil import.lock dengan FileShare.None. Hanya ERROR_SHARING_VIOLATION
    /// (HResult &amp; 0xFFFF == 32) yang berarti lock aktif; error lain fail closed.
    /// </summary>
    public static bool TryAcquireLock(string stateDir, out ImportLock lockHandle, out bool heldByOther,
        out string error)
    {
        lockHandle = null;
        heldByOther = false;
        error = null;

        string path = LockPath(stateDir);
        if (!VerifyWritePath(path, stateDir, out error))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(stateDir);
            if (!VerifyWritePath(path, stateDir, out error))
            {
                return false;
            }

            FileStream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.None);
            lockHandle = new ImportLock(path, stream);
            return true;
        }
        catch (IOException exception) when ((exception.HResult & 0xFFFF) == 32)
        {
            heldByOther = true;
            error = "Import sedang berjalan (import.lock dipegang proses lain).";
            return false;
        }
        catch (Exception exception)
        {
            error = "Gagal mengambil import.lock: " + exception.Message;
            return false;
        }
    }

    /// <summary>
    /// Wrapper recovery produksi: mengambil import.lock yang sama dengan importer,
    /// menjalankan state machine, lalu melepas lock.
    /// </summary>
    public static bool Recover(string allowRoot, string stateDir, string activeDir, string stagingRoot,
        out RecoveryOutcome outcome, out string error)
    {
        outcome = RecoveryOutcome.RecoveryRequired;
        ImportLock held;
        bool heldByOther;
        if (!TryAcquireLock(stateDir, out held, out heldByOther, out error))
        {
            if (!heldByOther)
            {
                outcome = RecoveryOutcome.RecoveryRequired;
            }

            return false;
        }

        using (held)
        {
            return RecoverLocked(allowRoot, stateDir, activeDir, stagingRoot, out outcome, out error);
        }
    }

    // ---------------------------------------------------------------- self-test

    /// <summary>
    /// Self-test path guard (Tahap 2). Jalankan:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod CurriculumImporter.PathGuardSelfTest
    /// </summary>
    public static void PathGuardSelfTest()
    {
        string sandbox = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_SelfTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        int total = 0;
        int passed = 0;
        int notRun = 0;

        try
        {
            string allow = Path.Combine(sandbox, "Allow");
            Directory.CreateDirectory(allow);
            string inside = Path.Combine(allow, "sub", "curriculum.json");
            Directory.CreateDirectory(Path.GetDirectoryName(inside));

            // T8/T57: traversal ditolak.
            total++; passed += Expect("T8 traversal ditolak", !IsWithin(allow, Path.Combine(allow, "..", "outside.json")));
            total++; passed += Expect("T57 traversal VerifyWritePath", !VerifyWritePath(Path.Combine(allow, "..", "outside.json"), allow, out _));

            // T69: prefix nama mirip ditolak.
            total++; passed += Expect("T69 prefix mirip ditolak", !IsWithin(Path.Combine(sandbox, "Curriculum"), Path.Combine(sandbox, "CurriculumX", "f.json")));

            // Path normal diterima.
            total++; passed += Expect("P path normal diterima", IsWithin(allow, inside) && VerifyWritePath(inside, allow, out _));

            // T73-T77/T30/T58/T59/T70/T71/T79/T80: reparse point.
            string realDir = Path.Combine(sandbox, "RealDir");
            Directory.CreateDirectory(realDir);
            string junction = Path.Combine(allow, "link");
            bool junctionCreated = TryCreateJunction(junction, realDir);

            if (junctionCreated)
            {
                total++; passed += Expect("T30 junction keluar ditolak", !VerifyAncestors(Path.Combine(junction, "f.json"), out _));
                total++; passed += Expect("T59 junction escape ditolak", !VerifyWritePath(Path.Combine(junction, "f.json"), allow, out _));
                total++; passed += Expect("T70 parent reparse ditolak", !VerifyAncestors(Path.Combine(junction, "f.json"), out _));
                total++; passed += Expect("T73 trusted root junction ditolak", !VerifyAncestors(junction, out _));

                // T60/T72: path berubah setelah validasi -> validasi ulang gagal.
                string changePath = Path.Combine(allow, "change", "f.json");
                Directory.CreateDirectory(Path.GetDirectoryName(changePath));
                bool firstOk = VerifyWritePath(changePath, allow, out _);
                Directory.Delete(Path.GetDirectoryName(changePath), true);
                bool junction2 = TryCreateJunction(Path.GetDirectoryName(changePath), realDir);
                if (junction2)
                {
                    total++; passed += Expect("T60 path berubah ditolak", firstOk && !VerifyWritePath(changePath, allow, out _));
                    total++; passed += Expect("T72 revalidasi sebelum tulis ditolak", !VerifyWritePath(changePath, allow, out _));
                }
                else
                {
                    notRun += 2;
                }
            }
            else
            {
                notRun += 8;
            }

            // T82: UNC ditolak.
            total++; passed += Expect("T82 UNC ditolak", !VerifyAncestors("\\\\server\\share\\file.json", out _));

            // T81 didelegasikan ke PathGuardAccessDeniedSelfTest (ACL nyata).

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[PathGuardSelfTest] RESULT: {result} {passed}/{total} | NOT RUN: {notRun}");
        }
        finally
        {
            TryDelete(sandbox);
        }
    }

    /// <summary>
    /// T81: access denied nyata pada sandbox temp. ACL hanya diterapkan ke folder
    /// sementara dan selalu dicabut; proyek tidak disentuh. Jika ACL tidak efektif,
    /// hasilnya NOT RUN (bukan PASS).
    /// </summary>
    public static void PathGuardAccessDeniedSelfTest()
    {
        string sandbox = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_ACL", Guid.NewGuid().ToString("N"));
        string blocked = Path.Combine(sandbox, "blocked");
        string probe = Path.Combine(blocked, "probe.json");
        Directory.CreateDirectory(blocked);
        File.WriteAllText(probe, "probe", new UTF8Encoding(false));

        bool aclApplied = false;
        int total = 0;
        int passed = 0;
        string output;
        string user = null;

        try
        {
            try
            {
                user = WindowsIdentity.GetCurrent()?.Name;
            }
            catch (Exception)
            {
                user = null;
            }

            if (string.IsNullOrEmpty(user))
            {
                Debug.Log("[PathGuardAccessDeniedSelfTest] RESULT: NOT RUN (identitas Windows tidak tersedia)");
                return;
            }

            string denyArgs = "\"" + blocked + "\" /deny \"" + user + ":(OI)(CI)(R)\"";
            int denyExit = RunProcess("icacls.exe", denyArgs, out output);
            if (denyExit != 0)
            {
                Debug.Log("[PathGuardAccessDeniedSelfTest] RESULT: NOT RUN (icacls deny gagal: " + output.Trim() + ")");
                return;
            }

            aclApplied = true;

            string error;
            total++;
            passed += Expect("T81 VerifyAncestors access denied fail-closed",
                !VerifyAncestors(probe, out error) && !string.IsNullOrEmpty(error));

            total++;
            passed += Expect("T81 VerifyWritePath access denied fail-closed",
                !VerifyWritePath(probe, sandbox, out error) && !string.IsNullOrEmpty(error));

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[PathGuardAccessDeniedSelfTest] RESULT: {result} {passed}/{total}");
        }
        finally
        {
            if (aclApplied)
            {
                RunProcess("icacls.exe", "\"" + blocked + "\" /remove:d \"" + user + "\"", out output);
                // Fallback: reset DACL bila remove deny tidak berhasil.
                RunProcess("icacls.exe", "\"" + blocked + "\" /reset", out output);
            }

            TryDelete(sandbox);
        }
    }

    private static int RunProcess(string fileName, string arguments, out string output)
    {
        output = string.Empty;
        try
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = Process.Start(info))
            {
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(15000))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                        // abaikan
                    }

                    output = "timeout";
                    return -1;
                }

                output = stdout + stderr;
                return process.ExitCode;
            }
        }
        catch (Exception exception)
        {
            output = exception.Message;
            return -1;
        }
    }

    /// <summary>Self-test journal atomik (Tahap 3): T43, T49, T103, T104, T106, T109.</summary>
    public static void JournalSelfTest()
    {
        string sandbox = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_SelfTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        int total = 0;
        int passed = 0;

        try
        {
            Journal journal = new Journal
            {
                transactionId = "tx1",
                state = StatePrepared,
                newContentSha256 = "abc",
                sourcePath = "source",
                activePath = "active",
                stagingPath = "staging",
                promotionTempPath = "promotion",
                backupPath = "backup",
                oldActiveExists = false,
                oldActiveSha256 = string.Empty,
                backupSha256 = string.Empty,
                timestamp = "2026-11-22T00:00:00Z"
            };

            string error;
            Journal read;

            total++; passed += Expect("T103 journal PREPARED (first write)", WriteJournalAtomic(sandbox, journal, out error));
            total++; passed += Expect("T103 baca PREPARED", TryReadJournal(sandbox, out read, out error) && read.state == StatePrepared);

            journal.state = StatePromoting;
            total++; passed += Expect("T103 journal PROMOTING (replace)", WriteJournalAtomic(sandbox, journal, out error) && TryReadJournal(sandbox, out read, out error) && read.state == StatePromoting);

            // Simulasi crash saat menulis journal: .tmp berisi COMMITTED, journal masih PROMOTING.
            File.WriteAllText(Path.Combine(sandbox, "journal.json.tmp"),
                JsonConvert.SerializeObject(new Journal { transactionId = "tx1", state = StateCommitted, newContentSha256 = "abc" }));
            total++; passed += Expect("T104 .tmp tidak authoritative",
                TryReadJournal(sandbox, out read, out error) && read.state == StatePromoting);

            // Hanya .tmp (tanpa journal) -> tidak ada authoritative.
            File.Delete(Path.Combine(sandbox, "journal.json"));
            total++; passed += Expect("T104 tanpa journal -> gagal", !TryReadJournal(sandbox, out read, out error));

            // Journal korup: dibaca gagal, file dipertahankan.
            string journalPath = Path.Combine(sandbox, "journal.json");
            File.WriteAllText(journalPath, "{ corrupt", new UTF8Encoding(false));
            string before = File.ReadAllText(journalPath);
            total++; passed += Expect("T106 journal korup ditolak", !TryReadJournal(sandbox, out read, out error));
            total++; passed += Expect("T106 journal korup dipertahankan", File.ReadAllText(journalPath) == before);

            // Tulis ulang setelah korup (replace) -> transaksi baru.
            Journal journal2 = new Journal
            {
                transactionId = "tx2",
                state = StatePrepared,
                newContentSha256 = "def",
                timestamp = "2026-11-22T00:01:00Z"
            };
            total++; passed += Expect("T103 tulis ulang setelah korup",
                WriteJournalAtomic(sandbox, journal2, out error) &&
                TryReadJournal(sandbox, out read, out error) && read.transactionId == "tx2");

            total++; passed += Expect("T109 journal idempoten",
                WriteJournalAtomic(sandbox, journal2, out error) && WriteJournalAtomic(sandbox, journal2, out error));

            // T43/T49: .tmp tertinggal -> journal lama tetap authoritative.
            File.WriteAllText(Path.Combine(sandbox, "journal.json.tmp"),
                "{\"transactionId\":\"tx2\",\"state\":\"RECOVERY_REQUIRED\"}");
            total++; passed += Expect("T43/T49 journal lama otoritatif",
                TryReadJournal(sandbox, out read, out error) && read.state == StatePrepared);

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[JournalSelfTest] RESULT: {result} {passed}/{total}");
        }
        finally
        {
            TryDelete(sandbox);
        }
    }

    /// <summary>Self-test backup/orphan/retensi (Tahap 4): T32, T33, T83–T93.</summary>
    public static void BackupSelfTest()
    {
        string sandbox = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_SelfTest", Guid.NewGuid().ToString("N"));
        string stateDir = Path.Combine(sandbox, "State");
        Directory.CreateDirectory(stateDir);
        Directory.CreateDirectory(Path.Combine(sandbox, "Active"));
        string activePath = Path.Combine(sandbox, "Active", "curriculum.json");
        File.WriteAllText(activePath, "{\"v\":1}", new UTF8Encoding(false));

        int total = 0;
        int passed = 0;

        try
        {
            string error;
            string backupPath;
            string sha;

            // T83: backup byte-identical + hash.
            total++; passed += Expect("T83 backup dibuat",
                CreateBackup(stateDir, "tx1", activePath, out backupPath, out sha, out error));
            total++; passed += Expect("T83 backup identik dengan aktif",
                File.ReadAllText(backupPath) == File.ReadAllText(activePath) &&
                IsBackupValid(backupPath, sha, out error));

            // T84: backup korup tidak valid.
            File.WriteAllText(backupPath, "{\"v\":999}", new UTF8Encoding(false));
            total++; passed += Expect("T84 backup korup ditolak", !IsBackupValid(backupPath, sha, out error));

            // Buat ulang backup valid.
            total++; passed += Expect("T83 backup ulang valid",
                CreateBackup(stateDir, "tx1", activePath, out backupPath, out sha, out error) &&
                IsBackupValid(backupPath, sha, out error));

            // T33/T34: backup hilang / hash mismatch.
            total++; passed += Expect("T32 backup hilang ditolak",
                !IsBackupValid(Path.Combine(sandbox, "tidak-ada.json"), sha, out error));
            total++; passed += Expect("T33 backup hash mismatch ditolak",
                !IsBackupValid(backupPath, "deadbeef", out error));

            // Catat tx1 sebagai COMMITTED.
            total++; passed += Expect("tx1 committed dicatat", RecordCommittedBackup(stateDir, "tx1", sha, out error));

            // T89: cleanup saat belum COMMITTED -> no-op.
            total++; passed += Expect("T89 cleanup saat PROMOTING no-op",
                CleanupBackups(stateDir, "tx1", false, out error) == 0);

            // T88: tx2 lebih baru -> tx1 lama dihapus.
            string sha2;
            total++; passed += Expect("tx2 backup dibuat",
                CreateBackup(stateDir, "tx2", activePath, out backupPath, out sha2, out error) &&
                RecordCommittedBackup(stateDir, "tx2", sha2, out error));
            int deleted = CleanupBackups(stateDir, "tx2", true, out error);
            total++; passed += Expect("T88 retensi hapus backup lama COMMITTED",
                deleted == 1 && !Directory.Exists(BackupDir(stateDir, "tx1")) &&
                Directory.Exists(BackupDir(stateDir, "tx2")));

            // T85/T90: orphan & file tak dikenal TIDAK dihapus.
            string orphanDir = BackupDir(stateDir, "txOrphan");
            Directory.CreateDirectory(orphanDir);
            File.WriteAllText(Path.Combine(orphanDir, "curriculum.json"), "orphan", new UTF8Encoding(false));
            string unknownDir = Path.Combine(stateDir, "Backup", "unknown");
            Directory.CreateDirectory(unknownDir);
            File.WriteAllText(Path.Combine(unknownDir, "x.bin"), "x", new UTF8Encoding(false));

            CleanupBackups(stateDir, "tx2", true, out error);
            total++; passed += Expect("T85 orphan tidak dihapus", Directory.Exists(orphanDir));
            total++; passed += Expect("T90 file tak dikenal tidak dihapus", Directory.Exists(unknownDir));

            // T91: idempoten.
            total++; passed += Expect("T91 cleanup idempoten", CleanupBackups(stateDir, "tx2", true, out error) == 0);

            // T92/T93: backup parsial (0 byte) tidak valid & tidak dihapus (tak tercatat).
            string partialDir = BackupDir(stateDir, "txPartial");
            Directory.CreateDirectory(partialDir);
            string partialPath = Path.Combine(partialDir, "curriculum.json");
            File.WriteAllBytes(partialPath, new byte[0]);
            total++; passed += Expect("T92 backup parsial tidak valid", !IsBackupValid(partialPath, sha2, out error));
            CleanupBackups(stateDir, "tx2", true, out error);
            total++; passed += Expect("T93 backup parsial tidak dihapus", File.Exists(partialPath));

            // T93b: index dimanipulasi (transactionId traversal) tidak boleh menghapus di luar Backup.
            string outsideDir = Path.Combine(stateDir, "outside");
            Directory.CreateDirectory(outsideDir);
            string outsideFile = Path.Combine(outsideDir, "curriculum.json");
            File.WriteAllText(outsideFile, "outside", new UTF8Encoding(false));
            string outsideSha = Sha256File(outsideFile);
            File.WriteAllText(Path.Combine(stateDir, "backup_index.json"),
                "{\"entries\":[{\"transactionId\":\"../outside\",\"sha256\":\"" + outsideSha +
                "\",\"committedAt\":\"2026-11-22T00:00:00Z\"}]}", new UTF8Encoding(false));
            CleanupBackups(stateDir, "tx2", true, out error);
            total++; passed += Expect("T93b index traversal tidak menghapus di luar", File.Exists(outsideFile));

            // T83: backup transaksi aktif tetap ada setelah cleanup.
            total++; passed += Expect("T88 backup aktif dipertahankan",
                File.Exists(BackupPath(stateDir, "tx2")));

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[BackupSelfTest] RESULT: {result} {passed}/{total}");
        }
        finally
        {
            TryDelete(sandbox);
        }
    }

    /// <summary>
    /// Self-test guard retensi (C2): backup current wajib valid sebelum backup lama dihapus.
    /// </summary>
    public static void RetentionGuardSelfTest()
    {
        string sandbox = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_Retention", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        string activePath = Path.Combine(sandbox, "curriculum.json");
        int total = 0;
        int passed = 0;
        int notRun = 0;

        string NewState(string name)
        {
            string dir = Path.Combine(sandbox, name);
            Directory.CreateDirectory(dir);
            return dir;
        }

        void WriteActive(string content)
        {
            File.WriteAllText(activePath, content, new UTF8Encoding(false));
        }

        bool MakeCommitted(string stateDir, string transactionId, out string path, out string sha)
        {
            string error;
            if (!CreateBackup(stateDir, transactionId, activePath, out path, out sha, out error))
            {
                Debug.LogError("[RetentionGuardSelfTest] setup backup gagal: " + error);
                return false;
            }

            return RecordCommittedBackup(stateDir, transactionId, sha, out error);
        }

        try
        {
            string error;
            string oldPath;
            string oldSha;
            string currentPath;
            string currentSha;

            // R1/R8: current valid -> backup lama valid dihapus, current/orphan/unknown tetap.
            string normal = NewState("normal");
            WriteActive("{\"v\":1}");
            total++; passed += Expect("R1 backup lama valid",
                MakeCommitted(normal, "old", out oldPath, out oldSha));
            WriteActive("{\"v\":2}");
            total++; passed += Expect("R1 backup current valid",
                MakeCommitted(normal, "current", out currentPath, out currentSha));
            string orphanDir = Path.Combine(normal, "Backup", "orphan");
            string unknownPath = Path.Combine(normal, "Backup", "unknown.bin");
            Directory.CreateDirectory(orphanDir);
            File.WriteAllText(Path.Combine(orphanDir, "curriculum.json"), "orphan", new UTF8Encoding(false));
            File.WriteAllText(unknownPath, "unknown", new UTF8Encoding(false));
            int deleted = CleanupBackups(normal, "current", true, out error);
            total++; passed += Expect("R1 cleanup normal setelah current valid",
                deleted == 1 && !File.Exists(oldPath) && File.Exists(currentPath) &&
                Directory.Exists(orphanDir) && File.Exists(unknownPath));

            // R2: current hilang -> abort total, backup lama tetap.
            string missing = NewState("missing");
            WriteActive("{\"v\":1}");
            total++; passed += Expect("R2 backup lama missing",
                MakeCommitted(missing, "old", out oldPath, out oldSha));
            WriteActive("{\"v\":2}");
            total++; passed += Expect("R2 backup current missing",
                MakeCommitted(missing, "current", out currentPath, out currentSha));
            File.Delete(currentPath);
            deleted = CleanupBackups(missing, "current", true, out error);
            total++; passed += Expect("R2 current hilang -> cleanup abort",
                deleted == 0 && File.Exists(oldPath) && !string.IsNullOrEmpty(error));

            // R3: current korup -> abort total.
            string corrupt = NewState("corrupt");
            WriteActive("{\"v\":1}");
            total++; passed += Expect("R3 backup lama corrupt",
                MakeCommitted(corrupt, "old", out oldPath, out oldSha));
            WriteActive("{\"v\":2}");
            total++; passed += Expect("R3 backup current corrupt",
                MakeCommitted(corrupt, "current", out currentPath, out currentSha));
            File.WriteAllText(currentPath, "corrupt", new UTF8Encoding(false));
            deleted = CleanupBackups(corrupt, "current", true, out error);
            total++; passed += Expect("R3 current korup -> cleanup abort",
                deleted == 0 && File.Exists(oldPath) && !string.IsNullOrEmpty(error));

            // R4: current access denied -> abort; ACL hanya di file sandbox.
            string denied = NewState("denied");
            WriteActive("{\"v\":1}");
            total++; passed += Expect("R4 backup lama denied",
                MakeCommitted(denied, "old", out oldPath, out oldSha));
            WriteActive("{\"v\":2}");
            total++; passed += Expect("R4 backup current denied",
                MakeCommitted(denied, "current", out currentPath, out currentSha));

            string user = null;
            try
            {
                user = WindowsIdentity.GetCurrent()?.Name;
            }
            catch (Exception)
            {
                user = null;
            }

            bool denyApplied = false;
            if (!string.IsNullOrEmpty(user) &&
                RunProcess("icacls.exe", "\"" + currentPath + "\" /deny \"" + user + ":(R)\"", out _) == 0)
            {
                denyApplied = true;
                deleted = CleanupBackups(denied, "current", true, out error);
                total++; passed += Expect("R4 current access denied -> cleanup abort",
                    deleted == 0 && File.Exists(oldPath) && !string.IsNullOrEmpty(error));
                RunProcess("icacls.exe", "\"" + currentPath + "\" /remove:d \"" + user + "\"", out _);
                RunProcess("icacls.exe", "\"" + currentPath + "\" /reset", out _);
            }
            else
            {
                notRun++;
                Debug.Log("[RetentionGuardSelfTest] NOT RUN R4 (ACL deny tidak tersedia/efektif)");
            }

            // R6: backup lama korup -> tidak dihapus.
            string oldCorrupt = NewState("oldcorrupt");
            WriteActive("{\"v\":1}");
            total++; passed += Expect("R6 backup lama corrupt disiapkan",
                MakeCommitted(oldCorrupt, "old", out oldPath, out oldSha));
            File.WriteAllText(oldPath, "corrupt", new UTF8Encoding(false));
            WriteActive("{\"v\":2}");
            total++; passed += Expect("R6 backup current valid",
                MakeCommitted(oldCorrupt, "current", out currentPath, out currentSha));
            deleted = CleanupBackups(oldCorrupt, "current", true, out error);
            total++; passed += Expect("R6 backup lama korup tidak dihapus",
                deleted == 0 && File.Exists(oldPath) && File.Exists(currentPath));

            // R7: current tidak tercatat -> abort; backup lama tetap.
            string unindexed = NewState("unindexed");
            WriteActive("{\"v\":1}");
            total++; passed += Expect("R7 backup lama unindexed",
                MakeCommitted(unindexed, "old", out oldPath, out oldSha));
            WriteActive("{\"v\":2}");
            total++; passed += Expect("R7 current tidak tercatat",
                CreateBackup(unindexed, "current", activePath, out currentPath, out currentSha, out error));
            deleted = CleanupBackups(unindexed, "current", true, out error);
            total++; passed += Expect("R7 current tidak tercatat -> cleanup abort",
                deleted == 0 && File.Exists(oldPath) && File.Exists(currentPath));

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[RetentionGuardSelfTest] RESULT: {result} {passed}/{total} | NOT RUN: {notRun}");
        }
        finally
        {
            TryDelete(sandbox);
        }
    }

    /// <summary>Self-test promosi (Tahap 5): T36–T42, T110, T111.</summary>
    public static void PromotionSelfTest()
    {
        string sandbox = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_SelfTest", Guid.NewGuid().ToString("N"));
        string activeDir = Path.Combine(sandbox, "Resources", "Curriculum");
        Directory.CreateDirectory(activeDir);
        string activePath = Path.Combine(activeDir, "curriculum.json");

        byte[] oldContent = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"locale\":\"id\",\"topics\":[]}");
        byte[] newContent = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"locale\":\"id\",\"topics\":[{\"id\":\"salam\"}]}");
        byte[] newerContent = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"locale\":\"id\",\"topics\":[{\"id\":\"salam\"},{\"id\":\"keluarga\"}]}");

        int total = 0;
        int passed = 0;

        try
        {
            string error;
            string tempPath;
            string tempSha;

            // T36 first-run: target belum ada -> File.Move.
            total++; passed += Expect("T36 temp promosi disiapkan",
                PreparePromotionTemp(sandbox, activeDir, newContent, out tempPath, out tempSha, out error) &&
                File.Exists(tempPath) && Path.GetDirectoryName(tempPath) == activeDir);
            total++; passed += Expect("T36 first-run promosi",
                Promote(sandbox, activeDir, activePath, tempPath, false, string.Empty, tempSha, out error) &&
                File.ReadAllBytes(activePath).SequenceEqual(newContent) && !File.Exists(tempPath));

            // T37 replace: target lama ada -> File.Replace.
            string oldSha = Sha256File(activePath);
            total++; passed += Expect("T37 temp replace disiapkan",
                PreparePromotionTemp(sandbox, activeDir, newerContent, out tempPath, out tempSha, out error));
            total++; passed += Expect("T37 replace promosi",
                Promote(sandbox, activeDir, activePath, tempPath, true, oldSha, tempSha, out error) &&
                File.ReadAllBytes(activePath).SequenceEqual(newerContent) && !File.Exists(tempPath));

            // T38 first-run: target muncul setelah cek -> batal, target tidak berubah.
            string firstRunDir = Path.Combine(sandbox, "FirstRun");
            Directory.CreateDirectory(firstRunDir);
            string firstRunPath = Path.Combine(firstRunDir, "curriculum.json");
            total++; passed += Expect("T38 temp first-run disiapkan",
                PreparePromotionTemp(sandbox, firstRunDir, newContent, out tempPath, out tempSha, out error));
            File.WriteAllBytes(firstRunPath, oldContent);
            total++; passed += Expect("T38 target muncul -> batal",
                !Promote(sandbox, firstRunDir, firstRunPath, tempPath, false, string.Empty, tempSha, out error) &&
                File.ReadAllBytes(firstRunPath).SequenceEqual(oldContent));
            total++; passed += Expect("T38 temp dipertahankan saat batal", File.Exists(tempPath));
            TryDeleteFile(tempPath);

            // T39 hash aktif berubah -> batal.
            byte[] changedActive = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"locale\":\"id\",\"topics\":[{\"id\":\"luar\"}]}");
            File.WriteAllBytes(activePath, changedActive);
            total++; passed += Expect("T39 temp hash lama disiapkan",
                PreparePromotionTemp(sandbox, activeDir, newContent, out tempPath, out tempSha, out error));
            total++; passed += Expect("T39 hash aktif berubah -> batal",
                !Promote(sandbox, activeDir, activePath, tempPath, true, oldSha, tempSha, out error) &&
                File.ReadAllBytes(activePath).SequenceEqual(changedActive));
            TryDeleteFile(tempPath);

            // T40 temp dirusak -> batal.
            total++; passed += Expect("T40 temp disiapkan",
                PreparePromotionTemp(sandbox, activeDir, newContent, out tempPath, out tempSha, out error));
            File.WriteAllBytes(tempPath, newerContent);
            total++; passed += Expect("T40 temp hash mismatch -> batal",
                !Promote(sandbox, activeDir, activePath, tempPath, true, Sha256File(activePath), tempSha, out error) &&
                File.ReadAllBytes(activePath).SequenceEqual(changedActive));
            TryDeleteFile(tempPath);

            // T41 temp di luar direktori aktif -> ditolak.
            string outsideDir = Path.Combine(sandbox, "Outside");
            Directory.CreateDirectory(outsideDir);
            total++; passed += Expect("T41 temp luar disiapkan",
                PreparePromotionTemp(sandbox, outsideDir, newContent, out tempPath, out tempSha, out error));
            total++; passed += Expect("T41 temp beda direktori ditolak",
                !Promote(sandbox, activeDir, activePath, tempPath, true, Sha256File(activePath), tempSha, out error));
            TryDeleteFile(tempPath);

            // T110 temp tidak ada -> ditolak.
            total++; passed += Expect("T110 temp tidak ada ditolak",
                !Promote(sandbox, activeDir, activePath, Path.Combine(activeDir, "tidak-ada.tmp"),
                    true, Sha256File(activePath), Sha256Bytes(newContent), out error));

            // T111 hash baru kosong/tidak cocok -> ditolak.
            total++; passed += Expect("T111 temp baru disiapkan",
                PreparePromotionTemp(sandbox, activeDir, newContent, out tempPath, out tempSha, out error));
            total++; passed += Expect("T111 hash baru salah ditolak",
                !Promote(sandbox, activeDir, activePath, tempPath, true, Sha256File(activePath),
                    "deadbeef", out error) &&
                File.ReadAllBytes(activePath).SequenceEqual(changedActive));
            TryDeleteFile(tempPath);

            // T42 tidak ada sisa temp di direktori aktif setelah transaksi sukses.
            string[] leftovers = Directory.GetFiles(activeDir, ".curriculum.*.tmp");
            total++; passed += Expect("T42 tidak ada temp tertinggal", leftovers.Length == 0);

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[PromotionSelfTest] RESULT: {result} {passed}/{total}");
        }
        finally
        {
            TryDelete(sandbox);
        }
    }

    /// <summary>
    /// Self-test recovery (Tahap 6): state machine, rollback dari backup, dan fail-closed.
    /// lock diuji terpisah pada Tahap 7; semua pemanggilan di sini adalah state machine murni.
    /// </summary>
    public static void RecoverySelfTest()
    {
        string sandbox = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_SelfTest", Guid.NewGuid().ToString("N"));
        string activeDir = Path.Combine(sandbox, "Resources", "Curriculum");
        string stagingRoot = Path.Combine(sandbox, "Staging");
        string statesRoot = Path.Combine(sandbox, "States");
        Directory.CreateDirectory(activeDir);
        Directory.CreateDirectory(stagingRoot);
        Directory.CreateDirectory(statesRoot);
        string activePath = Path.Combine(activeDir, "curriculum.json");

        byte[] oldContent = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"locale\":\"id\",\"topics\":[]}");
        byte[] newContent = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"locale\":\"id\",\"topics\":[{\"id\":\"salam\"}]}");
        byte[] thirdContent = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"locale\":\"id\",\"topics\":[{\"id\":\"asing\"}]}");

        int total = 0;
        int passed = 0;

        string NewState(string name)
        {
            string dir = Path.Combine(statesRoot, name);
            Directory.CreateDirectory(dir);
            return dir;
        }

        void WriteActive(byte[] content)
        {
            File.WriteAllBytes(activePath, content);
        }

        Journal NewJournal(string stateDir, string tx, string state, bool oldExists, string oldSha,
            string newSha, string backupPath, string backupSha, string tempPath)
        {
            return new Journal
            {
                transactionId = tx,
                state = state,
                oldActiveExists = oldExists,
                oldActiveSha256 = oldSha ?? string.Empty,
                newContentSha256 = newSha,
                backupPath = backupPath,
                backupSha256 = backupSha ?? string.Empty,
                promotionTempPath = tempPath ?? string.Empty,
                stagingPath = Path.Combine(stagingRoot, tx + ".json"),
                activePath = activePath,
                sourcePath = Path.Combine(sandbox, "source", "curriculum.json"),
                timestamp = "2026-11-22T00:00:00Z"
            };
        }

        try
        {
            RecoveryOutcome outcome;
            string error;

            // No journal -> no-op.
            string noJournal = NewState("nojournal");
            total++; passed += Expect("T44 tanpa journal no-op",
                RecoverLocked(sandbox, noJournal, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.NoJournal);

            // PREPARED + active lama -> rollback, temp dibersihkan, backup dipertahankan.
            WriteActive(oldContent);
            string statePrepared = NewState("prepared");
            string backupPath;
            string backupSha;
            string tempPath;
            string newSha;
            total++; passed += Expect("T44 backup prepared",
                CreateBackup(statePrepared, "txprep", activePath, out backupPath, out backupSha, out error));
            total++; passed += Expect("T44 temp prepared",
                PreparePromotionTemp(sandbox, activeDir, newContent, out tempPath, out newSha, out error));
            Journal jPrepared = NewJournal(statePrepared, "txprep", StatePrepared, true,
                Sha256File(activePath), newSha, backupPath, backupSha, tempPath);
            total++; passed += Expect("T44 journal prepared",
                WriteJournalAtomic(statePrepared, jPrepared, out error));
            total++; passed += Expect("T44 PREPARED rollback",
                RecoverLocked(sandbox, statePrepared, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RolledBack &&
                File.ReadAllBytes(activePath).SequenceEqual(oldContent) &&
                !File.Exists(tempPath) && File.Exists(backupPath));

            // T44b: PREPARED sudah mencatat temp path, tetapi temp belum sempat dibuat.
            string stateTracked = NewState("trackedtemp");
            WriteActive(oldContent);
            total++; passed += Expect("T44b backup tracked",
                CreateBackup(stateTracked, "txtracked", activePath, out backupPath, out backupSha, out error));
            string trackedTemp = Path.Combine(activeDir, ".curriculum.tracked.tmp");
            Journal jTracked = NewJournal(stateTracked, "txtracked", StatePrepared, true,
                Sha256File(activePath), Sha256Bytes(newContent), backupPath, backupSha, trackedTemp);
            total++; passed += Expect("T44b journal tracked",
                WriteJournalAtomic(stateTracked, jTracked, out error));
            total++; passed += Expect("T44b temp belum ada -> rollback aman",
                RecoverLocked(sandbox, stateTracked, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RolledBack && !File.Exists(trackedTemp) &&
                File.ReadAllBytes(activePath).SequenceEqual(oldContent));

            // PREPARED tetapi aktif sudah konten baru -> ambigu.
            string statePreparedNew = NewState("preparednew");
            WriteActive(oldContent);
            total++; passed += Expect("T45 backup prepared-new",
                CreateBackup(statePreparedNew, "txprepnew", activePath, out backupPath, out backupSha, out error));
            WriteActive(newContent);
            Journal jPreparedNew = NewJournal(statePreparedNew, "txprepnew", StatePrepared, true,
                backupSha, Sha256File(activePath), backupPath, backupSha, string.Empty);
            total++; passed += Expect("T45 journal prepared-new",
                WriteJournalAtomic(statePreparedNew, jPreparedNew, out error));
            total++; passed += Expect("T45 PREPARED + aktif baru -> RECOVERY_REQUIRED",
                !RecoverLocked(sandbox, statePreparedNew, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RecoveryRequired &&
                File.ReadAllBytes(activePath).SequenceEqual(newContent) &&
                TryReadJournal(statePreparedNew, out Journal marked, out _) &&
                marked.state == StateRecoveryRequired);

            // PROMOTING + aktif baru -> selesaikan COMMITTED.
            string statePromoted = NewState("promoted");
            WriteActive(oldContent);
            total++; passed += Expect("T46 backup promoted",
                CreateBackup(statePromoted, "txpromoted", activePath, out backupPath, out backupSha, out error));
            string oldSha = Sha256File(activePath);
            total++; passed += Expect("T46 temp promoted",
                PreparePromotionTemp(sandbox, activeDir, newContent, out tempPath, out newSha, out error));
            Journal jPromoted = NewJournal(statePromoted, "txpromoted", StatePromoting, true,
                oldSha, newSha, backupPath, backupSha, tempPath);
            total++; passed += Expect("T46 journal promoted",
                WriteJournalAtomic(statePromoted, jPromoted, out error));
            total++; passed += Expect("T46 promosi",
                Promote(sandbox, activeDir, activePath, tempPath, true, oldSha, newSha, out error));
            total++; passed += Expect("T46 PROMOTING + aktif baru -> COMMITTED",
                RecoverLocked(sandbox, statePromoted, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.Committed &&
                TryReadJournal(statePromoted, out Journal committed, out _) &&
                committed.state == StateCommitted);

            // PROMOTING + aktif lama -> rollback.
            string stateRollback = NewState("rollback");
            WriteActive(oldContent);
            total++; passed += Expect("T47 backup rollback",
                CreateBackup(stateRollback, "txrollback", activePath, out backupPath, out backupSha, out error));
            oldSha = Sha256File(activePath);
            total++; passed += Expect("T47 temp rollback",
                PreparePromotionTemp(sandbox, activeDir, newContent, out tempPath, out newSha, out error));
            Journal jRollback = NewJournal(stateRollback, "txrollback", StatePromoting, true,
                oldSha, newSha, backupPath, backupSha, tempPath);
            total++; passed += Expect("T47 journal rollback",
                WriteJournalAtomic(stateRollback, jRollback, out error));
            total++; passed += Expect("T47 PROMOTING + aktif lama -> rollback",
                RecoverLocked(sandbox, stateRollback, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RolledBack &&
                File.ReadAllBytes(activePath).SequenceEqual(oldContent) && !File.Exists(tempPath));

            // PROMOTING + aktif hilang -> restore dari backup.
            string stateRestore = NewState("restore");
            WriteActive(oldContent);
            total++; passed += Expect("T48 backup restore",
                CreateBackup(stateRestore, "txrestore", activePath, out backupPath, out backupSha, out error));
            oldSha = Sha256File(activePath);
            Journal jRestore = NewJournal(stateRestore, "txrestore", StatePromoting, true,
                oldSha, Sha256Bytes(newContent), backupPath, backupSha, string.Empty);
            total++; passed += Expect("T48 journal restore",
                WriteJournalAtomic(stateRestore, jRestore, out error));
            File.Delete(activePath);
            total++; passed += Expect("T48 aktif hilang -> restore backup",
                RecoverLocked(sandbox, stateRestore, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RolledBack &&
                File.Exists(activePath) &&
                string.Equals(Sha256File(activePath), oldSha, StringComparison.OrdinalIgnoreCase));

            // PROMOTING + konten ketiga -> ambiguity, aktif tidak disentuh.
            string stateThird = NewState("third");
            WriteActive(oldContent);
            total++; passed += Expect("T49 backup third",
                CreateBackup(stateThird, "txthird", activePath, out backupPath, out backupSha, out error));
            oldSha = Sha256File(activePath);
            Journal jThird = NewJournal(stateThird, "txthird", StatePromoting, true,
                oldSha, Sha256Bytes(newContent), backupPath, backupSha, string.Empty);
            total++; passed += Expect("T49 journal third",
                WriteJournalAtomic(stateThird, jThird, out error));
            WriteActive(thirdContent);
            total++; passed += Expect("T49 konten ketiga -> RECOVERY_REQUIRED",
                !RecoverLocked(sandbox, stateThird, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RecoveryRequired &&
                File.ReadAllBytes(activePath).SequenceEqual(thirdContent) &&
                TryReadJournal(stateThird, out Journal markedThird, out _) &&
                markedThird.state == StateRecoveryRequired);

            // Backup korup -> STOP, aktif tidak disentuh.
            string stateCorrupt = NewState("corruptbackup");
            WriteActive(oldContent);
            total++; passed += Expect("T50 backup corrupt",
                CreateBackup(stateCorrupt, "txcorrupt", activePath, out backupPath, out backupSha, out error));
            File.WriteAllText(backupPath, "corrupt", new UTF8Encoding(false));
            Journal jCorrupt = NewJournal(stateCorrupt, "txcorrupt", StatePromoting, true,
                Sha256File(activePath), Sha256Bytes(newContent), backupPath, backupSha, string.Empty);
            total++; passed += Expect("T50 journal corrupt",
                WriteJournalAtomic(stateCorrupt, jCorrupt, out error));
            total++; passed += Expect("T50 backup korup ditolak",
                !RecoverLocked(sandbox, stateCorrupt, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RecoveryRequired &&
                File.ReadAllBytes(activePath).SequenceEqual(oldContent));

            // Journal dengan activePath di luar allowlist -> ditolak.
            string stateOutside = NewState("outside");
            Journal jOutside = NewJournal(stateOutside, "txoutside", StatePromoting, false,
                string.Empty, Sha256Bytes(newContent), BackupPath(stateOutside, "txoutside"), string.Empty, string.Empty);
            jOutside.activePath = Path.Combine(sandbox, "Outside", "curriculum.json");
            total++; passed += Expect("T51 journal outside",
                WriteJournalAtomic(stateOutside, jOutside, out error));
            total++; passed += Expect("T51 activePath luar allowlist ditolak",
                !RecoverLocked(sandbox, stateOutside, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RecoveryRequired);

            // RECOVERY_REQUIRED -> tidak pernah dilanjutkan otomatis.
            string stateStuck = NewState("stuck");
            WriteActive(oldContent);
            total++; passed += Expect("T52 backup stuck",
                CreateBackup(stateStuck, "txstuck", activePath, out backupPath, out backupSha, out error));
            Journal jStuck = NewJournal(stateStuck, "txstuck", StateRecoveryRequired, true,
                Sha256File(activePath), Sha256Bytes(newContent),
                backupPath, backupSha, string.Empty);
            total++; passed += Expect("T52 journal stuck",
                WriteJournalAtomic(stateStuck, jStuck, out error));
            total++; passed += Expect("T52 RECOVERY_REQUIRED tidak dilanjutkan",
                !RecoverLocked(sandbox, stateStuck, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RecoveryRequired &&
                File.ReadAllBytes(activePath).SequenceEqual(oldContent));

            // COMMITTED + aktif baru -> sukses idempoten.
            string stateCommitted = NewState("committed");
            WriteActive(oldContent);
            total++; passed += Expect("T53 backup committed",
                CreateBackup(stateCommitted, "txcommitted", activePath, out backupPath, out backupSha, out error));
            oldSha = Sha256File(activePath);
            WriteActive(newContent);
            Journal jCommitted = NewJournal(stateCommitted, "txcommitted", StateCommitted, true,
                oldSha, Sha256File(activePath), backupPath, backupSha, string.Empty);
            total++; passed += Expect("T53 journal committed",
                WriteJournalAtomic(stateCommitted, jCommitted, out error));
            total++; passed += Expect("T53 COMMITTED konsisten",
                RecoverLocked(sandbox, stateCommitted, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.Committed);

            // COMMITTED tetapi aktif lama -> ambigu.
            string stateCommittedOld = NewState("committedold");
            WriteActive(oldContent);
            total++; passed += Expect("T54 backup committed-old",
                CreateBackup(stateCommittedOld, "txcommittedold", activePath, out backupPath, out backupSha, out error));
            Journal jCommittedOld = NewJournal(stateCommittedOld, "txcommittedold", StateCommitted, true,
                backupSha, Sha256Bytes(newContent), backupPath, backupSha, string.Empty);
            total++; passed += Expect("T54 journal committed-old",
                WriteJournalAtomic(stateCommittedOld, jCommittedOld, out error));
            total++; passed += Expect("T54 COMMITTED + aktif lama -> RECOVERY_REQUIRED",
                !RecoverLocked(sandbox, stateCommittedOld, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RecoveryRequired &&
                File.ReadAllBytes(activePath).SequenceEqual(oldContent));

            // First-run (tanpa file lama) + belum promosi -> rollback tanpa membuat file.
            string stateFirst = NewState("first");
            if (File.Exists(activePath))
            {
                File.Delete(activePath);
            }

            Journal jFirst = NewJournal(stateFirst, "txfirst", StatePromoting, false,
                string.Empty, Sha256Bytes(newContent), BackupPath(stateFirst, "txfirst"), string.Empty, string.Empty);
            total++; passed += Expect("T55 journal first-run",
                WriteJournalAtomic(stateFirst, jFirst, out error));
            total++; passed += Expect("T55 first-run rollback tanpa membuat target",
                RecoverLocked(sandbox, stateFirst, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.RolledBack && !File.Exists(activePath));

            // T56: journal dengan path tidak valid tidak boleh melempar exception.
            string stateMalformed = NewState("malformed");
            Journal jMalformed = NewJournal(stateMalformed, "txmalformed", StatePromoting, false,
                string.Empty, Sha256Bytes(newContent), BackupPath(stateMalformed, "txmalformed"),
                string.Empty, string.Empty);
            jMalformed.activePath = activePath + "\0invalid";
            total++; passed += Expect("T56 journal malformed ditulis",
                WriteJournalAtomic(stateMalformed, jMalformed, out error));
            bool threw = false;
            bool rejected;
            try
            {
                rejected = !RecoverLocked(sandbox, stateMalformed, activeDir, stagingRoot, out outcome,
                    out error) && outcome == RecoveryOutcome.RecoveryRequired;
            }
            catch (Exception)
            {
                threw = true;
                rejected = false;
            }

            total++; passed += Expect("T56 malformed journal fail-closed tanpa exception",
                !threw && rejected);

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[RecoverySelfTest] RESULT: {result} {passed}/{total}");
        }
        finally
        {
            TryDelete(sandbox);
        }
    }

    /// <summary>Self-test locking (Tahap 7): T61–T68, termasuk T64 lintas-proses dengan handshake.</summary>
    public static void LockSelfTest()
    {
        string sandbox = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_SelfTest", Guid.NewGuid().ToString("N"));
        string stateDir = Path.Combine(sandbox, "State");
        string activeDir = Path.Combine(sandbox, "Active");
        string stagingRoot = Path.Combine(sandbox, "Staging");
        Directory.CreateDirectory(stateDir);
        Directory.CreateDirectory(activeDir);
        Directory.CreateDirectory(stagingRoot);

        int total = 0;
        int passed = 0;

        try
        {
            string error;
            ImportLock first;
            bool heldByOther;

            // T61 acquire/release/reacquire.
            total++; passed += Expect("T61 lock diambil",
                TryAcquireLock(stateDir, out first, out heldByOther, out error) && first != null && !heldByOther);
            first.Dispose();
            ImportLock second;
            total++; passed += Expect("T61 lock dilepas dan bisa diambil lagi",
                TryAcquireLock(stateDir, out second, out heldByOther, out error) && second != null);

            // T62 lock kedua saat masih dipegang -> ditolak sebagai heldByOther.
            ImportLock blocked;
            total++; passed += Expect("T62 lock kedua ditolak",
                !TryAcquireLock(stateDir, out blocked, out heldByOther, out error) && heldByOther);
            second.Dispose();

            // T63 file lock boleh tertinggal; kepemilikan hanya dari handle.
            total++; passed += Expect("T63 file lock tertinggal bukan lock aktif",
                File.Exists(LockPath(stateDir)) &&
                TryAcquireLock(stateDir, out second, out heldByOther, out error));
            second.Dispose();

            // T66 recovery memakai lock yang sama.
            total++; passed += Expect("T66 lock untuk recovery",
                TryAcquireLock(stateDir, out second, out heldByOther, out error));
            RecoveryOutcome outcome;
            total++; passed += Expect("T66 recovery ditolak saat lock dipegang",
                !Recover(sandbox, stateDir, activeDir, stagingRoot, out outcome, out error));
            second.Dispose();
            total++; passed += Expect("T66 recovery berjalan setelah lock dilepas",
                Recover(sandbox, stateDir, activeDir, stagingRoot, out outcome, out error) &&
                outcome == RecoveryOutcome.NoJournal);

            // T67 error selain sharing violation fail closed (bukan dianggap lock aktif).
            string badState = Path.Combine(sandbox, "BadState");
            Directory.CreateDirectory(Path.Combine(badState, "import.lock"));
            total++; passed += Expect("T67 error non-sharing fail closed",
                !TryAcquireLock(badState, out blocked, out heldByOther, out error) && !heldByOther &&
                !string.IsNullOrEmpty(error));

            // T64 lintas-proses: child memegang lock, handshake LOCK_ACQUIRED.
            string lockPath = LockPath(stateDir);
            string readyFlag = Path.Combine(sandbox, "release.flag");
            string scriptPath = Path.Combine(sandbox, "hold-lock.ps1");
            string escapedLock = lockPath.Replace("'", "''");
            string escapedRelease = readyFlag.Replace("'", "''");
            File.WriteAllText(scriptPath,
                "$ErrorActionPreference = 'Stop'\n" +
                "$fs = [System.IO.File]::Open('" + escapedLock + "', " +
                "[System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, " +
                "[System.IO.FileShare]::None)\n" +
                "[Console]::Out.WriteLine('LOCK_ACQUIRED')\n" +
                "[Console]::Out.Flush()\n" +
                "$deadline = [DateTime]::UtcNow.AddSeconds(20)\n" +
                "while (-not (Test-Path -LiteralPath '" + escapedRelease + "')) {\n" +
                "  if ([DateTime]::UtcNow -gt $deadline) { break }\n" +
                "  Start-Sleep -Milliseconds 50\n" +
                "}\n" +
                "$fs.Dispose()\n", new UTF8Encoding(false));

            Process child = StartPowerShell(scriptPath);
            bool handshake = WaitForLine(child, "LOCK_ACQUIRED", 15000);
            total++; passed += Expect("T64 child memegang lock (handshake)", handshake);

            ImportLock duringChild;
            total++; passed += Expect("T64 lock lintas-proses terdeteksi",
                !TryAcquireLock(stateDir, out duringChild, out heldByOther, out error) && heldByOther);

            File.WriteAllText(readyFlag, "release", new UTF8Encoding(false));
            bool exited = child.WaitForExit(15000);
            if (!exited)
            {
                try
                {
                    child.Kill();
                }
                catch (Exception)
                {
                    // abaikan
                }
            }

            total++; passed += Expect("T64 child keluar", exited);
            child.Dispose();

            ImportLock afterChild;
            total++; passed += Expect("T64 lock tersedia setelah child keluar",
                TryAcquireLock(stateDir, out afterChild, out heldByOther, out error) && afterChild != null);
            afterChild.Dispose();

            // T68 crash child (exit tanpa dispose) -> OS melepas lock.
            string crashScript = Path.Combine(sandbox, "crash-lock.ps1");
            File.WriteAllText(crashScript,
                "$fs = [System.IO.File]::Open('" + escapedLock + "', " +
                "[System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, " +
                "[System.IO.FileShare]::None)\n" +
                "[Console]::Out.WriteLine('LOCK_ACQUIRED')\n" +
                "[Console]::Out.Flush()\n" +
                "[Environment]::Exit(7)\n", new UTF8Encoding(false));

            Process crashChild = StartPowerShell(crashScript);
            bool crashHandshake = WaitForLine(crashChild, "LOCK_ACQUIRED", 15000);
            bool crashExited = crashChild.WaitForExit(15000);
            crashChild.Dispose();
            total++; passed += Expect("T68 crash child handshake + exit", crashHandshake && crashExited);

            ImportLock afterCrash;
            total++; passed += Expect("T68 lock dilepas OS setelah crash",
                TryAcquireLock(stateDir, out afterCrash, out heldByOther, out error) && afterCrash != null);
            afterCrash.Dispose();

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[LockSelfTest] RESULT: {result} {passed}/{total}");
        }
        finally
        {
            TryDelete(sandbox);
        }
    }

    private static Process StartPowerShell(string scriptPath)
    {
        ProcessStartInfo info = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + scriptPath + "\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        return Process.Start(info);
    }

    private static bool WaitForLine(Process process, string expected, int timeoutMilliseconds)
    {
        Task<string> read = Task.Run(() => process.StandardOutput.ReadLine());
        if (!read.Wait(timeoutMilliseconds))
        {
            return false;
        }

        return string.Equals(read.Result, expected, StringComparison.Ordinal);
    }

    /// <summary>Self-test importer end-to-end (Tahap 8): no-op, staging, backup, promosi, lock, recovery, orphan.</summary>
    public static void ImporterSelfTest()
    {
        string sandbox = Path.Combine(Path.GetTempPath(), "UabMeto_Stage2_SelfTest", Guid.NewGuid().ToString("N"));
        string activeDir = Path.Combine(sandbox, "Assets", "Resources", "Curriculum");
        string stateDir = Path.Combine(sandbox, "Library", "UabMetoStage2");
        string stagingDir = Path.Combine(sandbox, "Temp", "Staging");
        string audioRoot = Path.Combine(sandbox, "Audio");
        string sourcePath = Path.Combine(sandbox, "Assets", "Curriculum", "source", "curriculum.json");
        string activePath = Path.Combine(activeDir, "curriculum.json");
        Directory.CreateDirectory(audioRoot);

        LearningDatabase db = ScriptableObject.CreateInstance<LearningDatabase>();
        Config config = new Config
        {
            ProjectRoot = sandbox,
            ActiveDir = activeDir,
            StateDir = stateDir,
            StagingDir = stagingDir,
            AudioRoot = audioRoot,
            SourcePath = sourcePath,
            Database = db
        };

        int total = 0;
        int passed = 0;

        try
        {
            ImportOutcome outcome;
            string message;

            // Sumber absen: NO-OP tanpa membuat state/staging/output.
            total++; passed += Expect("T112 sumber absen no-op",
                Import(config, out outcome, out message) && outcome == ImportOutcome.NoOp);
            total++; passed += Expect("T112 tidak membuat state/active/staging",
                !Directory.Exists(stateDir) && !Directory.Exists(activeDir) &&
                !Directory.Exists(stagingDir));

            // Impor pertama.
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath) ?? sandbox);
            byte[] first = CurriculumJson.SerializeUtf8(BuildImporterRoot("salam", false));
            File.WriteAllBytes(sourcePath, first);
            total++; passed += Expect("T113 impor pertama",
                Import(config, out outcome, out message) && outcome == ImportOutcome.Imported);
            total++; passed += Expect("T113 output identik dengan sumber",
                File.Exists(activePath) && File.ReadAllBytes(activePath).SequenceEqual(first));

            Journal journal;
            total++; passed += Expect("T113 journal COMMITTED",
                TryReadJournal(stateDir, out journal, out _) && journal.state == StateCommitted);
            total++; passed += Expect("T113 tidak ada temporal .tmp",
                !File.Exists(JournalPath(stateDir) + ".tmp"));
            total++; passed += Expect("T113 tidak ada temp promosi tertinggal",
                Directory.GetFiles(activeDir, ".curriculum.*.tmp").Length == 0);
            total++; passed += Expect("T113 staging dibersihkan",
                !File.Exists(journal.stagingPath));

            // Sumber identik: UP-TO-DATE.
            total++; passed += Expect("T114 sumber identik up-to-date",
                Import(config, out outcome, out message) && outcome == ImportOutcome.UpToDate);

            // Impor kedua (backup) + backup identik.
            byte[] second = CurriculumJson.SerializeUtf8(BuildImporterRoot("keluarga", true));
            File.WriteAllBytes(sourcePath, second);
            byte[] oldActive = File.ReadAllBytes(activePath);
            string oldSha = Sha256Bytes(oldActive);
            total++; passed += Expect("T115 impor kedua",
                Import(config, out outcome, out message) && outcome == ImportOutcome.Imported);
            total++; passed += Expect("T115 output baru",
                File.ReadAllBytes(activePath).SequenceEqual(second));
            total++; passed += Expect("T115 journal transaksi kedua",
                TryReadJournal(stateDir, out journal, out _) && journal.state == StateCommitted &&
                journal.oldActiveExists);
            total++; passed += Expect("T115 backup identik byte-per-byte",
                File.Exists(journal.backupPath) &&
                string.Equals(Sha256File(journal.backupPath), oldSha, StringComparison.OrdinalIgnoreCase) &&
                File.ReadAllBytes(journal.backupPath).SequenceEqual(oldActive));
            total++; passed += Expect("T115 backup tercatat di index",
                File.Exists(Path.Combine(stateDir, "backup_index.json")));

            // Orphan/unknown tidak dihapus saat retensi.
            string orphanDir = Path.Combine(stateDir, "Backup", "orphan-tx");
            Directory.CreateDirectory(orphanDir);
            string orphanFile = Path.Combine(orphanDir, "curriculum.json");
            File.WriteAllText(orphanFile, "orphan", new UTF8Encoding(false));
            string unknownFile = Path.Combine(stateDir, "Backup", "unknown.bin");
            File.WriteAllText(unknownFile, "unknown", new UTF8Encoding(false));
            File.WriteAllBytes(sourcePath, CurriculumJson.SerializeUtf8(BuildImporterRoot("salam", true)));
            total++; passed += Expect("T116 impor ketiga",
                Import(config, out outcome, out message) && outcome == ImportOutcome.Imported);
            total++; passed += Expect("T116 orphan backup dipertahankan",
                File.Exists(orphanFile) && File.Exists(unknownFile));

            // Sumber invalid: fail closed, aktif tidak berubah.
            byte[] beforeInvalid = File.ReadAllBytes(activePath);
            File.WriteAllText(sourcePath,
                "{\"schemaVersion\":1,\"schemaVersion\":2,\"topics\":[]}", new UTF8Encoding(false));
            total++; passed += Expect("T117 sumber duplikat key ditolak",
                !Import(config, out outcome, out message) && outcome == ImportOutcome.Failed &&
                File.ReadAllBytes(activePath).SequenceEqual(beforeInvalid));

            // Sumber absen setelah impor: no-op.
            File.Delete(sourcePath);
            total++; passed += Expect("T117 sumber absen setelah impor no-op",
                Import(config, out outcome, out message) && outcome == ImportOutcome.NoOp &&
                File.ReadAllBytes(activePath).SequenceEqual(beforeInvalid));

            // Lock aktif: tidak ada perubahan.
            byte[] lockedSource = CurriculumJson.SerializeUtf8(BuildImporterRoot("tanah", true));
            File.WriteAllBytes(sourcePath, lockedSource);
            ImportLock held;
            bool heldByOther;
            total++; passed += Expect("T118 lock untuk importer",
                TryAcquireLock(stateDir, out held, out heldByOther, out _));
            total++; passed += Expect("T118 impor ditolak saat lock aktif",
                !Import(config, out outcome, out message) && outcome == ImportOutcome.Locked &&
                File.ReadAllBytes(activePath).SequenceEqual(beforeInvalid));
            held.Dispose();

            // Recovery transaksi lama sebelum impor baru.
            string backupPath;
            string backupSha;
            string tempPath;
            string newSha;
            byte[] current = File.ReadAllBytes(activePath);
            total++; passed += Expect("T119 backup skenario recovery",
                CreateBackup(stateDir, "txrecovery", activePath, out backupPath, out backupSha, out _));
            string recoveryOldSha = Sha256File(activePath);
            total++; passed += Expect("T119 temp skenario recovery",
                PreparePromotionTemp(sandbox, activeDir, lockedSource, out tempPath, out newSha, out _));
            Journal interrupted = new Journal
            {
                transactionId = "txrecovery",
                sourcePath = sourcePath,
                activePath = activePath,
                stagingPath = Path.Combine(stagingDir, "txrecovery.json"),
                promotionTempPath = tempPath,
                backupPath = backupPath,
                oldActiveExists = true,
                oldActiveSha256 = recoveryOldSha,
                newContentSha256 = newSha,
                backupSha256 = backupSha,
                state = StatePromoting,
                timestamp = DateTime.UtcNow.ToString("o")
            };
            total++; passed += Expect("T119 journal skenario recovery",
                WriteJournalAtomic(stateDir, interrupted, out _));
            total++; passed += Expect("T119 recovery lalu impor baru",
                Import(config, out outcome, out message) && outcome == ImportOutcome.Imported &&
                File.ReadAllBytes(activePath).SequenceEqual(lockedSource));

            // Config berbahaya (ActiveDir = ProjectRoot) ditolak tanpa menulis.
            Config bad = new Config
            {
                ProjectRoot = sandbox,
                ActiveDir = sandbox,
                StateDir = stateDir,
                StagingDir = stagingDir,
                AudioRoot = audioRoot,
                SourcePath = sourcePath,
                Database = db
            };
            total++; passed += Expect("T120 config berbahaya ditolak",
                !Import(bad, out outcome, out message) && outcome == ImportOutcome.Failed);

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[ImporterSelfTest] RESULT: {result} {passed}/{total}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(db);
            TryDelete(sandbox);
        }
    }

    /// <summary>Self-test loader runtime (Tahap 8): pipeline bersama, filter PUBLISHED, cache, ref.</summary>
    public static void LoaderSelfTest()
    {
        int total = 0;
        int passed = 0;
        LearningDatabase db = ScriptableObject.CreateInstance<LearningDatabase>();
        LearningObjectDefinition periuk = ScriptableObject.CreateInstance<LearningObjectDefinition>();
        periuk.id = "periuk";
        db.objects.Add(periuk);

        try
        {
            CurriculumRoot unpublished = BuildImporterRoot("salam", false);
            string error;
            CurriculumRoot parsedUnpublished = CurriculumLoader.Parse(CurriculumJson.SerializeUtf8(unpublished),
                out error);
            total++; passed += Expect("T121 loader menyaring unpublished",
                parsedUnpublished != null && parsedUnpublished.topics.Count == 0);

            CurriculumRoot published = BuildImporterRoot("salam", true);
            CurriculumRoot parsedPublished = CurriculumLoader.Parse(CurriculumJson.SerializeUtf8(published),
                out error);
            total++; passed += Expect("T121 loader mengekspos published",
                parsedPublished != null && parsedPublished.topics.Count == 1 &&
                parsedPublished.topics[0].lessons.Count == 1 &&
                parsedPublished.topics[0].lessons[0].items.Count == 1);

            total++; passed += Expect("T122 loader menolak duplicate key",
                CurriculumLoader.Parse(Encoding.UTF8.GetBytes(
                    "{\"schemaVersion\":1,\"schemaVersion\":2,\"topics\":[]}"), out error) == null);

            total++; passed += Expect("T123 loader empty/null aman",
                CurriculumLoader.Parse(null, out error) == null &&
                CurriculumLoader.FilterPublished(null).topics.Count == 0);

            // Vocabulary ref: ada/tidak ada.
            CurriculumRoot withRef = BuildImporterRoot("salam", true);
            withRef.topics[0].lessons[0].items.Add(new LessonItemData
            {
                id = "salam-vocab",
                type = "vocabulary",
                validationStatus = ValidationStatus.VALIDATED,
                publicationStatus = PublicationStatus.PUBLISHED,
                objectRef = "periuk"
            });
            List<string> refErrors;
            total++; passed += Expect("T123 ref valid diterima",
                CurriculumLoader.ValidateReferences(withRef, db, out refErrors));
            total++; passed += Expect("T123 database null/ref resolution ditolak aman",
                !CurriculumLoader.ValidateReferences(withRef, null, out refErrors) &&
                CurriculumLoader.ResolveObject("periuk", db) == periuk);

            withRef.topics[0].lessons[0].items[1].objectRef = "tidak-ada";
            total++; passed += Expect("T123 ref yatim terdeteksi",
                !CurriculumLoader.ValidateReferences(withRef, db, out refErrors) && refErrors.Count == 1);

            CurriculumLoader.ClearCache();
            CurriculumRoot firstLoad = CurriculumLoader.Load();
            CurriculumRoot secondLoad = CurriculumLoader.Load();
            total++; passed += Expect("T124 loader cache + tidak crash",
                firstLoad != null && ReferenceEquals(firstLoad, secondLoad));

            string result = passed == total ? "PASS" : "FAIL";
            Debug.Log($"[LoaderSelfTest] RESULT: {result} {passed}/{total}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(periuk);
            UnityEngine.Object.DestroyImmediate(db);
            CurriculumLoader.ClearCache();
        }
    }

    private static CurriculumRoot BuildImporterRoot(string topicId, bool published)
    {
        PublicationStatus publication = published ? PublicationStatus.PUBLISHED : PublicationStatus.UNPUBLISHED;
        ValidationStatus validation = published ? ValidationStatus.VALIDATED : ValidationStatus.UNVALIDATED;

        return new CurriculumRoot
        {
            schemaVersion = CurriculumJson.SchemaVersion,
            locale = "id",
            topics = new List<TopicData>
            {
                new TopicData
                {
                    id = topicId,
                    order = 1,
                    title = new LocalizedText { id = "Topik " + topicId },
                    validationStatus = validation,
                    publicationStatus = publication,
                    lessons = new List<LessonData>
                    {
                        new LessonData
                        {
                            id = topicId + "-1",
                            order = 1,
                            title = new LocalizedText { id = "Pelajaran " + topicId },
                            objective = new LocalizedText { id = "Tujuan " + topicId },
                            skills = new List<string> { "menulis" },
                            validationStatus = validation,
                            publicationStatus = publication,
                            items = new List<LessonItemData>
                            {
                                new LessonItemData
                                {
                                    id = topicId + "-1-ex",
                                    type = "exercise",
                                    validationStatus = validation,
                                    publicationStatus = publication,
                                    skill = "menulis",
                                    question = "Pilih kata yang benar.",
                                    options = new List<string> { "a", "b" },
                                    correctIndex = 0
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// C7: uji jalur runtime nyata Resources.Load&lt;TextAsset&gt; dengan fixture
    /// self-cleaning. Jika fixture produksi sudah ada, test ini NOT RUN dan tidak menimpa.
    /// </summary>
    public static void LoaderRuntimeSelfTest()
    {
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        string curriculumDir = Path.Combine(resourcesDir, "Curriculum");
        string fixturePath = Path.Combine(curriculumDir, "curriculum.json");
        const string assetPath = "Assets/Resources/Curriculum/curriculum.json";

        if (File.Exists(fixturePath))
        {
            Debug.Log("[LoaderRuntimeSelfTest] RESULT: NOT RUN (fixture produksi sudah ada; tidak ditimpa)");
            return;
        }

        int total = 0;
        int passed = 0;
        bool crashed = false;

        try
        {
            Directory.CreateDirectory(curriculumDir);

            CurriculumRoot fixture = BuildImporterRoot("salam", true);
            fixture.topics.Add(BuildImporterRoot("draft", false).topics[0]);
            File.WriteAllBytes(fixturePath, CurriculumJson.SerializeUtf8(fixture));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            total++; passed += Expect("T125 fixture TextAsset terimpor", asset != null && asset.bytes.Length > 0);

            CurriculumLoader.ClearCache();
            TextAsset resourcesAsset = Resources.Load<TextAsset>(CurriculumLoader.ResourcePath);
            total++; passed += Expect("T125 Resources.Load<TextAsset>",
                resourcesAsset != null && resourcesAsset.bytes.Length > 0);

            CurriculumRoot loaded = CurriculumLoader.Load();
            bool onlyPublished = loaded != null && loaded.topics.Count == 1 &&
                                 loaded.topics[0].id == "salam" &&
                                 loaded.topics[0].lessons.Count == 1 &&
                                 loaded.topics[0].lessons[0].items.Count == 1;
            bool draftExcluded = true;
            if (loaded != null)
            {
                for (int i = 0; i < loaded.topics.Count; i++)
                {
                    if (loaded.topics[i].id == "draft")
                    {
                        draftExcluded = false;
                    }
                }
            }

            total++; passed += Expect("T125 filter PUBLISHED + DRAFT tidak dimuat",
                onlyPublished && draftExcluded);

            CurriculumRoot second = CurriculumLoader.Load();
            total++; passed += Expect("T125 cache instance sama", ReferenceEquals(loaded, second));

            File.WriteAllText(fixturePath, "{ corrupt", new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CurriculumLoader.ClearCache();
            CurriculumRoot corrupt = CurriculumLoader.Load();
            total++; passed += Expect("T125 JSON rusak -> kurikulum kosong",
                corrupt != null && corrupt.topics != null && corrupt.topics.Count == 0);

            AssetDatabase.DeleteAsset("Assets/Resources/Curriculum");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CurriculumLoader.ClearCache();
            TextAsset missingAsset = Resources.Load<TextAsset>(CurriculumLoader.ResourcePath);
            CurriculumRoot missing = CurriculumLoader.Load();
            total++; passed += Expect("T125 asset hilang -> fallback kosong",
                missingAsset == null && missing != null && missing.topics != null &&
                missing.topics.Count == 0);
        }
        catch (Exception exception)
        {
            crashed = true;
            Debug.LogError("[LoaderRuntimeSelfTest] exception: " + exception);
        }
        finally
        {
            CurriculumLoader.ClearCache();
            try
            {
                AssetDatabase.DeleteAsset("Assets/Resources/Curriculum");
            }
            catch (Exception)
            {
                // fallback di bawah
            }

            try
            {
                if (Directory.Exists(curriculumDir))
                {
                    Directory.Delete(curriculumDir, true);
                }

                string meta = curriculumDir + ".meta";
                if (File.Exists(meta))
                {
                    File.Delete(meta);
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception exception)
            {
                Debug.LogError("[LoaderRuntimeSelfTest] cleanup gagal: " + exception.Message);
            }
        }

        total++; passed += Expect("T125 tidak ada exception", !crashed);
        total++; passed += Expect("T125 fixture self-cleaning",
            !File.Exists(fixturePath) && !Directory.Exists(curriculumDir));

        string result = passed == total ? "PASS" : "FAIL";
        Debug.Log($"[LoaderRuntimeSelfTest] RESULT: {result} {passed}/{total}");
    }

    /// <summary>
    /// Regresi penuh Stage 1 + Stage 2 dalam satu proses. Setiap suite memakai sandbox
    /// sendiri; tidak menyentuh Resources/Curriculum maupun Library/UabMetoStage2 produksi.
    /// </summary>
    public static void FullRegressionSelfTest()
    {
        regressionFailures = 0;
        Debug.Log("[FullRegression] START");
        CurriculumValidator.EditorSelfTest(); // termasuk ParserSelfTest T118-T123
        PathGuardSelfTest();
        PathGuardAccessDeniedSelfTest();
        JournalSelfTest();
        BackupSelfTest();
        RetentionGuardSelfTest();
        PromotionSelfTest();
        RecoverySelfTest();
        LockSelfTest();
        ImporterSelfTest();
        LoaderSelfTest();
        LoaderRuntimeSelfTest();
        int integrationFailures = CurriculumIntegrationTests.RunAll();

        int stage1Failures = (CurriculumValidator.LastEditorTotal <= 0 ? 1 :
            CurriculumValidator.LastEditorTotal - CurriculumValidator.LastEditorPassed);
        int parserFailures = (CurriculumValidator.LastParserTotal <= 0 ? 1 :
            CurriculumValidator.LastParserTotal - CurriculumValidator.LastParserPassed);
        int totalFailures = regressionFailures + stage1Failures + parserFailures + integrationFailures;

        Debug.Log($"[FullRegression] END | failures={totalFailures} | stage2={regressionFailures} | " +
                  $"stage1={stage1Failures} | parser={parserFailures} | integration={integrationFailures}");

        if (totalFailures > 0)
        {
            string message = "Regresi gagal: " + totalFailures + " assertion.";
            Debug.LogError("[FullRegression] " + message);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
            else
            {
                throw new Exception(message);
            }
        }
    }

    private static bool TryCreateJunction(string linkPath, string targetPath)
    {
        try
        {
            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(info))
            {
                process.WaitForExit(10000);
                return Directory.Exists(linkPath);
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static int regressionFailures;

    private static int Expect(string name, bool condition)
    {
        if (condition)
        {
            Debug.Log($"[Stage2SelfTest] PASS {name}");
            return 1;
        }

        regressionFailures++;
        Debug.LogError($"[Stage2SelfTest] GAGAL {name}");
        return 0;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception)
        {
            // abaikan
        }
    }
}
