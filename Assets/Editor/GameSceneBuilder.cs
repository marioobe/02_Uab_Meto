using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Membangun empat scene utama dari latar PNG hasil impor.
/// Semua posisi diturunkan dari seni: koordinat dunia =
/// (27.52 * nx, 15.36 * (1 - ny)) dengan (nx, ny) ternormalisasi dari kiri-atas.
/// Scene lama (VillageScene, RoundHouseInterior) tetap disimpan sebagai backup.
/// </summary>
public static class GameSceneBuilder
{
    private const float MapWidth = 27.52f;
    private const float MapHeight = 15.36f;
    private const string SceneFolder = "Assets/Scenes";
    private const string YuutaFolder = "Assets/Art/Characters/Yuuta";
    private const string AnakFolder = "Assets/Art/Characters/Anak";

    // Batas lantai tempat pemain boleh berdiri per scene. Titik interaksi objek
    // belajar tidak pernah lebih rendah dari ini supaya tetap terjangkau.
    private const float DapurFloorY = 2.6f;
    private const float RumahFloorY = 4.6f;
    private const float KebunFloorY = 3.8f;

    // Mode spotlight: latar lama sudah berisi semua benda. Sprite transparan tidak
    // digambar lagi supaya tidak dobel/berantakan; titik interaksi duduk tepat di
    // benda yang tergambar dan hanya halo yang tampil saat pemain mendekat.
    private const bool ShowLearningObjectSprites = false;

    private static Sprite circleSprite;

    [MenuItem("UAB Meto/3 - Build Game Scenes", priority = 3)]
    public static void BuildAll()
    {
        LearningDatabase database =
            AssetDatabase.LoadAssetAtPath<LearningDatabase>("Assets/Data/Learning/LearningDatabase.asset");
        if (database == null)
        {
            Debug.LogError("LearningDatabase belum ada. Jalankan 'UAB Meto/2 - Build Learning Data' dulu.");
            return;
        }

        Directory.CreateDirectory(SceneFolder);
        circleSprite = LoadSprite("Assets/Art/UI/UI_Circle.png");
        if (circleSprite == null)
        {
            Debug.LogWarning("UI_Circle.png tidak ditemukan; penanda interaksi tidak akan terlihat.");
        }

        BuildBaseMap(database);
        BuildDapur(database);
        BuildRumah(database);
        BuildKebun(database);
        ApplyBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("GameSceneBuilder selesai: Base_Map, Dapur, Rumah, Kebun.");
    }

    // ---------------------------------------------------------------- Base_Map

