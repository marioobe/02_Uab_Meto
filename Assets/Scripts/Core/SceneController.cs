using UnityEngine;

/// <summary>
/// Menempatkan pemain di entry point, membangun UI scene, dan menyimpan status.
/// </summary>
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    public string sceneKey = "Base_Map";
    public string sceneTitle = "Kampung";
    public string defaultEntryId = "Start";
    public Vector2 mapSize = new Vector2(27.52f, 15.36f);
    public LearningDatabase database;
    public PlayerMovement2D player;
    public PlayerInteractor interactor;
    public CameraFollow2D followCamera;
    public GameUi ui;

    private void Awake()
    {
        Instance = this;

        if (sceneKey == "Rumah" &&
            (ProgressService.Instance == null || !ProgressService.Instance.RumahUnlocked))
        {
            Debug.Log("Rumah masih terkunci; kembali ke Base_Map.");
            SceneRouter.Instance?.TransitionTo("Base_Map", "Start");
            return;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerMovement2D>();
        }

        if (interactor == null && player != null)
        {
            interactor = player.GetComponent<PlayerInteractor>();
        }

        if (followCamera == null)
        {
            followCamera = FindFirstObjectByType<CameraFollow2D>();
        }

        string entryId = SceneRouter.PendingEntryId;
        SceneRouter.PendingEntryId = null;
        if (string.IsNullOrEmpty(entryId))
        {
            entryId = defaultEntryId;
        }

        EntryPoint entry = FindEntry(entryId);
        if (entry == null)
        {
            entry = FindEntry(defaultEntryId);
        }

        if (entry != null && player != null)
        {
            player.transform.position = entry.transform.position;
            player.MovementLocked = false;
        }

        if (followCamera != null && player != null)
        {
            // Peta digambar dari (0,0) sampai mapSize, jadi pusat kamera = mapSize/2.
            followCamera.Bind(player.transform, mapSize,
                new Vector2(mapSize.x * 0.5f, mapSize.y * 0.5f), 5.0f);
        }

        if (ui != null)
        {
            ui.Initialize(this);
        }

        GameState state = GameState.Ensure();
        state.Data.lastScene = sceneKey;
        state.Data.lastEntryId = entryId;
        SaveService.Instance?.SaveNow();

        Debug.Log($"Scene siap: {sceneKey} entry={entryId} pos={(player != null ? player.transform.position.ToString() : "-")}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool AllObjectsLearned()
    {
        if (database == null)
        {
            return false;
        }

        var list = database.ForScene(sceneKey);
        if (list.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (!GameState.Ensure().IsLearned(list[i].id))
            {
                return false;
            }
        }

        return true;
    }

    public int LearnedCount()
    {
        if (database == null)
        {
            return 0;
        }

        var list = database.ForScene(sceneKey);
        int count = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (GameState.Ensure().IsLearned(list[i].id))
            {
                count++;
            }
        }

        return count;
    }

    public int ObjectCount()
    {
        return database != null ? database.ForScene(sceneKey).Count : 0;
    }

    public void CompleteCurrentScene()
    {
        ProgressService.Instance?.CompleteScene(sceneKey);
    }

    private static EntryPoint FindEntry(string id)
    {
        EntryPoint[] entries = FindObjectsByType<EntryPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].id == id)
            {
                return entries[i];
            }
        }

        return entries.Length > 0 ? entries[0] : null;
    }
}
