using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Perekam suara sementara untuk latihan bicara (Fase C).
///
/// - Maksimum 10 detik, 44,1 kHz, auto-stop.
/// - AudioClip dipotong sesuai jumlah sampel yang benar-benar direkam.
/// - WAV PCM 16-bit ditulis ke persistentDataPath/speech_tmp.wav (sementara).
/// - Pemutaran memakai AudioClip di memori.
/// - Tidak ada upload, tidak ada riwayat; berkas dihapus saat panel ditutup
///   dan saat aplikasi dimulai.
/// </summary>
public class SpeechRecorder : MonoBehaviour
{
    public const int MaxSeconds = 10;
    public const int Frequency = 44100;
    private const string TempFileName = "speech_tmp.wav";

    private AudioSource source;
    private AudioClip clip;
    private string device;
    private bool recording;
    private float startedAt;
    private int recordedSamples;
    private Action<bool> pendingPermission;
#if UNITY_ANDROID
    private PermissionCallbacks permissionCallbacks;
#endif

    public bool IsRecording => recording;
    public bool HasRecording => clip != null;
    public int RecordedSamples => recordedSamples;
    public float DurationSeconds => clip != null ? clip.length : 0f;
    public AudioClip Clip => clip;

    public static string TempPath
    {
        get { return Path.Combine(Application.persistentDataPath, TempFileName); }
    }

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
    }

    private void Update()
    {
        if (!recording)
        {
            return;
        }

        if (!string.IsNullOrEmpty(device))
        {
            int position = Microphone.GetPosition(device);
            if (position > recordedSamples)
            {
                recordedSamples = position;
            }
        }

        if (Time.unscaledTime - startedAt >= MaxSeconds)
        {
            StopRecording();
        }
    }

    // ---------------------------------------------------------------- izin

    public void EnsurePermission(Action<bool> onResult)
    {
#if UNITY_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            onResult?.Invoke(true);
            return;
        }

        pendingPermission = onResult;
        permissionCallbacks = new PermissionCallbacks();
        permissionCallbacks.PermissionGranted += OnPermissionGranted;
        permissionCallbacks.PermissionDenied += OnPermissionDenied;
        permissionCallbacks.PermissionDeniedAndDontAskAgain += OnPermissionDenied;
        Permission.RequestUserPermission(Permission.Microphone, permissionCallbacks);
#else
        onResult?.Invoke(true);
#endif
    }

#if UNITY_ANDROID
    private void OnPermissionGranted(string permission)
    {
        Action<bool> callback = pendingPermission;
        pendingPermission = null;
        callback?.Invoke(true);
    }

    private void OnPermissionDenied(string permission)
    {
        Action<bool> callback = pendingPermission;
        pendingPermission = null;
        callback?.Invoke(false);
    }

    public static bool HasPermission()
    {
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
    }
#else
    public static bool HasPermission()
    {
        return true;
    }