    private static void BuildBaseMap(LearningDatabase database)
    {
        Scene scene = BeginScene("Base_Map", out CameraFollow2D follow);
        Transform root = WorldRoot("WorldColliders");
        Transform markers = WorldRoot("Markers");

        // Batas peta.
        Box("Boundary_Top", new Vector2(13.76f, 14.7f), new Vector2(28.6f, 1.3f), root);
        Box("Boundary_Left", new Vector2(-0.35f, 7.68f), new Vector2(1.3f, 16.8f), root);
        Box("Boundary_Right", new Vector2(27.9f, 7.68f), new Vector2(1.3f, 16.8f), root);

        // Pagar depan. Gerbang utama terbuka di x 12,25 .. 14,6.
        Box("Fence_Bottom_Left", new Vector2(5.64f, 1.85f), new Vector2(11.28f, 1.55f), root);
        Box("Fence_Bottom_Right", new Vector2(21.6f, 1.85f), new Vector2(11.83f, 1.55f), root);
        Box("GatePillar_Left", new Vector2(11.7f, 1.6f), new Vector2(1.1f, 1.7f), root);
        Box("GatePillar_Right", new Vector2(15.15f, 1.6f), new Vector2(1.1f, 1.7f), root);
        Box("Boundary_Gate", new Vector2(13.35f, 0.45f), new Vector2(3.4f, 1.0f), root);

        // Sungai di kiri. Jembatan menjadi celah y 8,95 .. 10,35.
        Box("River_Lower", new Vector2(3.3f, 4.48f), new Vector2(6.2f, 8.95f), root);
        Box("River_Upper", new Vector2(3.3f, 12.86f), new Vector2(6.2f, 5.0f), root);

        // Bangunan: rumah bulat (Dapur) dan rumah kayu (Rumah).
        Box("House_Round", new Vector2(8.74f, 11.9f), new Vector2(4.6f, 2.2f), root);
        Box("House_Wood", new Vector2(20.1f, 12.35f), new Vector2(5.4f, 2.6f), root);
        Circle("Well", new Vector2(8.3f, 7.3f), 1.0f, root);

        // Pagar kebun. Gerbang di x 21,7 .. 22,9.
        Box("Kebun_Fence_Top", new Vector2(20.9f, 8.55f), new Vector2(6.1f, 0.35f), root);
        Box("Kebun_Fence_Left", new Vector2(18.05f, 6.35f), new Vector2(0.35f, 4.5f), root);
        Box("Kebun_Fence_Right", new Vector2(23.8f, 6.35f), new Vector2(0.35f, 4.5f), root);
        Box("Kebun_Fence_BottomLeft", new Vector2(19.85f, 4.15f), new Vector2(3.7f, 0.35f), root);
        Box("Kebun_Fence_BottomRight", new Vector2(23.37f, 4.15f), new Vector2(0.95f, 0.35f), root);
        Box("Kebun_Gate_Block", new Vector2(22.3f, 4.4f), new Vector2(1.6f, 0.4f), root);

        // Pohon besar.
        Circle("Tree_Left", new Vector2(4.3f, 6.3f), 0.5f, root);
        Circle("Tree_LowerLeft", new Vector2(6.5f, 2.9f), 0.5f, root);
        Circle("Tree_CenterBottom", new Vector2(16.9f, 3.9f), 0.5f, root);
        Circle("Tree_TopRight", new Vector2(26.0f, 10.4f), 0.55f, root);
        Circle("Tree_Right", new Vector2(26.2f, 6.4f), 0.55f, root);
        Circle("Tree_BottomRight", new Vector2(26.3f, 1.7f), 0.55f, root);

        Entry("Start", new Vector2(13.35f, 2.5f));
        Entry("FromDapur", new Vector2(8.95f, 9.5f));
        Entry("FromRumah", new Vector2(20.2f, 9.8f));
        Entry("FromKebun", new Vector2(22.3f, 3.3f));

        Door("Door_Dapur", new Vector2(8.95f, 10.5f), new Vector2(1.8f, 1.1f),
            "Dapur", "Start", DoorLock.None, null, new Color(0.35f, 0.9f, 1f, 0.35f));
        Door("Door_Rumah", new Vector2(20.2f, 11.0f), new Vector2(1.8f, 1.1f),
            "Rumah", "Start", DoorLock.AfterDapur,
            "Rumah warga masih terkunci. Selesaikan materi dan kuis di Dapur Bulat dahulu.",
            new Color(1f, 0.7f, 0.3f, 0.4f));
        Door("Door_Kebun", new Vector2(22.3f, 4.15f), new Vector2(1.6f, 1.0f),
            "Kebun", "Start", DoorLock.None, null, new Color(0.55f, 1f, 0.55f, 0.35f));

        // Pemandu berdiri di rumput kiri jalur, tepat di depan pemain saat mulai.
        CreateNpc(new Vector2(11.9f, 4.3f), markers);
        CreatePlayer(new Vector2(13.35f, 2.5f),
            out PlayerMovement2D player, out PlayerInteractor interactor);

        Finish(scene, "Base_Map", "Kampung", player, interactor, follow, database);
    }

    // ---------------------------------------------------------------- Dapur

