using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the playable village scene from the base map PNG.
/// Run from the menu: Tools > UAB METO > 2. Build Village Scene.
/// The builder is idempotent: it regenerates the scene from scratch every run.
/// </summary>
public static class VillageSceneBuilder
{
    private const string MapSourcePath = "Assets/foto/Peta Desa RPG dengan Gubuk Buku.png";
    private const string MapAssetPath = "Assets/Art/Maps/VillageMap_Base.png";
    private const string PlayerSpritePath = "Assets/Art/Characters/Player_Placeholder.png";
    private const string CharacterFolder = "Assets/Art/Characters/Yuuta";
    private const string UiPanelPath = "Assets/Art/UI/UI_Panel_Beige.png";
    private const string HudFolderPath = "Assets/Art/UI/Hud";
    private const string CircleSpritePath = "Assets/Art/UI/UI_Circle.png";
    private const string VillageScenePath = "Assets/Scenes/VillageScene.unity";
    private const string InteriorScenePath = "Assets/Scenes/RoundHouseInterior.unity";

    // Titik awal pemain di koordinat piksel peta (origin kiri-atas).
    private static readonly Vector2 PlayerStartPx = new Vector2(640f, 600f);

    [MenuItem("Tools/UAB METO/1. Import Village Map", priority = 1)]
    public static void ImportMap()
    {
        EnsureFolders();
        Sprite map = EnsureMapSprite();

        if (map != null)
        {
            Debug.Log($"Peta siap: {MapAssetPath} ({map.rect.width}x{map.rect.height} px, PPU {MapImporter.PixelsPerUnit})");
        }
    }

