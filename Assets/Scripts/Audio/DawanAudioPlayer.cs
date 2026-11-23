using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Memutar pelafalan Dawan lewat android.media.MediaPlayer (PCM 16-bit).
///
/// Sebagian perangkat Android (mis. OPPO A3s / ColorOS pada Android 8.1) tidak
/// menyediakan profil output PCM float yang diminta Unity/FMOD, sehingga FMOD
/// gagal inisialisasi dan semua audio Unity senyap. MediaPlayer memakai jalur
/// 16-bit yang selalu tersedia, jadi pelafalan tetap terdengar di perangkat itu.
///
/// Audio dikemas sebagai M4A/AAC di StreamingAssets/Dawan (dibuat oleh
/// Tools/ConvertDawanAudioToAac.py). Pemutar juga menerima WAV, sama seperti
/// validator. Saat pertama diputar, berkas disalin ke penyimpanan aplikasi
/// karena MediaPlayer tidak bisa membuka isi APK langsung.
/// </summary>
public class DawanAudioPlayer : MonoBehaviour
{
    private const string FolderName = "Dawan";

    private static DawanAudioPlayer instance;

    private AndroidJavaObject player;
    private string cacheDirectory;
    private float startedAt;
    private float durationSeconds;
    private AudioClip fallbackClip;
    private float fallbackVolume;

    /// <summary>True bila platform memakai jalur MediaPlayer (Android).</summary>
    public static bool IsSupported
    {
        get { return Application.platform == RuntimePlatform.Android; }
    }

    /// <summary>
    /// Memutar pelafalan bila perangkat memerlukan fallback MediaPlayer.
    /// Mengembalikan false di luar Android agar pemanggil memakai AudioSource.
    /// Bila MediaPlayer gagal, klip yang sama diputar lewat AudioSource.
    /// </summary>
    public static bool TryPlay(string id, AudioClip clip, float volume)
    {
        if (!IsSupported || string.IsNullOrEmpty(id))
        {
            return false;
        }

        DawanAudioPlayer host = Ensure();
        host.fallbackClip = clip;
        host.fallbackVolume = Mathf.Clamp01(volume);
        host.PlayInternal(id + DawanAudioFormat.PrimaryExtension, host.fallbackVolume);
        return true;
    }

    /// <summary>
    /// Memutar audio dari path relatif StreamingAssets/Dawan, dipakai dialogue
    /// kurikulum. Ekstensi mengikuti DawanAudioFormat (.m4a/.wav). Path absolut
    /// dan traversal ditolak. Fallback AudioSource tidak dipakai (tanpa klip).
    /// </summary>
    public static bool TryPlayPath(string relativePath, float volume)
    {
        if (!IsSupported)
        {
            return false;
        }

        string normalized;
        if (!DawanAudioFormat.TryNormalizePath(relativePath, out normalized))
        {
            return false;
        }

        DawanAudioPlayer host = Ensure();
        host.fallbackClip = null;
        host.fallbackVolume = Mathf.Clamp01(volume);
        host.PlayInternal(normalized, host.fallbackVolume);
        return true;
    }

    /// <summary>Jaring pengaman: putar lewat AudioSource Unity bila MediaPlayer gagal.</summary>
    private void PlayFallback(string reason)
    {
        if (fallbackClip == null)
        {
            return;
        }

        Debug.LogWarning($"DawanAudioPlayer: memakai AudioSource ({reason})");
        AudioSource.PlayClipAtPoint(fallbackClip,
            Camera.main != null ? Camera.main.transform.position : Vector3.zero,
            fallbackVolume);
    }