    private static void BuildDapur(LearningDatabase database)
    {
        Scene scene = BeginScene("Dapur", out CameraFollow2D follow);
        Transform root = WorldRoot("WorldColliders");
        Transform markers = WorldRoot("Markers");

        // Pagar bulat mengikuti dinding anyaman pada seni lama: bagian dalam kira-kira
        // x 2,0..25,5 dan y 3,9..11,9. Celah 262..278 derajat adalah pintu di bawah.
        BuildEllipseFence(root, new Vector2(13.76f, 7.9f), 11.6f, 4.0f, 40, 262f, 278f);
        // Koridor pendek dari celah pagar ke pintu keluar (y 1,75) supaya pemain tidak
        // berjalan menembus pagar depan.
        Box("DoorCorridor_Left", new Vector2(12.3f, 2.6f), new Vector2(0.3f, 2.9f), root);
        Box("DoorCorridor_Right", new Vector2(15.2f, 2.6f), new Vector2(0.3f, 2.9f), root);
        Box("Boundary_Bottom", new Vector2(13.76f, 0.3f), new Vector2(28.6f, 0.6f), root);
        Box("Boundary_Left", new Vector2(-0.3f, 7.68f), new Vector2(1.0f, 16.8f), root);
        Box("Boundary_Right", new Vector2(27.9f, 7.68f), new Vector2(1.0f, 16.8f), root);
        Box("Boundary_Top", new Vector2(13.76f, 15.2f), new Vector2(28.6f, 0.8f), root);

        // Pemain masuk lebih ke dalam supaya prompt "Masuk" tidak langsung muncul.
        Entry("Start", new Vector2(13.76f, 3.6f));
        Door("Exit_Dapur", new Vector2(13.76f, 1.75f), new Vector2(2.2f, 1.0f),
            "Base_Map", "FromDapur", DoorLock.None, null, new Color(0.9f, 0.9f, 0.9f, 0.3f), "Keluar");

        AddLearningObject(database, markers, "Dapur", "periuk", DapurFloorY);
        AddLearningObject(database, markers, "Dapur", "tungku", DapurFloorY);
        AddLearningObject(database, markers, "Dapur", "kayu_bakar", DapurFloorY);
        AddLearningObject(database, markers, "Dapur", "jagung", DapurFloorY);
        AddLearningObject(database, markers, "Dapur", "tikar", DapurFloorY);
        AddLearningObject(database, markers, "Dapur", "tempayan", DapurFloorY);
        AddLearningObject(database, markers, "Dapur", "bakul", DapurFloorY);
        AddLearningObject(database, markers, "Dapur", "alat_masak", DapurFloorY);
        AddLearningObject(database, markers, "Dapur", "cobek", DapurFloorY);
        AddLearningObject(database, markers, "Dapur", "bangku", DapurFloorY);

        CreatePlayer(new Vector2(13.76f, 3.6f),
            out PlayerMovement2D player, out PlayerInteractor interactor);

        Finish(scene, "Dapur", "Dapur Bulat", player, interactor, follow, database);
    }

    // ---------------------------------------------------------------- Rumah

