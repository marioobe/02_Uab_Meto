using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Mengimpor paket aset transparan 23 (Assets/foto/aset_transparan_23) menjadi
/// sprite siap pakai, TANPA mengubah file sumber.
///
///  - Latar        -> Assets/Art/Scenes2/{Base_Map,Dapur,Rumah,Kebun}.png
///  - Objek belajar -> Assets/Art/Learning2/<scene>/<id>.png  (dipotong tepi
///                    transparan, PPU dihitung agar tinggi konten = target dunia)
///  - Karakter     -> Assets/Art/Characters/Anak/*.png + ketua_adat.png
///  - UI           -> Assets/Art/UI2/*.png  (hati, panah D-Pad, tombol Atur,
///                    panel dialog ketua adat, dan hati-kosong hasil turunan)
///
/// Pivot objek/karakter di dasar (0.5, 0) supaya "berdiri" di titik interaksi.
/// </summary>
public static class AsetTransparan23Importer
{
    public const string SourceRoot = "Assets/foto/aset_transparan_23";
    public const string SceneArtFolder = "Assets/Art/Scenes2";
    public const string LearningArtFolder = "Assets/Art/Learning2";
    public const string CharacterFolder = "Assets/Art/Characters/Anak";
    public const string UiFolder = "Assets/Art/UI2";

    // ---------------------------------------------------------------- latar

    private struct BackgroundSpec
    {
        public string Scene;
        public string Source;
        public float Ppu;

        public BackgroundSpec(string scene, string source, float ppu)
        {
            Scene = scene;
            Source = source;
            Ppu = ppu;
        }
    }

    private static readonly BackgroundSpec[] Backgrounds =
    {
        new BackgroundSpec("Base_Map", "base map kampung dawan.jpeg", 100f),
        new BackgroundSpec("Dapur", "rumah adat ume kbubu.jpeg", 50f),
        new BackgroundSpec("Rumah", "rumah warga.jpeg", 50f),
        new BackgroundSpec("Kebun", "kebun.jpeg", 50f),
    };

    // ---------------------------------------------------------------- objek

    private struct ObjectSpec
    {
        public string Scene;
        public string Id;
        public string Source;
        public float TargetHeight;

        public ObjectSpec(string scene, string id, string source, float targetHeight)
        {
            Scene = scene;
            Id = id;
            Source = source;
            TargetHeight = targetHeight;
        }
    }

