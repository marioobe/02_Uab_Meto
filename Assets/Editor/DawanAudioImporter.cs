using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menyetel impor audio pelafalan Dawan di Assets/Audio/Dawan agar ringan
/// (mono, Vorbis saat build, Decompress On Load) dan hasil build selalu sama.
/// Rekaman sumber dari penutur (Ogg Opus) dikonversi lebih dulu ke WAV PCM;
/// lihat Tools/ConvertDawanAudio.py dan dokumentasi implementasi.
/// </summary>
public static class DawanAudioImporter
{
    public const string TargetFolder = "Assets/Audio/Dawan";

    [MenuItem("UAB Meto/1b - Import Audio Dawan", priority = 2)]
    public static void ImportAll()
    {
        int count = EnsureImported();
        Debug.Log($"DawanAudioImporter selesai: {count} berkas audio disetel.");
    }

    /// <summary>Menyetel ulang semua audio di folder target. Aman dipanggil berulang.</summary>
    public static int EnsureImported()
    {
        Directory.CreateDirectory(TargetFolder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { TargetFolder });
        int count = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                continue;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;

            importer.forceToMono = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            count++;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return count;
    }

    public static AudioClip LoadClip(string id)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>($"{TargetFolder}/{id}.wav");
    }
}