    private static void BuildRumah(LearningDatabase database)
    {
        Scene scene = BeginScene("Rumah", out CameraFollow2D follow);
        Transform root = WorldRoot("WorldColliders");
        Transform markers = WorldRoot("Markers");

        // Dinding: lantai dalam berada di y 3,76 .. 12,5, pintu di x 12,1 .. 15,1.
        // Wall_Top berhenti di dasar batu dinding belakang (y ~12,5) agar pemain tidak
        // bisa naik ke dinding belakang.
        Box("Wall_Top", new Vector2(13.76f, 13.35f), new Vector2(25.6f, 1.7f), root);
        Box("Wall_Left", new Vector2(1.6f, 8.2f), new Vector2(1.2f, 14.0f), root);
        Box("Wall_Right", new Vector2(24.7f, 8.2f), new Vector2(1.2f, 14.0f), root);
        Box("Wall_BottomLeft", new Vector2(6.55f, 2.8f), new Vector2(11.1f, 1.9f), root);
        Box("Wall_BottomRight", new Vector2(20.2f, 2.8f), new Vector2(10.2f, 1.9f), root);
        // Koridor pintu depan: menjaga pemain tetap di dalam rumah, tidak berjalan di
        // atas pondasi batu depan.
        Box("DoorCorridor_Left", new Vector2(12.15f, 1.9f), new Vector2(0.3f, 2.6f), root);
        Box("DoorCorridor_Right", new Vector2(15.05f, 1.9f), new Vector2(0.3f, 2.6f), root);
        Box("Boundary_Bottom", new Vector2(13.76f, 0.35f), new Vector2(28.6f, 0.7f), root);

        Entry("Start", new Vector2(13.6f, 5.4f));
        Door("Exit_Rumah", new Vector2(13.6f, 2.8f), new Vector2(2.4f, 2.6f),
            "Base_Map", "FromRumah", DoorLock.None, null, new Color(0.9f, 0.9f, 0.9f, 0.3f), "Keluar");

        AddLearningObject(database, markers, "Rumah", "tempat_tidur", RumahFloorY);
        AddLearningObject(database, markers, "Rumah", "selimut_tenun", RumahFloorY);
        AddLearningObject(database, markers, "Rumah", "lemari", RumahFloorY);
        AddLearningObject(database, markers, "Rumah", "peti_kayu", RumahFloorY);
        AddLearningObject(database, markers, "Rumah", "rak_piring", RumahFloorY);
        AddLearningObject(database, markers, "Rumah", "meja_makan", RumahFloorY);
        AddLearningObject(database, markers, "Rumah", "tikar_rumah", RumahFloorY);
        AddLearningObject(database, markers, "Rumah", "tempayan_rumah", RumahFloorY);
        AddLearningObject(database, markers, "Rumah", "keranjang", RumahFloorY);
        AddLearningObject(database, markers, "Rumah", "bangku_rumah", RumahFloorY);

        CreatePlayer(new Vector2(13.6f, 5.4f),
            out PlayerMovement2D player, out PlayerInteractor interactor);

        Finish(scene, "Rumah", "Rumah Warga", player, interactor, follow, database);
    }

    // ---------------------------------------------------------------- Kebun