#endif

    // ---------------------------------------------------------------- rekam

    public bool StartRecording(out string error)
    {
        error = null;

        if (recording)
        {
            error = "Sedang merekam.";
            return false;
        }

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            error = "Mikrofon tidak ditemukan di perangkat ini.";
            return false;
        }

        if (!HasPermission())
        {
            error = "Izin mikrofon belum diberikan.";
            return false;
        }

        device = Microphone.devices[0];
        clip = Microphone.Start(device, false, MaxSeconds, Frequency);
        if (clip == null)
        {
            error = "Gagal memulai mikrofon.";
            return false;
        }

        recordedSamples = 0;
        startedAt = Time.unscaledTime;
        recording = true;
        return true;
    }

    public void StopRecording()
    {
        if (!recording)
        {
            return;
        }

        int position = 0;
        if (!string.IsNullOrEmpty(device))
        {
            position = Microphone.GetPosition(device);
            Microphone.End(device);
        }

        recording = false;
        if (position > recordedSamples)
        {
            recordedSamples = position;
        }

        if (clip == null)
        {
            return;
        }

        int channels = Mathf.Max(1, clip.channels);
        int available = Mathf.Max(1, clip.samples);
        int samples = Mathf.Clamp(recordedSamples, 1, available);

        float[] all = new float[available * channels];
        clip.GetData(all, 0);

        float[] trimmed = new float[samples * channels];
        Array.Copy(all, trimmed, trimmed.Length);

        AudioClip result = AudioClip.Create("speech_record", samples, channels, Frequency, false);
        result.SetData(trimmed, 0);

        Destroy(clip);
        clip = result;
        recordedSamples = samples;

        WriteWav(clip, TempPath);
    }

    // ---------------------------------------------------------------- putar

    public void Play()
    {
        if (source == null || clip == null)
        {
            return;
        }

        source.Stop();
        source.clip = clip;
        source.Play();
    }

    public void StopPlayback()
    {
        if (source != null)
        {
            source.Stop();
        }
    }

    public void Clear()
    {
        StopPlayback();
        if (clip != null)
        {
            Destroy(clip);
            clip = null;
        }

        recordedSamples = 0;
        DeleteTempFile();
    }

    private void OnDestroy()
    {
        Clear();
        pendingPermission = null;
#if UNITY_ANDROID
        permissionCallbacks = null;
#endif
    }

    // ---------------------------------------------------------------- berkas

    public static void CleanupTempFiles()
    {
        DeleteTempFile();
    }

    private static void DeleteTempFile()
    {
        try
        {
            if (File.Exists(TempPath))
            {
                File.Delete(TempPath);
            }
        }
        catch (Exception error)
        {
            Debug.LogWarning($"SpeechRecorder: gagal menghapus {TempPath}: {error.Message}");
        }
    }

    /// <summary>Menulis WAV PCM 16-bit tanpa dependensi eksternal.</summary>
    private static void WriteWav(AudioClip audio, string path)
    {
        if (audio == null)
        {
            return;
        }

        int channels = Mathf.Max(1, audio.channels);
        int sampleRate = Mathf.Max(1, audio.frequency);
        int sampleCount = Mathf.Max(1, audio.samples);

        float[] samples = new float[sampleCount * channels];
        audio.GetData(samples, 0);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);

            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                int dataBytes = sampleCount * channels * 2;

                WriteTag(writer, "RIFF");
                writer.Write(36 + dataBytes);
                WriteTag(writer, "WAVE");
                WriteTag(writer, "fmt ");
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                WriteTag(writer, "data");
                writer.Write(dataBytes);

                for (int i = 0; i < samples.Length; i++)
                {
                    float value = Mathf.Clamp(samples[i], -1f, 1f);
                    writer.Write((short)Mathf.RoundToInt(value * 32767f));
                }
            }
        }
        catch (Exception error)
        {
            Debug.LogWarning($"SpeechRecorder: gagal menulis WAV: {error.Message}");
        }
    }

    private static void WriteTag(BinaryWriter writer, string tag)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(tag);
        writer.Write(bytes, 0, 4);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Validasi penulisan WAV tanpa mikrofon (dipakai saat perangkat uji tidak
    /// menyediakan Microphone.devices). Menulis 2 detik sinus 440 Hz lalu
    /// mencetak ukuran berkas + jumlah sampel.
    /// </summary>
    public static void EditorSelfTest()
    {
        const int seconds = 2;
        int samples = Frequency * seconds;
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            data[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / Frequency) * 0.5f;
        }

        AudioClip clip = AudioClip.Create("speech_selftest", samples, 1, Frequency, false);
        clip.SetData(data, 0);

        string path = Path.Combine(Application.persistentDataPath, "speech_selftest.wav");
        WriteWav(clip, path);

        long size = File.Exists(path) ? new FileInfo(path).Length : -1;
        Debug.Log($"[SpeechRecorder] SelfTest path={path} samples={samples} " +
                  $"expectedBytes={44 + samples * 2} actualBytes={size}");
    }
#endif
}