    private static DawanAudioPlayer Ensure()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject host = new GameObject("DawanAudioPlayer");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<DawanAudioPlayer>();
        return instance;
    }

    private string CacheDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(cacheDirectory))
            {
                cacheDirectory = Path.Combine(Application.persistentDataPath, FolderName);
            }

            return cacheDirectory;
        }
    }

    private void PlayInternal(string relativeFile, float volume)
    {
        string path = Path.Combine(CacheDirectory, relativeFile);
        if (File.Exists(path))
        {
            StartPlayer(path, volume);
            return;
        }

        StartCoroutine(ExtractThenPlay(relativeFile, volume));
    }

    private IEnumerator ExtractThenPlay(string relativeFile, float volume)
    {
        string source = Path.Combine(Application.streamingAssetsPath,
            FolderName + "/" + relativeFile);

        using (UnityWebRequest request = UnityWebRequest.Get(source))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"DawanAudioPlayer: gagal membaca {source}: {request.error}");
                PlayFallback("berkas audio tidak terbaca");
                yield break;
            }

            string target = Path.Combine(CacheDirectory, relativeFile);
            string temporary = target + ".tmp";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target) ?? CacheDirectory);
                File.WriteAllBytes(temporary, request.downloadHandler.data);
                if (File.Exists(target))
                {
                    File.Delete(target);
                }

                File.Move(temporary, target);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"DawanAudioPlayer: gagal menyalin audio {relativeFile}: {exception.Message}");
                PlayFallback("gagal menyalin audio");
                yield break;
            }
        }

        StartPlayer(Path.Combine(CacheDirectory, relativeFile), volume);
    }

    private void StartPlayer(string path, float volume)
    {
        ReleasePlayer();

        try
        {
            AndroidJavaObject created = new AndroidJavaObject("android.media.MediaPlayer");

            using (AndroidJavaObject attributesBuilder =
                   new AndroidJavaObject("android.media.AudioAttributes$Builder"))
            {
                // USAGE_MEDIA = 1, CONTENT_TYPE_SPEECH = 1
                attributesBuilder.Call<AndroidJavaObject>("setUsage", 1);
                attributesBuilder.Call<AndroidJavaObject>("setContentType", 1);
                using (AndroidJavaObject attributes = attributesBuilder.Call<AndroidJavaObject>("build"))
                {
                    created.Call("setAudioAttributes", attributes);
                }
            }

            created.Call("setDataSource", path);
            created.Call("prepare");
            created.Call("setVolume", volume, volume);
            created.Call("start");

            int duration = created.Call<int>("getDuration");
            player = created;
            startedAt = Time.unscaledTime;
            durationSeconds = duration > 0 ? duration / 1000f : 0f;
            // Musik latar diturunkan selama pelafalan supaya kata Dawan tetap jelas.
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBgmDucked(true);
            }

            Debug.Log($"DawanAudioPlayer: memutar {path} ({duration} ms, volume {volume:0.00})");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"DawanAudioPlayer: gagal memutar {path}: {exception.Message}");
            ReleasePlayer();
            PlayFallback("MediaPlayer gagal");
        }
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        try
        {
            // Lepas player setelah durasi klip habis (plus jeda kecil). Ini lebih
            // andal daripada isPlaying, yang bisa bernilai false tepat setelah start
            // pada sebagian perangkat sehingga suara terpotong.
            float elapsed = Time.unscaledTime - startedAt;
            if (durationSeconds > 0f && elapsed > durationSeconds + 0.5f)
            {
                ReleasePlayer();
            }
        }
        catch (Exception)
        {
            ReleasePlayer();
        }
    }

    private void ReleasePlayer()
    {
        if (player == null)
        {
            return;
        }

        AndroidJavaObject released = player;
        player = null;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBgmDucked(false);
        }

        try
        {
            released.Call("stop");
        }
        catch (Exception)
        {
            // Klip mungkin sudah selesai; abaikan.
        }

        try
        {
            released.Call("release");
        }
        catch (Exception)
        {
            // Abaikan kegagalan pelepasan.
        }

        try
        {
            released.Dispose();
        }
        catch (Exception)
        {
            // Abaikan.
        }
    }

    private void OnDestroy()
    {
        ReleasePlayer();
        if (instance == this)
        {
            instance = null;
        }
    }
}