    private static void BuildKebun(LearningDatabase database)
    {
        Scene scene = BeginScene("Kebun", out CameraFollow2D follow);
        Transform root = WorldRoot("WorldColliders");
        Transform markers = WorldRoot("Markers");

        // Pagar keliling dengan gerbang bambu di x 12,25 .. 15,0.
        Box("Fence_Top", new Vector2(13.76f, 14.5f), new Vector2(28.6f, 1.6f), root);
        Box("Fence_Left", new Vector2(2.2f, 8.2f), new Vector2(1.1f, 13.2f), root);
        Box("Fence_Right", new Vector2(25.85f, 8.2f), new Vector2(1.3f, 13.2f), root);
        Box("Fence_BottomLeft", new Vector2(6.95f, 2.5f), new Vector2(10.6f, 1.5f), root);
        Box("Fence_BottomRight", new Vector2(20.45f, 2.5f), new Vector2(10.9f, 1.5f), root);
        Box("Boundary_Bottom", new Vector2(13.76f, 0.4f), new Vector2(28.6f, 0.8f), root);

        // Gudang, sumur, pohon, dan tumpukan kayu.
        Box("Shed", new Vector2(21.5f, 12.9f), new Vector2(6.2f, 4.2f), root);
        Circle("Well", new Vector2(3.55f, 8.3f), 0.9f, root);
        Circle("Tree_Orange", new Vector2(3.3f, 9.7f), 0.45f, root);
        Circle("Tree_Mango", new Vector2(17.7f, 13.0f), 0.5f, root);
        Circle("Tree_Right", new Vector2(25.4f, 3.4f), 0.5f, root);
        Circle("Palm_Mid", new Vector2(8.3f, 4.3f), 0.4f, root);
        Box("WoodPile", new Vector2(2.9f, 6.85f), new Vector2(1.2f, 0.6f), root);

        Entry("Start", new Vector2(13.76f, 4.6f));
        Door("Exit_Kebun", new Vector2(13.76f, 2.6f), new Vector2(2.5f, 2.0f),
            "Base_Map", "FromKebun", DoorLock.None, null, new Color(0.9f, 0.9f, 0.9f, 0.3f), "Keluar");

        AddLearningObject(database, markers, "Kebun", "pohon_jeruk", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "pohon_kelapa", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "pohon_mangga", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "kebun_sayur", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "tomat", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "cabai", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "terong", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "bakul_hasil", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "sumur", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "alat_kebun", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "bibit", KebunFloorY);
        AddLearningObject(database, markers, "Kebun", "daun_pisang", KebunFloorY);

        Sprite apple = LoadSprite("Assets/Art/UI/Hud/Food_Apple.png");
        Sprite banana = LoadSprite("Assets/Art/UI/Hud/Food_Banana.png");
        AddFruit(markers, "kebun_buah_1", new Vector2(6.5f, 6.4f), apple, false,
            new Color(1f, 0.93f, 0.55f, 1f));
        AddFruit(markers, "kebun_buah_2", new Vector2(10.2f, 9.4f), apple, false,
            new Color(1f, 0.93f, 0.55f, 1f));
        AddFruit(markers, "kebun_buah_3", new Vector2(19.6f, 9.8f), apple, false,
            new Color(1f, 0.93f, 0.55f, 1f));
        AddFruit(markers, "kebun_buah_4", new Vector2(22.8f, 4.4f), apple, false,
            new Color(1f, 0.93f, 0.55f, 1f));
        AddFruit(markers, "kebun_buah_5", new Vector2(9.5f, 4.2f), apple, false,
            new Color(1f, 0.93f, 0.55f, 1f));
        AddFruit(markers, "kebun_darurat", new Vector2(4.4f, 3.9f), banana, true,
            new Color(1f, 0.7f, 0.3f, 1f));

        CreatePlayer(new Vector2(13.76f, 4.6f),
            out PlayerMovement2D player, out PlayerInteractor interactor);

        Finish(scene, "Kebun", "Kebun", player, interactor, follow, database);
    }

    // ---------------------------------------------------------------- komponen umum

    private static Scene BeginScene(string name, out CameraFollow2D follow)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Semua scene memakai latar lama (perabot/pohon sudah digambar). Sprite objek
        // transparan baru ditambahkan di sela kosong pada latar tersebut.
        Sprite backgroundSprite = LoadSprite(GameArtImporter.SceneSpritePath(name));
        if (backgroundSprite == null)
        {
            Debug.LogError($"Latar {name} belum diimpor. Jalankan 'UAB Meto/1 - Import Scene Art'.");
        }

        GameObject background = new GameObject("Background_" + name);
        background.transform.position = new Vector3(MapWidth * 0.5f, MapHeight * 0.5f, 0f);
        SpriteRenderer renderer = background.AddComponent<SpriteRenderer>();
        renderer.sprite = backgroundSprite;
        renderer.sortingOrder = -100;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(MapWidth * 0.5f, MapHeight * 0.5f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.09f, 0.06f, 1f);
        cameraObject.AddComponent<AudioListener>();
        follow = cameraObject.AddComponent<CameraFollow2D>();