    // TargetHeight = tinggi konten dalam unit dunia (peta 27.52 x 15.36).
    private static readonly ObjectSpec[] Objects =
    {
        // Dapur Bulat. Tinggi disetel agar proporsional terhadap pemain (~1,58 unit).
        new ObjectSpec("Dapur", "periuk", "aset dalam rumah bulat/06.png", 1.00f),
        new ObjectSpec("Dapur", "tungku", "aset dalam rumah bulat/02.png", 0.90f),
        new ObjectSpec("Dapur", "kayu_bakar", "aset dalam rumah bulat/03.png", 0.48f),
        new ObjectSpec("Dapur", "jagung", "aset dalam rumah bulat/04.png", 0.62f),
        new ObjectSpec("Dapur", "tikar", "aset dalam rumah bulat/05.png", 0.55f),
        new ObjectSpec("Dapur", "tempayan", "aset dalam rumah bulat/01.png", 1.00f),
        // bakul dipotong dari keranjang di 08.png (lihat CropBakul).
        new ObjectSpec("Dapur", "bakul", "aset dalam rumah bulat/08.png", 0.62f),
        new ObjectSpec("Dapur", "alat_masak", "aset dalam rumah bulat/08.png", 0.58f),
        new ObjectSpec("Dapur", "cobek", "aset dalam rumah bulat/09.png", 0.36f),
        new ObjectSpec("Dapur", "bangku", "aset dalam rumah bulat/10.png", 0.48f),

        // Rumah Warga
        new ObjectSpec("Rumah", "tempat_tidur", "aset dalam rumah/11_tempat_tidur_hala.png", 0.70f),
        new ObjectSpec("Rumah", "selimut_tenun", "aset dalam rumah/12_selimut_tenun_beti_tais.png", 0.50f),
        new ObjectSpec("Rumah", "lemari", "aset dalam rumah/13_lemari.png", 1.60f),
        new ObjectSpec("Rumah", "peti_kayu", "aset dalam rumah/14_peti_kayu_peti.png", 0.70f),
        new ObjectSpec("Rumah", "rak_piring", "aset dalam rumah/15_rak_piring_pika_bale.png", 1.40f),
        new ObjectSpec("Rumah", "meja_makan", "aset dalam rumah/16_meja_makan_mei.png", 0.85f),
        new ObjectSpec("Rumah", "tempayan_rumah", "aset dalam rumah/17_tempayan_nai_najan.png", 0.95f),
        new ObjectSpec("Rumah", "keranjang", "aset dalam rumah/18_keranjang_oko_suli.png", 0.80f),
        new ObjectSpec("Rumah", "bangku_rumah", "aset dalam rumah/19_bangku_toko.png", 0.50f),
        new ObjectSpec("Rumah", "tikar_rumah", "aset dalam rumah/tikar_rumah.png", 1.10f),

        // Kebun. Pohon dibuat lebih kecil (1,8 unit) supaya bisa berdiri sendiri di
        // sela latar lama tanpa menimpa pohon yang sudah tergambar.
        new ObjectSpec("Kebun", "pohon_jeruk", "aset dalam kebun/20_pohon_jeruk_hau_lelo.png", 1.80f),
        new ObjectSpec("Kebun", "pohon_kelapa", "aset dalam kebun/21_pohon_kelapa_hau_noah.png", 1.80f),
        new ObjectSpec("Kebun", "pohon_mangga", "aset dalam kebun/22_pohon_mangga_hau_upin.png", 1.80f),
        new ObjectSpec("Kebun", "kebun_sayur", "aset dalam kebun/23_kebun_sayur_lele_utan.png", 1.40f),
        new ObjectSpec("Kebun", "tomat", "aset dalam kebun/24_tomat_kalimai.png", 1.10f),
        new ObjectSpec("Kebun", "cabai", "aset dalam kebun/cabai.png", 1.10f),
        new ObjectSpec("Kebun", "terong", "aset dalam kebun/terong.png", 1.10f),
        new ObjectSpec("Kebun", "bakul_hasil", "aset dalam kebun/hasil_panen.png", 0.80f),
        new ObjectSpec("Kebun", "sumur", "aset dalam kebun/sumur.png", 1.50f),
        new ObjectSpec("Kebun", "alat_kebun", "aset dalam kebun/alat_kebun.png", 0.60f),
        new ObjectSpec("Kebun", "bibit", "aset dalam kebun/bibit.png", 0.70f),
        new ObjectSpec("Kebun", "daun_pisang", "aset dalam kebun/daun_pisang.png", 0.90f),
    };

    // Posisi ternormalisasi (0..1 dari kiri-atas latar lama) untuk setiap objek.
    // Dipilih manual dari latar: berdiri di lantai/jalur kosong, tidak menimpa
    // perabot atau pohon yang sudah tergambar.
    private struct Placement
    {
        public string Scene;
        public string Id;
        public float X;
        public float Y;

        public Placement(string scene, string id, float x, float y)
        {
            Scene = scene;
            Id = id;
            X = x;
            Y = y;
        }
    }

    // Koordinat ternormalisasi (asal kiri-atas) diukur langsung pada latar 2752x1536
    // memakai overlay bernomor. Sprite objek tidak lagi digambar di scene, jadi titik
    // ini harus duduk tepat di dasar benda yang SUDAH ADA di gambar latar.
    private static readonly Placement[] Placements =
    {
        new Placement("Dapur", "periuk", 0.368f, 0.510f),
        new Placement("Dapur", "tungku", 0.404f, 0.590f),
        new Placement("Dapur", "kayu_bakar", 0.515f, 0.570f),
        new Placement("Dapur", "jagung", 0.460f, 0.420f),
        new Placement("Dapur", "tikar", 0.730f, 0.370f),
        new Placement("Dapur", "tempayan", 0.280f, 0.290f),
        new Placement("Dapur", "bakul", 0.225f, 0.320f),
        new Placement("Dapur", "alat_masak", 0.350f, 0.275f),
        new Placement("Dapur", "cobek", 0.295f, 0.605f),
        new Placement("Dapur", "bangku", 0.490f, 0.720f),

        new Placement("Rumah", "tempat_tidur", 0.160f, 0.450f),
        new Placement("Rumah", "lemari", 0.340f, 0.320f),
        new Placement("Rumah", "rak_piring", 0.590f, 0.310f),
        new Placement("Rumah", "selimut_tenun", 0.140f, 0.500f),
        new Placement("Rumah", "meja_makan", 0.760f, 0.380f),
        new Placement("Rumah", "peti_kayu", 0.475f, 0.330f),
        new Placement("Rumah", "tempayan_rumah", 0.855f, 0.280f),
        new Placement("Rumah", "keranjang", 0.275f, 0.270f),
        new Placement("Rumah", "bangku_rumah", 0.740f, 0.460f),
        new Placement("Rumah", "tikar_rumah", 0.720f, 0.620f),

        new Placement("Kebun", "pohon_mangga", 0.630f, 0.150f),
        new Placement("Kebun", "pohon_jeruk", 0.120f, 0.370f),
        new Placement("Kebun", "pohon_kelapa", 0.160f, 0.180f),
        new Placement("Kebun", "tomat", 0.640f, 0.710f),
        new Placement("Kebun", "cabai", 0.640f, 0.560f),
        new Placement("Kebun", "terong", 0.400f, 0.620f),
        new Placement("Kebun", "kebun_sayur", 0.270f, 0.390f),
        new Placement("Kebun", "daun_pisang", 0.120f, 0.750f),
        new Placement("Kebun", "sumur", 0.130f, 0.510f),
        new Placement("Kebun", "bibit", 0.260f, 0.630f),
        new Placement("Kebun", "alat_kebun", 0.780f, 0.290f),
        new Placement("Kebun", "bakul_hasil", 0.300f, 0.750f),
    };

