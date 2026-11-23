using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Satu-satunya pintu masuk perpindahan scene (fade + entry point + save).</summary>
public class SceneRouter : MonoBehaviour
{
    public static SceneRouter Instance { get; private set; }

    public static string PendingEntryId { get; set; }

    public bool IsTransitioning { get; private set; }

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

    public void TransitionTo(string sceneName, string entryId)
    {
        if (IsTransitioning || string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scene '{sceneName}' belum ada di Build Settings.");
            ToastView.Instance?.Show($"Scene {sceneName} belum tersedia.");
            return;
        }

        StartCoroutine(DoTransition(sceneName, entryId));
    }

    public void ReturnToBaseMap(string entryId = "Start")
    {
        TransitionTo("Base_Map", entryId);
    }

    private IEnumerator DoTransition(string sceneName, string entryId)
    {
        IsTransitioning = true;
        UiModalState.Clear();
        PendingEntryId = entryId;

        GameState state = GameState.Ensure();
        state.Data.lastScene = sceneName;
        state.Data.lastEntryId = entryId;
        SaveService.Instance?.SaveNow();

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeOut(0.22f);
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            IsTransitioning = false;
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeIn(0.22f);
        }

        IsTransitioning = false;
    }
}
