using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Mengubah empat JPEG scene menjadi PNG di Assets/Art/Scenes dan memotong
/// gambar objek belajar ke Assets/Art/Learning/<scene>/<id>.png.
/// Sumber asli di Assets/foto/Gambar_Scene_Baru tidak pernah diubah.
/// </summary>
public static class GameArtImporter
{
    public const string SourceFolder = "Assets/foto/Gambar_Scene_Baru";
    public const string SceneArtFolder = "Assets/Art/Scenes";
    public const string LearningArtFolder = "Assets/Art/Learning";

    public static readonly string[] SceneNames = { "Base_Map", "Dapur", "Rumah", "Kebun" };

    private struct CropSpec
    {
        public string Scene;
        public string Id;
        public Rect Normalized;

        public CropSpec(string scene, string id, float x, float y, float width, float height)
        {
            Scene = scene;
            Id = id;
            Normalized = new Rect(x, y, width, height);
        }
    }

    // Koordinat ternormalisasi (0..1) dari kiri-atas gambar sumber, diukur dari
    // gambar asli 2752x1536 memakai kisi 10%. Titik interaksi = tengah-bawah kotak.
    private static readonly CropSpec[] Crops =
    {
        // Dapur: tungku di tengah, jagung menggantung di tiang, tikar di kanan.
        new CropSpec("Dapur", "periuk", 0.325f, 0.385f, 0.075f, 0.095f),
        new CropSpec("Dapur", "tungku", 0.320f, 0.430f, 0.150f, 0.150f),
        new CropSpec("Dapur", "kayu_bakar", 0.475f, 0.440f, 0.060f, 0.120f),
        new CropSpec("Dapur", "jagung", 0.410f, 0.170f, 0.180f, 0.210f),
        new CropSpec("Dapur", "tikar", 0.600f, 0.190f, 0.190f, 0.220f),
        new CropSpec("Dapur", "tempayan", 0.250f, 0.160f, 0.060f, 0.120f),
        new CropSpec("Dapur", "bakul", 0.200f, 0.190f, 0.050f, 0.110f),
        new CropSpec("Dapur", "alat_masak", 0.300f, 0.040f, 0.130f, 0.160f),
        new CropSpec("Dapur", "cobek", 0.260f, 0.500f, 0.060f, 0.080f),
        new CropSpec("Dapur", "bangku", 0.410f, 0.590f, 0.060f, 0.060f),

        // Rumah: kasur di kiri, rak piring di tengah, meja makan di kanan.
        new CropSpec("Rumah", "tempat_tidur", 0.100f, 0.180f, 0.130f, 0.160f),
        new CropSpec("Rumah", "selimut_tenun", 0.110f, 0.410f, 0.070f, 0.140f),
        new CropSpec("Rumah", "lemari", 0.300f, 0.150f, 0.090f, 0.160f),
        new CropSpec("Rumah", "peti_kayu", 0.320f, 0.670f, 0.080f, 0.070f),
        new CropSpec("Rumah", "rak_piring", 0.550f, 0.100f, 0.080f, 0.180f),
        new CropSpec("Rumah", "meja_makan", 0.660f, 0.270f, 0.190f, 0.120f),
        new CropSpec("Rumah", "tikar_rumah", 0.620f, 0.380f, 0.200f, 0.200f),
        new CropSpec("Rumah", "tempayan_rumah", 0.840f, 0.220f, 0.060f, 0.110f),
        new CropSpec("Rumah", "keranjang", 0.565f, 0.655f, 0.055f, 0.080f),
        new CropSpec("Rumah", "bangku_rumah", 0.710f, 0.400f, 0.070f, 0.050f),

        // Kebun: pohon di tepi, petak sayur di tengah, gudang di kanan atas.
        new CropSpec("Kebun", "pohon_jeruk", 0.050f, 0.140f, 0.140f, 0.200f),
        new CropSpec("Kebun", "pohon_kelapa", 0.110f, 0.000f, 0.120f, 0.180f),
        new CropSpec("Kebun", "pohon_mangga", 0.590f, 0.000f, 0.080f, 0.150f),
        new CropSpec("Kebun", "kebun_sayur", 0.180f, 0.150f, 0.190f, 0.200f),
        new CropSpec("Kebun", "tomat", 0.450f, 0.140f, 0.100f, 0.160f),
        new CropSpec("Kebun", "cabai", 0.570f, 0.500f, 0.100f, 0.220f),
        new CropSpec("Kebun", "terong", 0.180f, 0.550f, 0.150f, 0.200f),
        new CropSpec("Kebun", "bakul_hasil", 0.390f, 0.270f, 0.070f, 0.100f),
        new CropSpec("Kebun", "sumur", 0.100f, 0.410f, 0.060f, 0.110f),
        new CropSpec("Kebun", "alat_kebun", 0.860f, 0.180f, 0.060f, 0.140f),
        new CropSpec("Kebun", "bibit", 0.220f, 0.620f, 0.060f, 0.060f),
        new CropSpec("Kebun", "daun_pisang", 0.020f, 0.550f, 0.090f, 0.240f),
    };