    public static bool TryGetPlacement(string sceneName, string id, out Vector2 normalized)
    {
        for (int i = 0; i < Placements.Length; i++)
        {
            Placement placement = Placements[i];
            if (placement.Scene == sceneName && placement.Id == id)
            {
                normalized = new Vector2(placement.X, placement.Y);
                return true;
            }
        }

        normalized = Vector2.zero;
        return false;
    }

    // Objek rata di lantai (diberi feetOffset negatif saat di-scene agar selalu di
    // belakang pemain). Tikar Dapur berbentuk gulungan, jadi tidak termasuk.
    private static readonly HashSet<string> FlatObjects = new HashSet<string>
    {
        "tikar_rumah", "kebun_sayur"
    };

    public static bool IsFlat(string id)
    {
        return FlatObjects.Contains(id);
    }

    // ---------------------------------------------------------------- karakter

    private struct CharacterSpec
    {
        public string Source;
        public string Output;
    }

    private static readonly CharacterSpec[] Characters =
    {
        new CharacterSpec { Source = "aset_karakter_transparan/karakter_utama_01.png", Output = "anak_01.png" },
        new CharacterSpec { Source = "aset_karakter_transparan/karakter_utama_02.png", Output = "anak_02.png" },
        new CharacterSpec { Source = "aset_karakter_transparan/karakter_utama_03.png", Output = "anak_03.png" },
        new CharacterSpec { Source = "aset_karakter_transparan/karakter_utama_04.png", Output = "anak_04.png" },
        new CharacterSpec { Source = "aset_karakter_transparan/karakter_utama_05.png", Output = "anak_05.png" },
        new CharacterSpec { Source = "aset_karakter_transparan/karakter_utama_06.png", Output = "anak_06.png" },
        new CharacterSpec { Source = "aset_karakter_transparan/karakter_utama_07.png", Output = "anak_07.png" },
        new CharacterSpec { Source = "aset_karakter_transparan/karakter_utama_08.png", Output = "anak_08.png" },
        new CharacterSpec { Source = "aset_karakter_transparan/ketua_adat.png", Output = "ketua_adat.png" },
    };

    // ---------------------------------------------------------------- UI

    private struct UiSpec
    {
        public string Source;
        public string Output;
    }

    private static readonly UiSpec[] UiSprites =
    {
        new UiSpec { Source = "aset_ui_game_transparan/nyawa_penuh_1.png", Output = "heart_full.png" },
        new UiSpec { Source = "aset_ui_game_transparan/panah_atas.png", Output = "dpad_up.png" },
        new UiSpec { Source = "aset_ui_game_transparan/panah_bawah.png", Output = "dpad_down.png" },
        new UiSpec { Source = "aset_ui_game_transparan/panah_kiri.png", Output = "dpad_left.png" },
        new UiSpec { Source = "aset_ui_game_transparan/panah_kanan.png", Output = "dpad_right.png" },
        new UiSpec { Source = "aset_ui_game_transparan/tombol_setting.png", Output = "settings.png" },
        new UiSpec { Source = "aset_ui_game_transparan/asset penjelasan toko adat.png", Output = "dialog_panel.png" },
    };

    // ---------------------------------------------------------------- API

    public static string SceneSpritePath(string sceneName)
    {
        return $"{SceneArtFolder}/{sceneName}.png";
    }

    public static string ObjectSpritePath(string sceneName, string id)
    {
        return $"{LearningArtFolder}/{sceneName}/{id}.png";
    }

