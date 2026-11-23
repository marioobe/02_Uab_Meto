using UnityEngine;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SceneSetup
{
    [MenuItem("Tools/Create MainScene for UAB METO")] // for manual use
    public static void CreateMainScene()
    {
        // Create a new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainScene";

        // ------------------ Main Camera ------------------
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.orthographicSize = 5; // placeholder, will be adjusted by Pixel Perfect
        camGO.tag = "MainCamera";
        // Cinemachine brain is required on the camera that renders the scene
        camGO.AddComponent<CinemachineBrain>();
        // Add Cinemachine Virtual Camera (requires Cinemachine package)
        var vcamGO = new GameObject("CM VCam");
        var vcam = vcamGO.AddComponent<CinemachineCamera>();
        vcam.Target.TrackingTarget = null; // will be set after player created
        // Make Virtual Camera child of Main Camera for hierarchy tidiness
        vcamGO.transform.SetParent(camGO.transform);

        // ------------------ Placeholder Player ------------------
        var playerGO = new GameObject("PlayerPlaceholder");
        var sr = playerGO.AddComponent<SpriteRenderer>();
        // Use a default Unity sprite (white square) – no asset required.
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("Default-Particle.png");
        var rb = playerGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        playerGO.AddComponent<BoxCollider2D>();
        // Position at origin
        playerGO.transform.position = Vector3.zero;
        // Assign the virtual camera to follow this player
        vcam.Target.TrackingTarget = playerGO.transform;

        // ------------------ GameManager ------------------
        var gm = new GameObject("GameManager");
        gm.AddComponent<MonoBehaviour>(); // placeholder, will be replaced later

        // ------------------ InputManager ------------------
        var im = new GameObject("InputManager");
        im.AddComponent<MonoBehaviour>(); // placeholder

        // ------------------ UI Root ------------------
        var canvasGO = new GameObject("UIRoot");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Save scene to Assets/Scenes/MainScene.unity
        var scenesPath = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesPath))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        var scenePath = scenesPath + "/MainScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("MainScene created at " + scenePath);
    }
}