        return scene;
    }

    private static Transform WorldRoot(string name)
    {
        GameObject root = new GameObject(name);
        root.transform.position = Vector3.zero;
        return root.transform;
    }

    private static void CreatePlayer(Vector2 spawn, out PlayerMovement2D movement,
        out PlayerInteractor interactor)
    {
        // Spritesheet Yuuta 4x4: 4 arah x 4 frame.
        Sprite[] idleDown = Row2("idle_down");
        Sprite[] walkDown = Row2("walk_down");
        Sprite[] walkSide = Row2("walk_side");
        Sprite[] walkUp = Row2("walk_up");
        Sprite[] idleSide = walkSide.Length > 0 ? new[] { walkSide[0] } : new Sprite[0];
        Sprite[] idleUp = walkUp.Length > 0 ? new[] { walkUp[0] } : new Sprite[0];

        GameObject go = new GameObject("Player");
        go.transform.position = spawn;
        // PPU 300 -> tinggi ~1,58 unit; skala 1.
        go.transform.localScale = Vector3.one;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = idleDown != null && idleDown.Length > 0 ? idleDown[0] : null;

        YSort sort = go.AddComponent<YSort>();
        sort.mapHeight = MapHeight;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.26f;
        collider.offset = new Vector2(0f, 0.30f);

        PlayerAnimator2D animator = go.AddComponent<PlayerAnimator2D>();
        animator.Bind(renderer, idleDown, walkDown, idleSide, walkSide, idleUp, walkUp);

        movement = go.AddComponent<PlayerMovement2D>();
        movement.BindAnimator(animator);

        interactor = go.AddComponent<PlayerInteractor>();
    }

    private static void CreateNpc(Vector2 position, Transform markers)
    {
        GameObject npc = new GameObject("Guide_NPC");
        npc.transform.position = position;
        // Sprite ketua adat 924 px / PPU 300 = 3,08 unit (2x pemain). Skala 0,62
        // membuatnya ~1,9 unit, sedikit lebih tinggi dari pemain.
        npc.transform.localScale = Vector3.one * 0.62f;

        SpriteRenderer renderer = npc.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite($"{AnakFolder}/ketua_adat.png");

        YSort sort = npc.AddComponent<YSort>();
        sort.mapHeight = MapHeight;

        CircleCollider2D collider = npc.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.7f;
        collider.offset = new Vector2(0f, 0.9f);

        NpcInteractable script = npc.AddComponent<NpcInteractable>();
        script.Bind("Pemandu Desa", new[]
        {
            "Selamat datang di kampung Uab Meto.",
            "Di sini kamu bisa belajar nama benda dalam bahasa Dawan.",
            "Mulai dari Dapur Bulat: baca semua benda, lalu jawab kuisnya.",
            "Rumah warga akan terbuka setelah kamu menyelesaikan Dapur.",
            "Kebun selalu terbuka. Buah di sana bisa memulihkan hati."
        }, true);

        Marker("Marker_NPC", position + new Vector2(0f, 1.1f), new Color(0.7f, 0.6f, 1f, 0.3f),
            0.6f, markers);
    }

    /// <summary>
    /// Titik interaksi = tengah-bawah sprite. Posisi diambil dari tabel penempatan
    /// (sela kosong latar lama); bila tidak ada, fallback ke crop seni lama.
    /// </summary>
    private static void AddLearningObject(LearningDatabase database, Transform markers,
        string sceneName, string id, float floorY)
    {
        Vector2 position;
        if (AsetTransparan23Importer.TryGetPlacement(sceneName, id, out Vector2 normalized))
        {
            position = new Vector2(normalized.x * MapWidth, (1f - normalized.y) * MapHeight);
        }
        else
        {
            position = GameArtImporter.CropWorldPosition(sceneName, id, MapWidth, MapHeight);
        }

        position.x = Mathf.Clamp(position.x, 0.9f, MapWidth - 0.9f);
        position.y = Mathf.Clamp(position.y, floorY, MapHeight - 0.9f);

        GameObject go = new GameObject("Learn_" + id);
        go.transform.position = position;

        // Mode spotlight: sprite transparan hanya dibuat bila flag di atas aktif.
        if (ShowLearningObjectSprites && sceneName != "Base_Map")
        {
            Sprite art = LoadSprite(AsetTransparan23Importer.ObjectSpritePath(sceneName, id));
            if (art != null)
            {
                SpriteRenderer viewRenderer = go.AddComponent<SpriteRenderer>();
                viewRenderer.sprite = art;
                LearningObjectView view = go.AddComponent<LearningObjectView>();
                view.Bind(art, MapHeight, AsetTransparan23Importer.IsFlat(id) ? -0.35f : 0f);
            }
        }

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.55f;

        LearningObjectInteractable interactable = go.AddComponent<LearningObjectInteractable>();
        interactable.Bind(database, id);

        Marker("Marker_" + id, position + new Vector2(0f, 0.32f),
            new Color(1f, 0.86f, 0.25f, 0.28f), 0.42f, markers, 0.10f, 0.34f);
    }

    private static void AddFruit(Transform parent, string id, Vector2 position, Sprite sprite,
        bool rescue, Color tint)
    {
        GameObject go = new GameObject("Fruit_" + id);
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 1.6f;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.sortingOrder = 6;

        InteractMarker pulse = go.AddComponent<InteractMarker>();
        pulse.minAlpha = 0.55f;
        pulse.maxAlpha = 1f;
        pulse.pulseSpeed = 2.6f;

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.4f;

        FruitPickup pickup = go.AddComponent<FruitPickup>();
        pickup.Bind(id, rescue, 1);
    }

    private static void Marker(string name, Vector2 position, Color color, float scale,
        Transform parent, float minAlpha = 0.16f, float maxAlpha = 0.46f)
    {
        if (circleSprite == null)
        {
            return;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = color;
        // Di belakang objek/pemain: lingkaran hanya tampak sebagai halo di sekitar
        // kaki objek sehingga sprite belajar tetap jelas terlihat.
        renderer.sortingOrder = 4;
        InteractMarker pulse = go.AddComponent<InteractMarker>();
        pulse.minAlpha = minAlpha;
        pulse.maxAlpha = maxAlpha;
    }

    private static void Door(string name, Vector2 position, Vector2 size, string targetScene,
        string entryId, DoorLock lockKind, string lockedMessage, Color markerColor, string prompt = null)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = size;

        DoorInteractable door = go.AddComponent<DoorInteractable>();
        door.Bind(targetScene, entryId, lockKind, lockedMessage, prompt);

        // Penanda pintu: warna hangat, alpha rendah supaya tidak terlihat seperti lubang.
        Color tint = Color.Lerp(markerColor, new Color(1f, 0.83f, 0.45f, 1f), 0.7f);
        tint.a = 0.32f;
        Marker("Marker_" + name, position + new Vector2(0f, 0.2f), tint, 0.62f,
            WorldRoot("Markers_" + name), 0.07f, 0.18f);
    }

    private static void Entry(string id, Vector2 position)
    {
        GameObject go = new GameObject("Entry_" + id);
        go.transform.position = position;
        EntryPoint point = go.AddComponent<EntryPoint>();
        point.id = id;
    }

    private static void BuildEllipseFence(Transform parent, Vector2 center, float radiusX,
        float radiusY, int segments, float gapStart, float gapEnd)
    {
        for (int i = 0; i < segments; i++)
        {
            float degrees = i * (360f / segments);
            if (degrees >= gapStart && degrees <= gapEnd)
            {
                continue;
            }

            float radians = degrees * Mathf.Deg2Rad;
            Vector2 point = new Vector2(
                center.x + radiusX * Mathf.Cos(radians),
                center.y + radiusY * Mathf.Sin(radians));

            GameObject go = new GameObject($"Fence_{i:00}");
            go.transform.SetParent(parent, true);
            go.transform.position = point;
            go.transform.rotation = Quaternion.Euler(0f, 0f, degrees + 90f);

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2.1f, 0.5f);
        }
    }

    private static void Box(string name, Vector2 center, Vector2 size, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = center;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static void Circle(string name, Vector2 center, float radius, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = center;

        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = radius;
    }

    private static void Finish(Scene scene, string key, string title, PlayerMovement2D player,
        PlayerInteractor interactor, CameraFollow2D follow, LearningDatabase database)
    {
        GameObject uiObject = new GameObject("GameUI");
        GameUi ui = uiObject.AddComponent<GameUi>();
        ui.panelSprite = LoadSprite("Assets/Art/UI/UI_Panel_Beige.png");
        ui.circleSprite = LoadSprite("Assets/Art/UI/UI_Circle.png");
        ui.heartFull = LoadSprite($"{AsetTransparan23Importer.UiFolder}/heart_full.png")
                        ?? LoadSprite("Assets/Art/UI/Hud/Heart_Full.png");
        ui.heartEmpty = LoadSprite($"{AsetTransparan23Importer.UiFolder}/heart_empty.png")
                        ?? LoadSprite("Assets/Art/UI/Hud/Heart_Empty.png");
        ui.fruitIcon = LoadSprite("Assets/Art/UI/Hud/Food_Apple.png");
        ui.dialogPanel = LoadSprite($"{AsetTransparan23Importer.UiFolder}/dialog_panel.png");
        ui.settingsIcon = LoadSprite($"{AsetTransparan23Importer.UiFolder}/settings.png");
        ui.dpadUp = LoadSprite($"{AsetTransparan23Importer.UiFolder}/dpad_up.png");
        ui.dpadDown = LoadSprite($"{AsetTransparan23Importer.UiFolder}/dpad_down.png");
        ui.dpadLeft = LoadSprite($"{AsetTransparan23Importer.UiFolder}/dpad_left.png");
        ui.dpadRight = LoadSprite($"{AsetTransparan23Importer.UiFolder}/dpad_right.png");

        GameObject controllerObject = new GameObject("SceneController");
        SceneController controller = controllerObject.AddComponent<SceneController>();
        controller.sceneKey = key;
        controller.sceneTitle = title;
        controller.defaultEntryId = "Start";
        controller.mapSize = new Vector2(MapWidth, MapHeight);
        controller.database = database;
        controller.player = player;
        controller.interactor = interactor;
        controller.followCamera = follow;
        controller.ui = ui;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, $"{SceneFolder}/{key}.unity");
    }

    private static void ApplyBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{SceneFolder}/Base_Map.unity", true),
            new EditorBuildSettingsScene($"{SceneFolder}/Dapur.unity", true),
            new EditorBuildSettingsScene($"{SceneFolder}/Rumah.unity", true),
            new EditorBuildSettingsScene($"{SceneFolder}/Kebun.unity", true)
        };
    }

    /// <summary>
    /// Menulis posisi semua titik interaksi ke log supaya bisa dicocokkan dengan seni.
    /// </summary>
    [MenuItem("UAB Meto/4 - Verify Scene Layout", priority = 4)]
    public static void VerifyLayout()
    {
        string[] keys = { "Base_Map", "Dapur", "Rumah", "Kebun" };
        for (int i = 0; i < keys.Length; i++)
        {
            Scene scene = EditorSceneManager.OpenScene($"{SceneFolder}/{keys[i]}.unity",
                OpenSceneMode.Single);
            Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (Transform transform in all)
            {
                string name = transform.name;
                if (!name.StartsWith("Learn_") && !name.StartsWith("Entry_") &&
                    !name.StartsWith("Door_") && !name.StartsWith("Exit_") &&
                    !name.StartsWith("Fruit_") && name != "Player")
                {
                    continue;
                }

                Debug.Log($"LAYOUT {keys[i]}: {name} @ ({transform.position.x:F2}, " +
                          $"{transform.position.y:F2})");
            }
        }
    }

    private static Sprite[] Row2(string rowName)
    {
        List<Sprite> frames = new List<Sprite>();
        for (int i = 1; i <= 4; i++)
        {
            Sprite sprite = LoadSprite($"Assets/Art/Characters/Yuuta2/yuuta2_{rowName}_{i}.png");
            if (sprite != null)
            {
                frames.Add(sprite);
            }
        }

        return frames.ToArray();
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
