using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Membuat service persisten sebelum scene pertama dimuat: save, setting,
/// audio, router, fader, toast, EventSystem.
/// </summary>
public static class GameBootstrap
{
    private static GameObject root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (root != null)
        {
            return;
        }

        root = new GameObject("GameRoot");
        Object.DontDestroyOnLoad(root);

        GameState.Ensure();

        // Fase C: bersihkan rekaman sementara dari sesi sebelumnya.
        SpeechRecorder.CleanupTempFiles();

        root.AddComponent<SaveService>();
        root.AddComponent<SettingsService>();
        root.AddComponent<HealthService>();
        root.AddComponent<InventoryService>();
        root.AddComponent<ProgressService>();
        root.AddComponent<AudioManager>();
        root.AddComponent<SceneRouter>();
        root.AddComponent<ScreenFader>();
        root.AddComponent<ToastView>();
        root.AddComponent<AppLifecycle>();

        SaveService.Instance.Load();
        SettingsService.Instance.ApplyFromState();

        UiModalState.Clear();
        EnsureEventSystem();

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 0;

        // Game bergaya santai: layar tidak boleh mati sendiri saat pemain membaca panel.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log($"GameBootstrap siap. Save: {SaveService.Instance.SavePath}");
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        Object.DontDestroyOnLoad(eventSystem);
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}

/// <summary>Menyimpan progres saat aplikasi di-pause/ditutup.</summary>
public class AppLifecycle : MonoBehaviour
{
    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveService.Instance?.SaveNow();
        }
    }

    private void OnApplicationQuit()
    {
        SaveService.Instance?.SaveNow();
    }
}
