using System.Collections;
using UnityEngine;

public enum SfxId
{
    Click = 0,
    Pickup = 1,
    Correct = 2,
    Wrong = 3,
    Door = 4,
    Heart = 5
}

/// <summary>
/// Audio reusable. SFX dibuat prosedural agar tidak butuh file audio.
/// Slot BGM dan pelafalan Dawan sengaja kosong (belum tersedia).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // Trek latar dibaca dari Resources supaya tidak perlu wiring di scene.
    private const string BgmResourcePath = "Music/bgm_village";
    private const float BgmFadeInSeconds = 1.5f;

    private AudioSource sfxSource;
    private AudioSource bgmSource;
    private AudioClip[] clips;
    private float musicVolume = 0.55f;
    private float duckScale = 1f;

    private void Awake()
    {
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        clips = new AudioClip[6];
        clips[(int)SfxId.Click] = MakeTone("sfx_click", 760f, 0.05f, 0.35f, 18f);
        clips[(int)SfxId.Pickup] = MakeTone("sfx_pickup", 620f, 0.09f, 0.45f, 12f, 930f);
        clips[(int)SfxId.Correct] = MakeTone("sfx_correct", 880f, 0.16f, 0.45f, 5f, 1320f);
        clips[(int)SfxId.Wrong] = MakeTone("sfx_wrong", 210f, 0.22f, 0.45f, 4f, 150f);
        clips[(int)SfxId.Door] = MakeTone("sfx_door", 300f, 0.16f, 0.4f, 7f);
        clips[(int)SfxId.Heart] = MakeTone("sfx_heart", 540f, 0.18f, 0.4f, 6f, 810f);

        StartCoroutine(StartBgmWhenReady());
    }

    /// <summary>
    /// Memuat trek latar sekali dan memutarnya berulang; fade-in kecil supaya tidak
    /// mengejutkan saat aplikasi dibuka. Bila berkas tidak ada, diam saja (tidak error).
    /// </summary>
    private IEnumerator StartBgmWhenReady()
    {
        AudioClip clip = Resources.Load<AudioClip>(BgmResourcePath);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: BGM '{BgmResourcePath}' tidak ditemukan; musik latar dilewati.");
            yield break;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float elapsed = 0f;
        while (elapsed < BgmFadeInSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = TargetBgmVolume * Mathf.Clamp01(elapsed / BgmFadeInSeconds);
            yield return null;
        }

        bgmSource.volume = TargetBgmVolume;
        Debug.Log($"AudioManager: BGM '{clip.name}' diputar ({clip.length:0.0} detik, loop).");
    }

    private float TargetBgmVolume => musicVolume * duckScale;

    /// <summary>
    /// Menurunkan musik sementara saat pelafalan Dawan berbunyi agar kata tetap jelas,
    /// lalu mengembalikannya. Dipanggil oleh DawanAudioPlayer.
    /// </summary>
    public void SetBgmDucked(bool ducked)
    {
        duckScale = ducked ? 0.35f : 1f;
        if (bgmSource != null)
        {
            bgmSource.volume = TargetBgmVolume;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ApplyVolumes(float music, float sfx)
    {
        musicVolume = Mathf.Clamp01(music);
        if (bgmSource != null)
        {
            bgmSource.volume = TargetBgmVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(sfx);
        }
    }

    public void PlaySfx(SfxId id)
    {
        if (sfxSource == null || clips == null)
        {
            return;
        }

        int index = (int)id;
        if (index < 0 || index >= clips.Length || clips[index] == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clips[index]);
    }

    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null)
        {
            return;
        }

        if (clip == null)
        {
            bgmSource.Stop();
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    private static AudioClip MakeTone(string name, float frequency, float duration, float volume,
        float decay, float secondFrequency = 0f)
    {
        const int sampleRate = 44100;
        int count = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Exp(-decay * t);
            float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);

            if (secondFrequency > 0f)
            {
                wave = (wave + Mathf.Sin(2f * Mathf.PI * secondFrequency * t)) * 0.5f;
            }

            samples[i] = wave * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