    [MenuItem("Tools/UAB METO/2. Build Village Scene", priority = 2)]
    public static void BuildVillageScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("Build Village Scene dibatalkan.");
            return;
        }

        EnsureFolders();

        Sprite mapSprite = EnsureMapSprite();
        if (mapSprite == null)
        {
            return;
        }

        CharacterFrames frames = EnsureCharacterAssets();
        Sprite playerSprite = frames?.Idle ?? EnsurePlayerSprite();
        Sprite circleSprite = EnsureCircleSprite();
        Sprite panelSprite = EnsureUiPanelSprite();

        Vector2 mapPixels = new Vector2(mapSprite.rect.width, mapSprite.rect.height);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildVillage(mapSprite, mapPixels);
        GameObject player = BuildPlayer(frames, playerSprite, PixelToWorld(PlayerStartPx, mapPixels));
        player.AddComponent<SpawnPoint2D>();
        BuildCamera(player.transform, mapPixels);
        BuildGuide(frames, playerSprite, mapPixels);
        BuildLearningSpots(mapPixels);
        BuildFoodPickups(mapPixels);
        BuildUserInterface(player, circleSprite, panelSprite);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, VillageScenePath);
        AssetDatabase.SaveAssets();
        RegisterScenes();

        Selection.activeGameObject = player;
        Debug.Log($"VillageScene dibangun: peta {mapPixels.x}x{mapPixels.y} px, PPU {MapImporter.PixelsPerUnit}");
    }

    [MenuItem("Tools/UAB METO/3. Configure Village Map Import Only", priority = 3)]
    public static void ConfigureMapOnly()
    {
        MapImporter.ConfigureSprite(MapAssetPath);
        Debug.Log($"Import setting peta diperbarui: {MapAssetPath}");
    }

    [MenuItem("Tools/UAB METO/4. Diagnose UI Font", priority = 4)]
    public static void DiagnoseFont()
    {
        Font legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Font arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Debug.Log($"Font LegacyRuntime.ttf = {(legacy != null ? legacy.name : "NULL")}, Arial.ttf = {(arial != null ? arial.name : "NULL")}");
    }

    [MenuItem("Tools/UAB METO/6. Render Collider Overlay", priority = 6)]
    public static void RenderColliderOverlay()
    {
        if (EnsureMapSprite() == null)
        {
            return;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(MapAssetPath)))
        {
            Debug.LogError("Gagal membaca peta untuk overlay collider.");
            return;
        }

        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();

        // Gelapkan peta supaya garis collider menonjol.
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            pixels[i] = new Color32((byte)(c.r * 0.45f), (byte)(c.g * 0.45f), (byte)(c.b * 0.45f), 255);
        }

        Color32 boxColor = new Color32(255, 45, 45, 255);
        Color32 fenceColor = new Color32(60, 170, 255, 255);

        foreach ((string _, Vector2 center, Vector2 size) in Blockers)
        {
            DrawRect(pixels, width, height, center - size * 0.5f, center + size * 0.5f, boxColor, 2);
        }

        foreach ((string _, Vector2 a, Vector2 b, float thickness) in Fences)
        {
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);

            DrawLine(pixels, width, height, a + normal, b + normal, fenceColor);
            DrawLine(pixels, width, height, a - normal, b - normal, fenceColor);
        }

        // Penanda hasil kebun yang bisa diambil.
        Color32 pickupColor = new Color32(140, 255, 140, 255);
        foreach ((string _, string _, Vector2 px) in FoodSpots)
        {
            DrawRect(pixels, width, height, px - new Vector2(8f, 8f), px + new Vector2(8f, 8f), pickupColor, 2);
        }

        Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false);
        output.SetPixels32(pixels);
        output.Apply();

        string outputPath = Path.Combine(Path.GetTempPath(), "uab_collider_overlay.png");
        File.WriteAllBytes(outputPath, output.EncodeToPNG());

        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(output);
        Debug.Log($"Overlay collider disimpan: {outputPath}");
    }

    private static void DrawLine(Color32[] pixels, int width, int height, Vector2 a, Vector2 b, Color32 color)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));

        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(a, b, i / (float)steps);
            int x = Mathf.RoundToInt(point.x);
            int y = Mathf.RoundToInt(point.y);
            Plot(pixels, width, height, x, y, color);
            Plot(pixels, width, height, x, y + 1, color);
        }
    }

    private static void DrawRect(Color32[] pixels, int width, int height,
        Vector2 min, Vector2 max, Color32 color, int thickness)
    {
        for (int t = 0; t < thickness; t++)
        {
            float inset = t;
            DrawLine(pixels, width, height, new Vector2(min.x + inset, min.y + inset), new Vector2(max.x - inset, min.y + inset), color);
            DrawLine(pixels, width, height, new Vector2(max.x - inset, min.y + inset), new Vector2(max.x - inset, max.y - inset), color);
            DrawLine(pixels, width, height, new Vector2(max.x - inset, max.y - inset), new Vector2(min.x + inset, max.y - inset), color);
            DrawLine(pixels, width, height, new Vector2(min.x + inset, max.y - inset), new Vector2(min.x + inset, min.y + inset), color);
        }
    }

    private static void Plot(Color32[] pixels, int width, int height, int x, int y, Color32 color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        pixels[(height - 1 - y) * width + x] = color;
    }

    // ---------------------------------------------------------------- aset

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Art/Maps");
        EnsureFolder("Assets/Art/Characters");
        EnsureFolder("Assets/Art/UI");
        EnsureFolder("Assets/Scenes");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string leaf = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    private static Sprite EnsureMapSprite()
    {
        if (!File.Exists(MapAssetPath))
        {
            if (!File.Exists(MapSourcePath))
            {
                Debug.LogError($"Peta tidak ditemukan di {MapSourcePath}");
                return null;
            }

            File.Copy(MapSourcePath, MapAssetPath, false);
            AssetDatabase.ImportAsset(MapAssetPath, ImportAssetOptions.ForceUpdate);
        }

        MapImporter.ConfigureSprite(MapAssetPath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(MapAssetPath);
    }

    // ---------------------------------------------------------------- karakter & UI

    private sealed class CharacterFrames
    {
        public Sprite[] IdleDown;
        public Sprite[] WalkDown;
        public Sprite[] WalkSide;
        public Sprite[] WalkUp;

        public Sprite Idle => IdleDown != null && IdleDown.Length > 0 ? IdleDown[0] : null;
    }

    /// <summary>
    /// Converts the downloaded spritesheet / UI pack on first use and loads the
    /// 16 character frames. Returns null when the frames are unavailable.
    /// </summary>
    private static CharacterFrames EnsureCharacterAssets()
    {
        if (!File.Exists($"{CharacterFolder}/yuuta_idle_down_1.png") || !File.Exists(UiPanelPath))
        {
            AssetConverter.ConvertAll();
        }

        CharacterFrames frames = new CharacterFrames
        {
            IdleDown = LoadCharacterRow("idle_down"),
            WalkDown = LoadCharacterRow("walk_down"),
            WalkSide = LoadCharacterRow("walk_side"),
            WalkUp = LoadCharacterRow("walk_up"),
        };

        if (frames.IdleDown == null || frames.WalkDown == null || frames.WalkSide == null || frames.WalkUp == null)
        {
            Debug.LogError("Frame karakter belum lengkap; memakai placeholder.");
            return null;
        }

        return frames;
    }

    private static Sprite[] LoadCharacterRow(string rowName)
    {
        Sprite[] frames = new Sprite[4];

        for (int i = 0; i < frames.Length; i++)
        {
            string path = $"{CharacterFolder}/yuuta_{rowName}_{i + 1}.png";
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (frames[i] == null)
            {
                Debug.LogError($"Frame karakter tidak ditemukan: {path}");
                return null;
            }
        }

        return frames;
    }

    private static Sprite EnsureUiPanelSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiPanelPath);
        if (sprite == null)
        {
            Debug.LogError($"Panel UI tidak ditemukan di {UiPanelPath}");
        }

        return sprite;
    }

    private static Sprite[] FirstOnly(Sprite[] frames)
    {
        return frames != null && frames.Length > 0 ? new[] { frames[0] } : new Sprite[0];
    }

    private static Sprite EnsurePlayerSprite()
    {
        if (!File.Exists(PlayerSpritePath))
        {
            const int width = 64;
            const int height = 96;
            Color[] pixels = new Color[width * height];

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            Color outline = new Color32(43, 33, 24, 255);
            Color hair = new Color32(58, 40, 28, 255);
            Color skin = new Color32(232, 185, 138, 255);
            Color cloth = new Color32(63, 111, 90, 255);
            Color sash = new Color32(214, 176, 92, 255);
            Color pants = new Color32(56, 52, 66, 255);
            Color shadow = new Color(0f, 0f, 0f, 0.22f);

            FillEllipse(pixels, width, height, 32, 5, 17, 5, shadow);

            FillRect(pixels, width, height, 19, 8, 45, 20, outline);
            FillRect(pixels, width, height, 21, 9, 30, 19, pants);
            FillRect(pixels, width, height, 34, 9, 43, 19, pants);

            FillRect(pixels, width, height, 17, 17, 47, 54, outline);
            FillRect(pixels, width, height, 19, 19, 45, 52, cloth);
            FillRect(pixels, width, height, 19, 32, 45, 39, sash);

            FillCircle(pixels, width, height, 32, 66, 16, outline);
            FillCircle(pixels, width, height, 32, 66, 14, skin);
            FillRect(pixels, width, height, 18, 73, 46, 80, hair);
            FillEllipse(pixels, width, height, 32, 78, 14, 8, hair);

            FillRect(pixels, width, height, 27, 64, 29, 68, outline);
            FillRect(pixels, width, height, 36, 64, 38, 68, outline);

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(PlayerSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PlayerSpritePath, ImportAssetOptions.ForceUpdate);
        }

        MapImporter.ConfigureSprite(PlayerSpritePath, new Vector2(0.5f, 0f));
        return AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
    }

    private static Sprite EnsureCircleSprite()
    {
        if (!File.Exists(CircleSpritePath))
        {
            const int size = 128;
            Color[] pixels = new Color[size * size];

            float radius = size * 0.5f - 1f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(CircleSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(CircleSpritePath, ImportAssetOptions.ForceUpdate);
        }

        MapImporter.ConfigureSprite(CircleSpritePath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
    }

    // ---------------------------------------------------------------- peta

    private static void BuildVillage(Sprite mapSprite, Vector2 mapPixels)
    {
        Transform village = new GameObject("Village").transform;

        GameObject background = new GameObject("Background");
        background.transform.SetParent(village, false);

        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = mapSprite;
        renderer.sortingOrder = -100;

        Transform colliders = new GameObject("Colliders").transform;
        colliders.SetParent(village, false);

        BuildBlockers(colliders, mapPixels);
        BuildFenceSegments(colliders, mapPixels);
    }

    // Kotak penghalang (bangunan, kebun, batas desa) dalam koordinat piksel peta.
    // Semua angka diukur langsung dari VillageMap_Base.png (grid 10 px).
    private static readonly (string Name, Vector2 Center, Vector2 Size)[] Blockers =
    {
        // Rumah batu beratap jerami (termasuk atap, supaya pemain tidak menembusnya)
        ("Rumah_01", new Vector2(332f, 195f), new Vector2(285f, 205f)),
        ("Rumah_02", new Vector2(1020f, 285f), new Vector2(320f, 270f)),
        ("Rumah_03", new Vector2(255f, 480f), new Vector2(200f, 185f)),
        ("Rumah_04", new Vector2(425f, 705f), new Vector2(420f, 215f)),
        ("Rumah_05", new Vector2(1100f, 685f), new Vector2(180f, 200f)),

        // Gubuk bulat (sampai dasar tangga supaya pemain berhenti di depan pintu)
        ("Gubuk_01", new Vector2(538f, 230f), new Vector2(145f, 180f)),
        ("Gubuk_02", new Vector2(1232f, 285f), new Vector2(140f, 175f)),
        ("Gubuk_03", new Vector2(400f, 545f), new Vector2(140f, 155f)),
        ("Gubuk_04", new Vector2(1232f, 630f), new Vector2(125f, 140f)),
        ("Gubuk_05", new Vector2(707f, 760f), new Vector2(140f, 160f)),

        // Sumur
        ("Sumur", new Vector2(568f, 530f), new Vector2(58f, 62f)),

        // Kebun dan bedeng tanaman (kebun barat dipecah dua: baris bawah lebih lebar)
        ("Kebun_Barat_Atas", new Vector2(145f, 250f), new Vector2(110f, 140f)),
        ("Kebun_Barat_Bawah", new Vector2(198f, 344f), new Vector2(204f, 46f)),
        ("Kebun_Utara", new Vector2(626f, 209f), new Vector2(62f, 162f)),
        ("Bedeng_Utara", new Vector2(1076f, 138f), new Vector2(100f, 72f)),
        ("Kebun_Selatan", new Vector2(122f, 790f), new Vector2(120f, 240f)),
        ("Bedeng_Tenggara", new Vector2(1170f, 887f), new Vector2(255f, 78f)),
        ("Bedeng_Hut5", new Vector2(658f, 887f), new Vector2(95f, 66f)),

        // Kandang kecil kosong di utara halaman barat
        ("Kandang_Kecil", new Vector2(265f, 80f), new Vector2(88f, 38f)),

        // Reruntuhan dinding batu di barat laut gubuk 5
        ("Reruntuhan_Batu", new Vector2(818f, 775f), new Vector2(145f, 62f)),

        // Batas luar desa
        ("Batas_Atas", new Vector2(679f, 30f), new Vector2(1358f, 60f)),
        ("Batas_Kiri", new Vector2(25f, 620f), new Vector2(50f, 1080f)),
        ("Batas_Kanan", new Vector2(1341f, 560f), new Vector2(34f, 1000f)),
        ("Batas_Bawah", new Vector2(679f, 1120f), new Vector2(1358f, 76f)),
    };

    // Garis pagar kayu / dinding batu: (nama, titik A, titik B, tebal dalam piksel).
    private static readonly (string Name, Vector2 A, Vector2 B, float Thickness)[] Fences =
    {
        // Pagar halaman barat (celah gerbang di x 348..445)
        ("Pagar_Barat_1", new Vector2(40f, 385f), new Vector2(348f, 385f), 24f),
        ("Pagar_Barat_2", new Vector2(445f, 385f), new Vector2(680f, 385f), 24f),

        // Pagar tegak pagar kebun barat
        ("Pagar_Kebun_Barat", new Vector2(80f, 158f), new Vector2(80f, 385f), 14f),

        // Pagar tegak di antara halaman barat dan halaman timur
        ("Pagar_Timur_Kiri", new Vector2(678f, 80f), new Vector2(678f, 385f), 18f),
        ("Pagar_Timur_Kanan", new Vector2(777f, 92f), new Vector2(777f, 348f), 18f),
        ("Pagar_Timur_Bawah", new Vector2(777f, 348f), new Vector2(862f, 348f), 18f),

        // Pagar di depan Rumah_05 dan pagar halaman gubuk 4
        ("Pagar_Rumah05", new Vector2(895f, 765f), new Vector2(1030f, 765f), 24f),
        ("Pagar_Kanan_Tegak", new Vector2(1302f, 692f), new Vector2(1302f, 940f), 18f),
        ("Pagar_Kanan_Datar", new Vector2(1302f, 800f), new Vector2(1358f, 800f), 18f),

        // Dinding batu miring di sudut atas desa
        ("Dinding_Miring_Kiri", new Vector2(0f, 112f), new Vector2(118f, 36f), 38f),
        ("Dinding_Miring_Kanan_1", new Vector2(1060f, 55f), new Vector2(1200f, 130f), 45f),
        ("Dinding_Miring_Kanan_2", new Vector2(1200f, 130f), new Vector2(1265f, 172f), 45f),
        ("Dinding_Miring_Kanan_3", new Vector2(1265f, 172f), new Vector2(1358f, 182f), 42f),

        // Dinding batu di selatan: celah jalan di x 752..945
        ("Dinding_Batu_Selatan_Barat", new Vector2(0f, 955f), new Vector2(752f, 955f), 40f),
        ("Dinding_Batu_Selatan_Timur", new Vector2(945f, 955f), new Vector2(1358f, 955f), 40f),
        ("Dinding_Batu_Tegak_Barat", new Vector2(738f, 845f), new Vector2(738f, 952f), 26f),
        ("Dinding_Batu_Miring_Timur", new Vector2(884f, 786f), new Vector2(947f, 820f), 28f),
        ("Dinding_Batu_Tegak_Timur", new Vector2(947f, 820f), new Vector2(947f, 952f), 24f),
    };

    private static void BuildBlockers(Transform parent, Vector2 mapPixels)
    {
        foreach ((string name, Vector2 centerPx, Vector2 sizePx) in Blockers)
        {
            GameObject blocker = new GameObject(name);
            blocker.transform.SetParent(parent, false);
            blocker.transform.position = PixelToWorld(centerPx, mapPixels);

            BoxCollider2D box = blocker.AddComponent<BoxCollider2D>();
            box.size = sizePx / MapImporter.PixelsPerUnit;
        }
    }

    private static void BuildFenceSegments(Transform parent, Vector2 mapPixels)
    {
        foreach ((string name, Vector2 aPx, Vector2 bPx, float thicknessPx) in Fences)
        {
            Vector2 a = PixelToWorld(aPx, mapPixels);
            Vector2 b = PixelToWorld(bPx, mapPixels);
            Vector2 delta = b - a;

            GameObject fence = new GameObject(name);
            fence.transform.SetParent(parent, false);
            fence.transform.position = (a + b) * 0.5f;
            fence.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            BoxCollider2D box = fence.AddComponent<BoxCollider2D>();
            box.size = new Vector2(delta.magnitude, thicknessPx / MapImporter.PixelsPerUnit);
        }
    }

    // ---------------------------------------------------------------- aktor

    private static GameObject BuildPlayer(CharacterFrames frames, Sprite fallbackSprite, Vector2 spawnWorld)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = spawnWorld;

        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = frames?.Idle ?? fallbackSprite;
        renderer.sortingOrder = 10;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.size = new Vector2(0.30f, 0.18f);
        capsule.offset = new Vector2(0f, 0.09f);

        PlayerMovement2D movement = player.AddComponent<PlayerMovement2D>();
        player.AddComponent<PlayerInteractor>();

        if (frames != null)
        {
            PlayerAnimator2D animator = player.AddComponent<PlayerAnimator2D>();
            animator.Bind(renderer,
                frames.IdleDown, frames.WalkDown,
                FirstOnly(frames.WalkSide), frames.WalkSide,
                FirstOnly(frames.WalkUp), frames.WalkUp);
            movement.BindAnimator(animator);
        }

        return player;
    }

    private static void BuildCamera(Transform target, Vector2 mapPixels)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.11f, 0.08f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 50f;

        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(target.position.x, target.position.y + 0.35f, -10f);

        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        follow.Bind(target, mapPixels / MapImporter.PixelsPerUnit);
    }

    private static void BuildGuide(CharacterFrames frames, Sprite fallbackSprite, Vector2 mapPixels)
    {
        Transform parent = new GameObject("NPCs").transform;

        GameObject guideObject = new GameObject("Guide");
        guideObject.transform.SetParent(parent, false);
        guideObject.transform.position = PixelToWorld(new Vector2(780f, 600f), mapPixels);

        SpriteRenderer renderer = guideObject.AddComponent<SpriteRenderer>();
        renderer.sprite = frames?.Idle ?? fallbackSprite;
        renderer.color = new Color(0.78f, 0.72f, 1f, 1f);
        renderer.sortingOrder = 9;

        CircleCollider2D trigger = guideObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.7f;

        NPCGuide guide = guideObject.AddComponent<NPCGuide>();
        guide.BindNpc("Pemandu Desa");
        guide.Bind(
            "Pemandu Desa",
            "Tabe! Selamat datang di desa. Saya pemandu yang akan menemani kamu belajar tentang kehidupan di sini.\n\n" +
            "Geser joystick di kiri bawah untuk berjalan. Dekati orang atau gubuk sampai muncul tulisan di atas layar, lalu tekan tombol Baca.\n\n" +
            "Setiap gubuk buku punya materi sendiri. Masuk lewat pintunya, baca materi di dalam, lalu keluar lagi lewat pintu.",
            "Tekan E / Baca untuk berbicara");
    }

    private static void BuildLearningSpots(Vector2 mapPixels)
    {
        Transform parent = new GameObject("Interactables").transform;

        (string Name, Vector2 DoorPx)[] spots =
        {
            ("GubukBuku_01", new Vector2(535f, 302f)),
            ("GubukBuku_02", new Vector2(1205f, 352f)),
            ("GubukBuku_03", new Vector2(395f, 587f)),
            ("GubukBuku_04", new Vector2(1225f, 687f)),
            ("GubukBuku_05", new Vector2(705f, 807f)),
        };

        int index = 0;
        foreach ((string name, Vector2 doorPx) in spots)
        {
            GameObject spot = new GameObject(name);
            spot.transform.SetParent(parent, false);
            spot.transform.position = PixelToWorld(doorPx, mapPixels);

            BoxCollider2D trigger = spot.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.9f, 0.7f);

            RoundHouseInteraction interaction = spot.AddComponent<RoundHouseInteraction>();
            interaction.BindSpot(index);
            interaction.BindInterior("RoundHouseInterior", true);

            index++;
        }
    }

    // ---------------------------------------------------------------- interior

    private const string InteriorArtPath = "Assets/Art/Maps/Interior_UmeKbubu.png";
    private const int InteriorArtWidth = 1024;
    private const int InteriorArtHeight = 480;
    private const float InteriorPixelsPerUnit = 60f;

    /// <summary>Bangun desa + interior + overlay collider dalam satu jalan (untuk batch).</summary>
    public static void BuildAllScenes()
    {
        BuildVillageScene();
        BuildInteriorScene();
        RenderColliderOverlay();
        ValidatePickupSpots();
        Debug.Log("Semua scene dibangun (desa + interior).");
    }

    [MenuItem("Tools/UAB METO/10. Build Interior Scene", priority = 10)]
    public static void BuildInteriorScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("Build Interior Scene dibatalkan.");
            return;
        }

        EnsureFolders();

        CharacterFrames frames = EnsureCharacterAssets();
        Sprite playerSprite = frames?.Idle ?? EnsurePlayerSprite();
        Sprite circleSprite = EnsureCircleSprite();
        Sprite panelSprite = EnsureUiPanelSprite();

        // Selalu gambar ulang supaya latar ikut perubahan kode.
        GenerateInteriorArt();
        Sprite interiorSprite = EnsureInteriorSprite();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject background = new GameObject("InteriorBackground");
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = interiorSprite;
        backgroundRenderer.sortingOrder = -10;

        // Pemain masuk lewat pintu di bawah; muncul sedikit ke dalam ruangan supaya
        // prompt "keluar" tidak langsung aktif.
        GameObject player = BuildPlayer(frames, playerSprite, new Vector2(1.9f, -1.4f));
        BuildInteriorCamera();
        BuildInteriorColliders();
        BuildInteriorSpots();
        BuildUserInterface(player, circleSprite, panelSprite);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, InteriorScenePath);
        AssetDatabase.SaveAssets();
        RegisterScenes();

        Selection.activeGameObject = player;
        Debug.Log("RoundHouseInterior dibangun: latar 1024x480 px, ruang 17x8 unit.");
    }

    [MenuItem("Tools/UAB METO/11. Regenerate Interior Art", priority = 11)]
    public static void RegenerateInteriorArt()
    {
        EnsureFolders();
        GenerateInteriorArt();
        Debug.Log($"Interior art dibuat ulang: {InteriorArtPath}");
    }

    private static Sprite EnsureInteriorSprite()
    {
        if (!File.Exists(InteriorArtPath))
        {
            GenerateInteriorArt();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(InteriorArtPath);
        if (sprite == null)
        {
            Debug.LogError($"Latar interior tidak ditemukan di {InteriorArtPath}");
        }

        return sprite;
    }

    private static void BuildInteriorCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.04f, 0.03f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 50f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        cameraObject.AddComponent<AudioListener>();
    }

    private static void BuildInteriorColliders()
    {
        Transform parent = new GameObject("Colliders").transform;

        AddBoxCollider(parent, "Dinding_Atas", new Vector2(0f, 1.02f), new Vector2(17.2f, 3.08f));
        AddBoxCollider(parent, "Dinding_Kiri", new Vector2(-7.95f, -1.5f), new Vector2(1.2f, 6f));
        AddBoxCollider(parent, "Dinding_Kanan", new Vector2(7.95f, -1.5f), new Vector2(1.2f, 6f));
        AddBoxCollider(parent, "Dinding_Bawah", new Vector2(0f, -4.15f), new Vector2(17.2f, 0.7f));
        AddBoxCollider(parent, "Meja_Kayu", new Vector2(3.47f, -2.47f), new Vector2(2.6f, 1.1f));
    }

    private static void AddBoxCollider(Transform parent, string name, Vector2 center, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = center;

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static void BuildInteriorSpots()
    {
        Transform parent = new GameObject("Interactables").transform;

        AddShelfSpot(parent, "RakBuku_Kiri", new Vector2(-4.4f, -0.75f), 1.35f);
        AddShelfSpot(parent, "RakBuku_Kanan", new Vector2(4.4f, -0.75f), 1.35f);

        GameObject exit = new GameObject("PintuKeluar");
        exit.transform.SetParent(parent, false);
        exit.transform.position = new Vector2(0f, -3.45f);

        BoxCollider2D exitTrigger = exit.AddComponent<BoxCollider2D>();
        exitTrigger.isTrigger = true;
        exitTrigger.size = new Vector2(1.9f, 1.1f);

        InteriorExit exitInteraction = exit.AddComponent<InteriorExit>();
        exitInteraction.Bind("Pintu Keluar", string.Empty, "Tekan E / Baca untuk keluar");
    }

    private static void AddShelfSpot(Transform parent, string name, Vector2 position, float radius)
    {
        GameObject shelf = new GameObject(name);
        shelf.transform.SetParent(parent, false);
        shelf.transform.position = position;

        CircleCollider2D trigger = shelf.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = radius;

        InteriorBookshelf bookshelf = shelf.AddComponent<InteriorBookshelf>();
        bookshelf.Bind("Rak Buku", string.Empty, "Tekan E / Baca untuk membaca");
    }

    /// <summary>
    /// Menggambar latar interior gubuk (ruang bundar) secara prosedural dengan
    /// palet yang sama seperti peta, supaya tidak perlu aset luar.
    /// </summary>
    private static void GenerateInteriorArt()
    {
        int width = InteriorArtWidth;
        int height = InteriorArtHeight;
        Color[] pixels = new Color[width * height];
        System.Random random = new System.Random(20261116);

        Color stone = new Color(0.36f, 0.33f, 0.30f);
        Color stoneLight = new Color(0.48f, 0.44f, 0.39f);
        Color mortar = new Color(0.17f, 0.15f, 0.13f);
        Color beam = new Color(0.20f, 0.11f, 0.06f);
        Color beamLight = new Color(0.30f, 0.18f, 0.09f);
        Color floorBase = new Color(0.38f, 0.23f, 0.12f);
        Color floorLight = new Color(0.47f, 0.30f, 0.16f);
        Color floorSeam = new Color(0.22f, 0.12f, 0.06f);
        Color shelfDark = new Color(0.13f, 0.07f, 0.04f);

        const int wallBottom = 272;

        // Dinding batu.
        for (int y = 0; y < wallBottom; y++)
        {
            float heightRatio = 1f - (float)y / wallBottom;

            for (int x = 0; x < width; x++)
            {
                float noise = (float)(random.NextDouble() - 0.5) * 0.045f;
                Color color = Color.Lerp(stone, stoneLight, 0.12f + heightRatio * 0.55f);
                pixels[y * width + x] = new Color(color.r + noise, color.g + noise, color.b + noise, 1f);
            }
        }

        // Nat bata.
        for (int row = 0; row * 36 < wallBottom; row++)
        {
            int rowTop = row * 36;
            FillRect(pixels, width, height, 0, rowTop + 34, width - 1, rowTop + 35, mortar);

            int offset = row % 2 == 0 ? 0 : 40;
            for (int x = offset; x < width; x += 80)
            {
                FillRect(pixels, width, height, x, rowTop, x + 1, rowTop + 33, mortar);
            }
        }

        // Lantai papan kayu.
        for (int y = wallBottom; y < height; y++)
        {
            int plank = (y - wallBottom) / 26;
            float shade = 0.35f + (plank * 37 % 100) / 160f;
            Color baseColor = Color.Lerp(floorBase, floorLight, Mathf.Clamp01(shade));

            for (int x = 0; x < width; x++)
            {
                float noise = (float)(random.NextDouble() - 0.5) * 0.03f;
                pixels[y * width + x] = new Color(baseColor.r + noise, baseColor.g + noise, baseColor.b + noise, 1f);
            }
        }

        for (int y = wallBottom; y < height; y += 26)
        {
            FillRect(pixels, width, height, 0, y, width - 1, y + 1, floorSeam);

            int offset = (y / 26 % 3) * 90;
            for (int x = offset; x < width; x += 170)
            {
                FillRect(pixels, width, height, x, y + 2, x + 1, Mathf.Min(y + 25, height - 1), floorSeam);
            }
        }

        // Balok kayu di batas dinding dan lantai.
        FillRect(pixels, width, height, 0, wallBottom - 18, width - 1, wallBottom - 2, beam);
        FillRect(pixels, width, height, 0, wallBottom - 18, width - 1, wallBottom - 15, beamLight);

        // Permadani kecil di depan rak kiri.
        FillEllipse(pixels, width, height, 248, 308, 110, 30, new Color(0.31f, 0.14f, 0.11f));
        FillEllipse(pixels, width, height, 248, 308, 96, 24, new Color(0.47f, 0.23f, 0.17f));

        DrawBookshelf(pixels, width, height, 92, 112, 404, wallBottom, random);
        DrawBookshelf(pixels, width, height, 620, 112, 932, wallBottom, random);

        DrawDoor(pixels, width, height, shelfDark);

        // Meja rendah dengan kitab terbuka, di kanan pintu.
        FillRect(pixels, width, height, 654, 396, 664, 418, new Color(0.29f, 0.16f, 0.08f));
        FillRect(pixels, width, height, 776, 396, 786, 418, new Color(0.29f, 0.16f, 0.08f));
        FillRect(pixels, width, height, 644, 376, 796, 400, new Color(0.42f, 0.25f, 0.13f));
        FillRect(pixels, width, height, 644, 376, 796, 380, new Color(0.53f, 0.33f, 0.18f));
        FillRect(pixels, width, height, 644, 396, 796, 400, new Color(0.26f, 0.14f, 0.07f));

        FillRect(pixels, width, height, 676, 362, 764, 380, new Color(0.58f, 0.20f, 0.16f));
        FillRect(pixels, width, height, 682, 350, 758, 364, new Color(0.90f, 0.86f, 0.74f));
        FillRect(pixels, width, height, 719, 350, 721, 364, new Color(0.55f, 0.42f, 0.28f));

        // Lampu minyak di tengah langit-langit.
        DrawGlow(pixels, width, height, 512, 88, 120, 0.30f);
        FillRect(pixels, width, height, 511, 0, 513, 62, new Color(0.16f, 0.13f, 0.10f));
        FillRect(pixels, width, height, 496, 62, 528, 92, new Color(0.24f, 0.20f, 0.14f));
        FillRect(pixels, width, height, 502, 68, 522, 88, new Color(0.98f, 0.80f, 0.40f));

        // Vignette supaya sudut ruangan terasa gelap.
        for (int y = 0; y < height; y++)
        {
            float dy = (y - height * 0.5f) / (height * 0.5f);

            for (int x = 0; x < width; x++)
            {
                float dx = (x - width * 0.5f) / (width * 0.5f);
                float distance = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) - 0.55f);
                float darken = distance * 0.55f;
                Color color = pixels[y * width + x];
                pixels[y * width + x] = new Color(color.r * (1f - darken), color.g * (1f - darken),
                    color.b * (1f - darken), 1f);
            }
        }

        Color32[] output = new Color32[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            Color color = pixels[i];
            int x = i % width;
            int y = i / width;
            // Texture2D baris 0 = bawah, sedangkan gambar ini digambar dari atas.
            output[(height - 1 - y) * width + x] = new Color32(
                (byte)(Mathf.Clamp01(color.r) * 255f),
                (byte)(Mathf.Clamp01(color.g) * 255f),
                (byte)(Mathf.Clamp01(color.b) * 255f),
                255);
        }

        WriteTexturePng(InteriorArtPath, output, width, height);
        ConfigureInteriorSprite(InteriorArtPath);
        AssetDatabase.Refresh();
    }

    private static void WriteTexturePng(string path, Color32[] pixels, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets");
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static void DrawBookshelf(Color[] pixels, int width, int height,
        int x0, int y0, int x1, int y1, System.Random random)
    {
        Color wood = new Color(0.31f, 0.18f, 0.09f);
        Color woodLight = new Color(0.40f, 0.24f, 0.12f);
        Color back = new Color(0.11f, 0.06f, 0.03f);

        Color[] spines =
        {
            new Color(0.55f, 0.22f, 0.18f),
            new Color(0.24f, 0.36f, 0.22f),
            new Color(0.22f, 0.30f, 0.42f),
            new Color(0.62f, 0.50f, 0.22f),
            new Color(0.35f, 0.22f, 0.38f),
            new Color(0.58f, 0.48f, 0.34f),
            new Color(0.30f, 0.42f, 0.40f),
        };

        FillRect(pixels, width, height, x0, y0, x1, y1, wood);
        FillRect(pixels, width, height, x0 + 6, y0 + 6, x1 - 6, y1 - 8, back);
        FillRect(pixels, width, height, x0, y0, x1, y0 + 5, woodLight);

        int innerTop = y0 + 10;
        int innerBottom = y1 - 10;
        int rowHeight = (innerBottom - innerTop) / 3;

        for (int row = 0; row < 3; row++)
        {
            int boardY = innerTop + (row + 1) * rowHeight;
            int bookBottom = boardY - 2;

            int x = x0 + 12;
            while (x < x1 - 16)
            {
                int spineWidth = 7 + random.Next(0, 8);
                if (random.NextDouble() < 0.10)
                {
                    x += spineWidth + 6;
                    continue;
                }

                int spineHeight = 24 + random.Next(0, 18);
                Color color = spines[random.Next(spines.Length)];
                FillRect(pixels, width, height, x, bookBottom - spineHeight, x + spineWidth, bookBottom, color);

                // Garis tipis di punggung buku supaya tidak terlihat rata.
                FillRect(pixels, width, height, x + 2, bookBottom - spineHeight + 4, x + spineWidth - 2,
                    bookBottom - spineHeight + 5, new Color(color.r + 0.12f, color.g + 0.10f, color.b + 0.08f));

                x += spineWidth + 3;
            }

            FillRect(pixels, width, height, x0 + 4, boardY, x1 - 4, boardY + 5, wood);
            FillRect(pixels, width, height, x0 + 4, boardY, x1 - 4, boardY + 1, woodLight);
        }
    }

    private static void DrawDoor(Color[] pixels, int width, int height, Color dark)
    {
        const int centerX = 512;

        FillEllipse(pixels, width, height, centerX, 298, 64, 36, dark);
        FillRect(pixels, width, height, centerX - 62, 298, centerX + 62, height - 1, dark);

        // Cahaya matahari dari luar masuk lewat pintu.
        for (int y = 298; y < height; y++)
        {
            float vertical = (y - 298f) / (height - 298f);

            for (int x = centerX - 62; x <= centerX + 62; x++)
            {
                float horizontal = 1f - Mathf.Abs(x - centerX) / 62f;
                float glow = Mathf.Clamp01(vertical * 0.9f + horizontal * 0.2f) * 0.75f;
                Color warm = new Color(0.60f, 0.42f, 0.20f);
                Color baseColor = pixels[y * width + x];
                pixels[y * width + x] = Color.Lerp(baseColor, warm, glow);
            }
        }

        // Kusen pintu.
        FillRect(pixels, width, height, centerX - 70, 296, centerX - 62, height - 1, new Color(0.28f, 0.16f, 0.08f));
        FillRect(pixels, width, height, centerX + 63, 296, centerX + 71, height - 1, new Color(0.28f, 0.16f, 0.08f));
        FillRect(pixels, width, height, centerX - 62, 296, centerX + 63, 302, new Color(0.30f, 0.17f, 0.09f));
    }

    private static void DrawGlow(Color[] pixels, int width, int height,
        int centerX, int centerY, int radius, float strength)
    {
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            if (y < 0 || y >= height)
            {
                continue;
            }

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x < 0 || x >= width)
                {
                    continue;
                }

                float dx = (x - centerX) / (float)radius;
                float dy = (y - centerY) / (float)radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance > 1f)
                {
                    continue;
                }

                float alpha = (1f - distance) * (1f - distance) * strength;
                Color color = pixels[y * width + x];
                pixels[y * width + x] = new Color(
                    Mathf.Lerp(color.r, 1f, alpha),
                    Mathf.Lerp(color.g, 0.88f, alpha),
                    Mathf.Lerp(color.b, 0.60f, alpha),
                    1f);
            }
        }
    }

    private static void ConfigureInteriorSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Tidak ada TextureImporter di {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = InteriorPixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.isReadable = false;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteBorder = Vector4.zero;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // ---------------------------------------------------------------- UI

    private static readonly Color DarkInk = new Color(0.24f, 0.14f, 0.06f, 1f);

    private static void BuildUserInterface(GameObject player, Sprite circleSprite, Sprite panelSprite)
    {
        GameObject canvasObject = new GameObject("UI");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();

        PlayerMovement2D movement = player.GetComponent<PlayerMovement2D>();
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();

        // --- Joystick (paket aset tidak punya analog stick, jadi tetap bulatan) ---
        Image joystickBase = CreateImage(canvasObject.transform, "Joystick", circleSprite,
            new Vector2(300f, 300f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
            new Vector2(220f, 220f), new Color(1f, 1f, 1f, 0.2f));

        Image joystickHandle = CreateImage(joystickBase.transform, "Handle", circleSprite,
            new Vector2(130f, 130f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Color(1f, 1f, 1f, 0.55f));

        MobileJoystick joystick = joystickBase.gameObject.AddComponent<MobileJoystick>();
        joystick.Bind(joystickBase.rectTransform, joystickHandle.rectTransform, 90f);
        movement.Bind(joystick, playerRenderer);

        // --- Tombol interaksi (panel krem dari paket) ---
        Image buttonImage = CreateImage(canvasObject.transform, "InteractButton", panelSprite,
            new Vector2(220f, 220f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
            new Vector2(-180f, 200f), Color.white);
        StyleAsPackPanel(buttonImage);

        Button interactButton = buttonImage.gameObject.AddComponent<Button>();
        interactButton.targetGraphic = buttonImage;
        // Listener runtime tidak ikut tersimpan di scene; pakai persistent listener.
        UnityEventTools.AddPersistentListener(interactButton.onClick, interactor.InteractWithCurrent);

        CreateText(buttonImage.transform, "Label", "Baca", 34, FontStyle.Bold,
            DarkInk, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            TextAnchor.MiddleCenter);

        // --- Prompt interaksi ---
        Image promptImage = CreateImage(canvasObject.transform, "InteractPrompt", panelSprite,
            new Vector2(520f, 100f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -40f), Color.white);
        StyleAsPackPanel(promptImage);

        Text promptLabel = CreateText(promptImage.transform, "Label", "Tekan E", 30, FontStyle.Bold,
            DarkInk, Vector2.zero, Vector2.one, new Vector2(26f, 10f), new Vector2(-26f, -10f),
            TextAnchor.MiddleCenter);

        interactor.Bind(promptImage.gameObject, promptLabel);

        // --- Panel dialog ---
        Image panelImage = CreateImage(canvasObject.transform, "DialoguePanel", panelSprite,
            new Vector2(0f, 300f), new Vector2(0f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 140f), Color.white);
        StyleAsPackPanel(panelImage);

        RectTransform panelRect = panelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(-120f, 300f);
        panelRect.anchoredPosition = new Vector2(0f, 140f);

        Text titleText = CreateText(panelImage.transform, "Title", "Judul", 40, FontStyle.Bold,
            new Color(0.22f, 0.12f, 0.05f), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(54f, -86f), new Vector2(-54f, -24f), TextAnchor.MiddleLeft);

        Text bodyText = CreateText(panelImage.transform, "Body", "Teks materi", 30, FontStyle.Normal,
            DarkInk, new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(54f, 64f), new Vector2(-54f, -94f), TextAnchor.UpperLeft);

        Text hintText = CreateText(panelImage.transform, "Hint", string.Empty, 22, FontStyle.Italic,
            new Color(0.32f, 0.2f, 0.1f, 0.85f), new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(54f, 22f), new Vector2(-54f, 62f), TextAnchor.MiddleRight);

        DialogueUI dialogue = canvasObject.AddComponent<DialogueUI>();
        dialogue.Bind(panelImage.gameObject, titleText, bodyText, hintText);

        Button closeButton = panelImage.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = panelImage;
        // Panel = halaman berikutnya; halaman terakhir menutup dialog.
        UnityEventTools.AddPersistentListener(closeButton.onClick, dialogue.Advance);

        BuildHud(canvasObject.transform, panelSprite);
    }

    /// <summary>HUD kiri-atas: tiga hati dan jumlah hasil kebun.</summary>
    private static void BuildHud(Transform canvas, Sprite panelSprite)
    {
        Sprite heartFull = LoadHudSprite("Heart_Full");
        Sprite heartEmpty = LoadHudSprite("Heart_Empty");
        Sprite foodIcon = LoadHudSprite("Food_Apple");

        Image hud = CreateImage(canvas, "HudPanel", panelSprite, new Vector2(470f, 150f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -26f), new Color(1f, 1f, 1f, 0.94f));
        StyleAsPackPanel(hud);

        Image[] hearts = new Image[GameSession.MaxHearts];
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i] = CreateImage(hud.transform, $"Heart_{i + 1}", heartFull,
                new Vector2(54f, 50f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(26f + i * 66f, -20f), Color.white);
        }

        CreateImage(hud.transform, "FoodIcon", foodIcon, new Vector2(50f, 44f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(236f, -24f), Color.white);

        Text foodCount = CreateText(hud.transform, "FoodCount", "0", 42, FontStyle.Bold, DarkInk,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(296f, -80f), new Vector2(446f, -20f), TextAnchor.MiddleLeft);

        HudController hudController = canvas.transform.gameObject.AddComponent<HudController>();
        hudController.Bind(hearts, heartFull, heartEmpty, foodCount);
    }

    private static Sprite LoadHudSprite(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{HudFolderPath}/{name}.png");

        if (sprite == null)
        {
            Debug.LogWarning($"Ikon HUD tidak ditemukan: {HudFolderPath}/{name}.png");
        }

        return sprite;
    }

    /// <summary>Hasil kebun yang bisa diambil dengan berjalan menabraknya.</summary>
    private static readonly (string Name, string Icon, Vector2 Px)[] FoodSpots =
    {
        ("Apel", "Food_Apple", new Vector2(820f, 500f)),
        ("Roti", "Food_Bread", new Vector2(980f, 620f)),
        ("Pisang", "Food_Banana", new Vector2(480f, 420f)),
        ("Anggur", "Food_Grapes", new Vector2(430f, 430f)),
        ("Susu", "Food_Milk", new Vector2(560f, 890f)),
    };

    /// <summary>Cek posisi hasil kebun tidak berada di dalam bangunan/pagar.</summary>
    public static void ValidatePickupSpots()
    {
        foreach ((string name, string _, Vector2 px) in FoodSpots)
        {
            string problem = "OK";

            foreach ((string blockerName, Vector2 center, Vector2 size) in Blockers)
            {
                if (Mathf.Abs(px.x - center.x) < size.x * 0.5f + 14f &&
                    Mathf.Abs(px.y - center.y) < size.y * 0.5f + 14f)
                {
                    problem = $"tertutup {blockerName}";
                    break;
                }
            }

            if (problem == "OK")
            {
                foreach ((string fenceName, Vector2 a, Vector2 b, float thickness) in Fences)
                {
                    if (DistanceToSegment(px, a, b) < thickness * 0.5f + 14f)
                    {
                        problem = $"dekat pagar {fenceName}";
                        break;
                    }
                }
            }

            Debug.Log($"Pickup {name} ({px.x:0},{px.y:0}): {problem}");
        }
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSquared = ab.sqrMagnitude;
        if (lengthSquared < 0.0001f)
        {
            return Vector2.Distance(point, a);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
        return Vector2.Distance(point, a + ab * t);
    }

    private static void BuildFoodPickups(Vector2 mapPixels)
    {
        Transform parent = new GameObject("FoodPickups").transform;

        foreach ((string name, string icon, Vector2 px) in FoodSpots)
        {
            GameObject item = new GameObject($"Makanan_{name}");
            item.transform.SetParent(parent, false);
            item.transform.position = PixelToWorld(px, mapPixels);
            item.transform.localScale = Vector3.one * 1.7f;

            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadHudSprite(icon);
            renderer.sortingOrder = 8;

            BoxCollider2D trigger = item.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.35f, 0.35f);
            trigger.offset = new Vector2(0f, 0.12f);

            FoodPickup pickup = item.AddComponent<FoodPickup>();
            pickup.Bind(name);
        }
    }

    /// <summary>Makes an Image use the beige pack panel as a 9-slice box.</summary>
    private static void StyleAsPackPanel(Image image)
    {
        if (image == null || image.sprite == null)
        {
            return;
        }

        image.type = Image.Type.Sliced;
        // Border asli 8 px; 0.5 membuatnya ~16 UI px sehingga tetap terlihat di HP.
        image.pixelsPerUnitMultiplier = 0.5f;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 size,
        Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Color color)
    {
        RectTransform rect = CreateRect(parent, name);
        rect.sizeDelta = size;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize,
        FontStyle style, Color color, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(parent, name);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text text = rect.gameObject.AddComponent<Text>();
        text.font = GetBuiltinFont();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        text.supportRichText = true;
        return text;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return (RectTransform)gameObject.transform;
    }

    /// <summary>
    /// Unity 2022.2+ ships the legacy Arial replacement as LegacyRuntime.ttf.
    /// Older editors still expose Arial.ttf, so both are tried.
    /// </summary>
    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (font == null)
        {
            Debug.LogError("Font bawaan Unity tidak ditemukan; teks UI tidak akan tampil.");
        }

        return font;
    }

    // ---------------------------------------------------------------- util

    private static void RegisterScenes()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        AddSceneIfMissing(scenes, VillageScenePath);
        AddSceneIfMissing(scenes, InteriorScenePath);

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddSceneIfMissing(List<EditorBuildSettingsScene> scenes, string scenePath)
    {
        if (!File.Exists(scenePath))
        {
            return;
        }

        if (scenes.Exists(scene => scene.path == scenePath))
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
    }

    private static Vector2 PixelToWorld(Vector2 pixel, Vector2 mapPixels)
    {
        return new Vector2(
            (pixel.x - mapPixels.x * 0.5f) / MapImporter.PixelsPerUnit,
            (mapPixels.y * 0.5f - pixel.y) / MapImporter.PixelsPerUnit);
    }

    private static void FillRect(Color[] pixels, int width, int height,
        int xMin, int yMin, int xMax, int yMax, Color color)
    {
        xMin = Mathf.Clamp(xMin, 0, width - 1);
        xMax = Mathf.Clamp(xMax, 0, width - 1);
        yMin = Mathf.Clamp(yMin, 0, height - 1);
        yMax = Mathf.Clamp(yMax, 0, height - 1);

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                pixels[y * width + x] = color;
            }
        }
    }

    private static void FillCircle(Color[] pixels, int width, int height,
        int centerX, int centerY, int radius, Color color)
    {
        FillEllipse(pixels, width, height, centerX, centerY, radius, radius, color);
    }

    private static void FillEllipse(Color[] pixels, int width, int height,
        int centerX, int centerY, int radiusX, int radiusY, Color color)
    {
        for (int y = -radiusY; y <= radiusY; y++)
        {
            for (int x = -radiusX; x <= radiusX; x++)
            {
                float normalized = (x * x) / (float)(radiusX * radiusX) + (y * y) / (float)(radiusY * radiusY);
                if (normalized > 1f)
                {
                    continue;
                }

                int pixelX = centerX + x;
                int pixelY = centerY + y;

                if (pixelX < 0 || pixelX >= width || pixelY < 0 || pixelY >= height)
                {
                    continue;
                }

                pixels[pixelY * width + pixelX] = color;
            }
        }
    }
}