    [MenuItem("UAB Meto/1 - Import Scene Art", priority = 1)]
    public static void ImportAll()
    {
        Directory.CreateDirectory(SceneArtFolder);

        for (int i = 0; i < SceneNames.Length; i++)
        {
            ConvertScene(SceneNames[i]);
        }

        AssetDatabase.Refresh();
        ImportCrops();
        AssetDatabase.SaveAssets();
        Debug.Log($"GameArtImporter selesai: {SceneNames.Length} latar + {Crops.Length} gambar objek.");
    }

    public static string SceneSpritePath(string sceneName)
    {
        return $"{SceneArtFolder}/{sceneName}.png";
    }

    public static string CropSpritePath(string sceneName, string id)
    {
        return $"{LearningArtFolder}/{sceneName}/{id}.png";
    }

    /// <summary>
    /// Tengah-bawah kotak potong dalam koordinat ternormalisasi (asal kiri-atas).
    /// Dipakai untuk menaruh titik interaksi tepat di dasar benda pada latar.
    /// </summary>
    public static bool TryGetCropBaseCenter(string sceneName, string id, out Vector2 normalized)
    {
        for (int i = 0; i < Crops.Length; i++)
        {
            CropSpec spec = Crops[i];
            if (spec.Scene != sceneName || spec.Id != id)
            {
                continue;
            }

            normalized = new Vector2(
                spec.Normalized.x + spec.Normalized.width * 0.5f,
                spec.Normalized.y + spec.Normalized.height);
            return true;
        }

        normalized = Vector2.zero;
        return false;
    }

    /// <summary>Posisi dunia untuk titik interaksi objek belajar.</summary>
    public static Vector2 CropWorldPosition(string sceneName, string id, float mapWidth,
        float mapHeight)
    {
        if (!TryGetCropBaseCenter(sceneName, id, out Vector2 normalized))
        {
            Debug.LogWarning($"Tidak ada kotak seni untuk {sceneName}/{id}; memakai tengah peta.");
            return new Vector2(mapWidth * 0.5f, mapHeight * 0.5f);
        }

        return new Vector2(mapWidth * normalized.x, mapHeight * (1f - normalized.y));
    }

    private static void ConvertScene(string sceneName)
    {
        string source = $"{SourceFolder}/{sceneName}.jpeg";
        if (!File.Exists(source))
        {
            Debug.LogError($"Sumber tidak ditemukan: {source}");
            return;
        }

        string destination = SceneSpritePath(sceneName);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(File.ReadAllBytes(source));

        File.WriteAllBytes(destination, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
        ConfigureImporter(destination, true, 4096);
    }

    private static void ImportCrops()
    {
        for (int i = 0; i < Crops.Length; i++)
        {
            CropSpec spec = Crops[i];
            string source = $"{SourceFolder}/{spec.Scene}.jpeg";
            if (!File.Exists(source))
            {
                Debug.LogWarning($"Lewati crop {spec.Id}: {source} tidak ada.");
                continue;
            }

            Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            sourceTexture.LoadImage(File.ReadAllBytes(source));

            int x = Mathf.Clamp(Mathf.RoundToInt(spec.Normalized.x * sourceTexture.width), 0,
                sourceTexture.width - 1);
            int width = Mathf.Clamp(Mathf.RoundToInt(spec.Normalized.width * sourceTexture.width), 1,
                sourceTexture.width - x);
            int top = Mathf.Clamp(Mathf.RoundToInt(spec.Normalized.y * sourceTexture.height), 0,
                sourceTexture.height - 1);
            int height = Mathf.Clamp(Mathf.RoundToInt(spec.Normalized.height * sourceTexture.height), 1,
                sourceTexture.height - top);
            int y = sourceTexture.height - top - height;

            Color[] pixels = sourceTexture.GetPixels(x, y, width, height);
            Texture2D crop = new Texture2D(width, height, TextureFormat.RGBA32, false);
            crop.SetPixels(pixels);
            crop.Apply();

            string destination = CropSpritePath(spec.Scene, spec.Id);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? LearningArtFolder);
            File.WriteAllBytes(destination, crop.EncodeToPNG());

            Object.DestroyImmediate(sourceTexture);
            Object.DestroyImmediate(crop);

            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(destination, false, 512);
        }

        AssetDatabase.Refresh();
    }

    private static void ConfigureImporter(string path, bool isScene, int maxSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"Importer tidak ditemukan untuk {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.maxTextureSize = maxSize;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = !isScene;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.npotScale = TextureImporterNPOTScale.None;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);

        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
        {
            name = "Android",
            overridden = true,
            maxTextureSize = maxSize,
            format = TextureImporterFormat.ASTC_6x6,
            textureCompression = TextureImporterCompression.CompressedHQ,
            compressionQuality = 50
        });

        importer.SaveAndReimport();
    }
}
