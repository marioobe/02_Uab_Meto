using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Placeholder for scene switching between the village and the round house interior.
/// Keep scene names identical to the assets in Assets/Scenes.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    [SerializeField] private string defaultSceneName = "VillageScene";

    /// <summary>
    /// Loads the scene configured on this component.
    /// </summary>
    public void LoadDefault()
    {
        Load(defaultSceneName);
    }

    /// <summary>
    /// Loads a scene by name. The scene must be added to the build settings.
    /// </summary>
    public void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneTransition: scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