    [MenuItem("UAB Meto/1c - Import Aset Transparan 23", priority = 3)]
    public static void ImportAll()
    {
        Directory.CreateDirectory(SceneArtFolder);
        Directory.CreateDirectory(LearningArtFolder);
        Directory.CreateDirectory(CharacterFolder);
        Directory.CreateDirectory(UiFolder);

        ImportBackgrounds();
        ImportObjects();
        ImportCharacters();
        ImportUi();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("AsetTransparan23Importer selesai: " +
                  $"{Backgrounds.Length} latar, {Objects.Length} objek, {Characters.Length} karakter, " +
                  $"{UiSprites.Length + 1} UI.");
    }

    // ---------------------------------------------------------------- latar

    private static void ImportBackgrounds()
    {
        for (int i = 0; i < Backgrounds.Length; i++)
        {
            BackgroundSpec spec = Backgrounds[i];
            string source = $"{SourceRoot}/{spec.Source}";
            string destination = SceneSpritePath(spec.Scene);
            if (!File.Exists(source))
            {
                Debug.LogError($"Latar tidak ditemukan: {source}");
                continue;
            }

            // JPEG -> PNG (tanpa alpha) agar Unity mengimpornya sebagai sprite.
            Texture2D texture = Load(source);
            if (texture == null)
            {
                Debug.LogError($"Latar gagal dibaca: {source}");
                continue;
            }

            EnsureFolder(SceneArtFolder);
            File.WriteAllBytes(destination, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);

            ConfigureSprite(destination, spec.Ppu, new Vector2(0.5f, 0.5f),
                FilterMode.Bilinear, false, 4096, false);
            Debug.Log($"Latar {spec.Scene}: {destination} (PPU {spec.Ppu}).");
        }
    }

    // ---------------------------------------------------------------- objek

    private static void ImportObjects()
    {
        for (int i = 0; i < Objects.Length; i++)
        {
            ObjectSpec spec = Objects[i];
            string source = $"{SourceRoot}/{spec.Source}";
            string destination = ObjectSpritePath(spec.Scene, spec.Id);
            if (!File.Exists(source))
            {
                Debug.LogError($"Sumber objek tidak ditemukan: {source}");
                continue;
            }

            bool isBakul = spec.Id == "bakul";
            byte[] raw = isBakul
                ? CropBakul(source)
                : File.ReadAllBytes(source);

            int trimmedWidth;
            int trimmedHeight;
            byte[] trimmed = TrimToAlpha(raw, out trimmedWidth, out trimmedHeight);
            if (trimmed == null)
            {
                Debug.LogWarning($"Objek {spec.Id}: tidak ada konten (alpha kosong), dilewati.");
                continue;
            }

            EnsureFolder(Path.GetDirectoryName(destination) ?? LearningArtFolder);
            File.WriteAllBytes(destination, trimmed);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);

            float ppu = Mathf.Max(0.1f, trimmedHeight / Mathf.Max(0.1f, spec.TargetHeight));
            ConfigureSprite(destination, ppu, new Vector2(0.5f, 0f),
                FilterMode.Bilinear, true, 2048, false);

            Debug.Log($"Objek {spec.Scene}/{spec.Id}: {trimmedWidth}x{trimmedHeight} " +
                      $"-> PPU {ppu:0.0} (tinggi dunia {spec.TargetHeight:0.00} u, " +
                      $"aspek {(float)trimmedWidth / Mathf.Max(1, trimmedHeight):0.00}).");
        }
    }

    /// <summary>Memotong bagian keranjang dari 08.png (separuh kiri).</summary>
    private static byte[] CropBakul(string source)
    {
        Texture2D texture = Load(source);
        if (texture == null)
        {
            return File.ReadAllBytes(source);
        }

        Color32[] all = texture.GetPixels32();
        int srcWidth = texture.width;
        int srcHeight = texture.height;
        int width = Mathf.Max(1, Mathf.RoundToInt(srcWidth * 0.55f));
        int height = srcHeight;
        Color32[] sub = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                sub[y * width + x] = all[y * srcWidth + x];
            }
        }

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.SetPixels32(sub);
        result.Apply();
        byte[] png = result.EncodeToPNG();
        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(result);
        return png;
    }

    // ---------------------------------------------------------------- karakter

    private static void ImportCharacters()
    {
        for (int i = 0; i < Characters.Length; i++)
        {
            CharacterSpec spec = Characters[i];
            string source = $"{SourceRoot}/{spec.Source}";
            string destination = $"{CharacterFolder}/{spec.Output}";
            if (!File.Exists(source))
            {
                Debug.LogError($"Karakter tidak ditemukan: {source}");
                continue;
            }

            WriteCopy(source, destination);
            // PPU 300 -> tinggi ~1.5 unit untuk karakter utama, ~3 unit untuk ketua adat.
            ConfigureSprite(destination, 300f, new Vector2(0.5f, 0f),
                FilterMode.Point, true, 1024, false);
            Debug.Log($"Karakter {spec.Output}: {destination}.");
        }
    }

    // ---------------------------------------------------------------- UI

    private static void ImportUi()
    {
        for (int i = 0; i < UiSprites.Length; i++)
        {
            UiSpec spec = UiSprites[i];
            string source = $"{SourceRoot}/{spec.Source}";
            string destination = $"{UiFolder}/{spec.Output}";
            if (!File.Exists(source))
            {
                Debug.LogError($"UI tidak ditemukan: {source}");
                continue;
            }

            WriteCopy(source, destination);
            bool pixelArt = spec.Output.StartsWith("dpad_") ||
                            spec.Output == "heart_full.png" ||
                            spec.Output == "settings.png";
            ConfigureSprite(destination, 100f, new Vector2(0.5f, 0.5f),
                pixelArt ? FilterMode.Point : FilterMode.Bilinear, true, 2048, false);
        }

        GenerateHeartEmpty();
    }

    /// <summary>Hati kosong diturunkan dari hati penuh (abu-abu gelap).</summary>
    private static void GenerateHeartEmpty()
    {
        string fullPath = $"{UiFolder}/heart_full.png";
        if (!File.Exists(fullPath))
        {
            return;
        }

        Texture2D texture = Load(fullPath);
        if (texture == null)
        {
            return;
        }

        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a == 0)
            {
                continue;
            }

            int gray = Mathf.RoundToInt(pixels[i].r * 0.35f + pixels[i].g * 0.35f + pixels[i].b * 0.3f);
            gray = Mathf.Clamp(gray, 40, 180);
            pixels[i] = new Color32((byte)gray, (byte)gray, (byte)gray, pixels[i].a);
        }

        string destination = $"{UiFolder}/heart_empty.png";
        WritePng(destination, pixels, texture.width, texture.height);
        Object.DestroyImmediate(texture);
        ConfigureSprite(destination, 100f, new Vector2(0.5f, 0.5f),
            FilterMode.Point, true, 512, false);
    }

    // ---------------------------------------------------------------- util

    private static Texture2D Load(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Object.DestroyImmediate(texture);
            return null;
        }

        return texture;
    }

    private static void WriteCopy(string source, string destination)
    {
        byte[] bytes = File.ReadAllBytes(source);
        EnsureFolder(Path.GetDirectoryName(destination) ?? "Assets");
        File.WriteAllBytes(destination, bytes);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
    }

    private static void WritePng(string path, Color32[] pixels, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        EnsureFolder(Path.GetDirectoryName(path) ?? "Assets");
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static byte[] TrimToAlpha(byte[] png, out int width, out int height)
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(png))
        {
            width = 0;
            height = 0;
            Object.DestroyImmediate(texture);
            return null;
        }

        Color32[] pixels = texture.GetPixels32();
        int srcWidth = texture.width;
        int srcHeight = texture.height;

        int minX = srcWidth, minY = srcHeight, maxX = -1, maxY = -1;
        for (int y = 0; y < srcHeight; y++)
        {
            for (int x = 0; x < srcWidth; x++)
            {
                if (pixels[y * srcWidth + x].a < 8)
                {
                    continue;
                }

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < 0)
        {
            width = 0;
            height = 0;
            Object.DestroyImmediate(texture);
            return null;
        }

        width = maxX - minX + 1;
        height = maxY - minY + 1;
        Color32[] trimmed = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                trimmed[y * width + x] = pixels[(minY + y) * srcWidth + (minX + x)];
            }
        }

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.SetPixels32(trimmed);
        result.Apply();
        byte[] output = result.EncodeToPNG();
        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(result);
        return output;
    }

    private static void ConfigureSprite(string path, float ppu, Vector2 pivot,
        FilterMode filterMode, bool alpha, int maxSize, bool sliced)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"Tidak ada TextureImporter di {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = filterMode;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = alpha;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = maxSize;
        importer.isReadable = false;
        importer.npotScale = TextureImporterNPOTScale.None;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteBorder = sliced ? new Vector4(8f, 8f, 8f, 8f) : Vector4.zero;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);

        importer.spritePixelsPerUnit = ppu;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string path)
    {
        // Pakai System.IO murni: mencampur AssetDatabase.CreateFolder dengan
        // Directory.CreateDirectory membuat folder duplikat ("X 1") saat path
        // sudah ada di disk tetapi belum di-refresh.
        if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
