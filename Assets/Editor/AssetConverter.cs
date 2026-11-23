using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Converts the two downloaded JPEG references into game-ready PNG sprites:
///  - Yuuta spritesheet -> 16 transparent frames (4 directions / poses)
///  - UI pack           -> beige dialogue panel (9-slice)
/// JPEG has no alpha and a white background, so the white is flood-filled from
/// the borders (this keeps white details inside the character, e.g. shoes).
/// Menu: Tools > UAB METO > 7. Convert Character &amp; UI Assets
/// </summary>
public static class AssetConverter
{
    private const string CharacterSheetPath =
        "Assets/foto/Yuuta Mito Project Sekai OC Spritesheet.jpeg";

    // The pack filename contains Cyrillic characters, so it is resolved by search
    // instead of being hardcoded (safer across encodings and renames).
    private const string UiSheetFolder = "Assets/foto";
    private const string UiSheetPrefix = "Asset for the game";

    private const string CharacterFolder = "Assets/Art/Characters/Yuuta";
    private const string UiFolder = "Assets/Art/UI";
    private const string HudFolder = "Assets/Art/UI/Hud";

    private const int CellSize = 184;
    private const int Columns = 4;
    private const int Rows = 4;

    // Source rect of the beige rounded panel, in top-left pixel coordinates.
    // It is the clean panel above the slot panel; the drop shadow to the bottom
    // right is left out so the 9-slice stays symmetric.
    private static readonly RectInt PanelRect = new RectInt(1, 274, 84, 32);

    [MenuItem("Tools/UAB METO/7. Convert Character & UI Assets", priority = 7)]
    public static void ConvertAll()
    {
        ConvertCharacterSheet();
        ConvertUiPack();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Konversi aset selesai (karakter + UI).");
    }

    [MenuItem("Tools/UAB METO/8. Convert Character Sheet Only", priority = 8)]
    public static void ConvertCharacterSheet()
    {
        EnsureFolder(CharacterFolder);

        Texture2D source = LoadReadable(CharacterSheetPath);
        if (source == null)
        {
            return;
        }

        Color32[] pixels = source.GetPixels32();
        int width = source.width;
        int height = source.height;

        if (width < CellSize * Columns || height < CellSize * Rows)
        {
            Debug.LogError($"Spritesheet terlalu kecil: {width}x{height}, butuh minimal {CellSize * Columns}x{CellSize * Rows}");
            return;
        }

        bool[] background = BuildBackgroundMask(pixels, width, height, 34);

        string[] rowNames = { "idle_down", "walk_down", "walk_side", "walk_up" };

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                Color32[] cell = ExtractCell(pixels, background, width, height, column, row);
                string path = $"{CharacterFolder}/yuuta_{rowNames[row]}_{column + 1}.png";
                WritePng(path, cell, CellSize, CellSize);
            }
        }

        AssetDatabase.Refresh();

        foreach (string rowName in rowNames)
        {
            for (int i = 1; i <= Columns; i++)
            {
                ConfigureCharacterSprite($"{CharacterFolder}/yuuta_{rowName}_{i}.png");
            }
        }

        Debug.Log($"Karakter: {Rows * Columns} frame PNG dibuat di {CharacterFolder} (PPU 280, pivot bawah).");
    }

    [MenuItem("Tools/UAB METO/9. Convert UI Pack Only", priority = 9)]
    public static void ConvertUiPack()
    {
        EnsureFolder(UiFolder);

        string uiSheetPath = ResolveUiSheetPath();
        if (uiSheetPath == null)
        {
            return;
        }

        Texture2D source = LoadReadable(uiSheetPath);
        if (source == null)
        {
            return;
        }

        Color32[] pixels = source.GetPixels32();
        int width = source.width;
        int height = source.height;
        Debug.Log($"UI pack: {uiSheetPath} ({width}x{height})");
        bool[] background = BuildBackgroundMask(pixels, width, height, 30);

        Color32[] panel = ExtractRect(pixels, background, width, height, PanelRect);
        panel = TrimTransparent(panel, PanelRect.width, PanelRect.height, out int panelWidth, out int panelHeight);
        panel = FlattenPanel(panel, panelWidth, panelHeight, 8);

        if (panel == null)
        {
            Debug.LogError("Panel UI gagal dipotong.");
            return;
        }

        string panelPath = $"{UiFolder}/UI_Panel_Beige.png";
        WritePng(panelPath, panel, panelWidth, panelHeight);
        AssetDatabase.Refresh();
        ConfigurePanelSprite(panelPath);

        Debug.Log($"UI: panel {panelWidth}x{panelHeight} px dibuat di {panelPath}");

        ConvertHudIcons(pixels, background, width, height);
    }

    /// <summary>Kotak sumber ikon HUD di pak aset (koordinat kiri-atas).</summary>
    private static readonly (string Name, RectInt Rect)[] HudIconRects =
    {
        ("Heart_Full", new RectInt(13, 130, 11, 10)),
        ("Heart_Empty", new RectInt(37, 130, 11, 10)),
        ("Food_Apple", new RectInt(3, 100, 13, 10)),
        ("Food_Bread", new RectInt(19, 98, 14, 11)),
        ("Food_Banana", new RectInt(82, 97, 14, 11)),
        ("Food_Grapes", new RectInt(3, 114, 12, 10)),
        ("Food_Milk", new RectInt(69, 114, 8, 13)),
    };

    private static void ConvertHudIcons(Color32[] pixels, bool[] background, int width, int height)
    {
        EnsureFolder(HudFolder);

        foreach ((string name, RectInt rect) in HudIconRects)
        {
            Color32[] icon = ExtractRect(pixels, background, width, height, rect);
            if (icon == null)
            {
                continue;
            }

            SoftenHalo(icon);
            ClearOuterWhite(icon, rect.width, rect.height);
            string path = $"{HudFolder}/{name}.png";
            WritePng(path, icon, rect.width, rect.height);
            ConfigureHudSprite(path);
        }

        AssetDatabase.Refresh();
        Debug.Log($"HUD: {HudIconRects.Length} ikon dibuat di {HudFolder} (PPU 100, Point).");
    }

    /// <summary>Membuang latar putih yang menyambung ke tepi ikon (halo JPEG).</summary>
    private static void ClearOuterWhite(Color32[] pixels, int width, int height)
    {
        bool[] background = new bool[pixels.Length];
        Queue<int> queue = new Queue<int>();

        for (int x = 0; x < width; x++)
        {
            MarkIfWhite(pixels, background, queue, x);
            MarkIfWhite(pixels, background, queue, (height - 1) * width + x);
        }

        for (int y = 0; y < height; y++)
        {
            MarkIfWhite(pixels, background, queue, y * width);
            MarkIfWhite(pixels, background, queue, y * width + width - 1);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;

            if (x > 0)
            {
                MarkIfWhite(pixels, background, queue, index - 1);
            }

            if (x < width - 1)
            {
                MarkIfWhite(pixels, background, queue, index + 1);
            }

            if (y > 0)
            {
                MarkIfWhite(pixels, background, queue, index - width);
            }

            if (y < height - 1)
            {
                MarkIfWhite(pixels, background, queue, index + width);
            }
        }

        for (int i = 0; i < pixels.Length; i++)
        {
            if (background[i])
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }
        }
    }

    private static void MarkIfWhite(Color32[] pixels, bool[] background, Queue<int> queue, int index)
    {
        if (index < 0 || index >= pixels.Length || background[index])
        {
            return;
        }

        Color32 pixel = pixels[index];
        int minimum = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));

        if (pixel.a == 0 || minimum < 200)
        {
            return;
        }

        background[index] = true;
        queue.Enqueue(index);
    }

    /// <summary>Menghilangkan sisa halo putih JPEG di tepi ikon HUD.</summary>
    private static void SoftenHalo(Color32[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            if (pixel.a == 0)
            {
                continue;
            }

            int minimum = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
            if (minimum <= 205)
            {
                continue;
            }

            byte alpha = (byte)Mathf.Clamp((255 - minimum) * 255 / 50, 0, 255);
            pixels[i] = new Color32(pixel.r, pixel.g, pixel.b, alpha);
        }
    }

    private static void ConfigureHudSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 256;
        importer.isReadable = false;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteBorder = Vector4.zero;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // ---------------------------------------------------------------- konfigurasi importer

    private static void ConfigureCharacterSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Tidak ada TextureImporter di {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        // 180 px tinggi karakter / 280 PPU = 0.64 unit dunia (~64 px di peta, sama dengan penduduk).
        importer.spritePixelsPerUnit = 280f;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
        importer.isReadable = false;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        settings.spriteBorder = Vector4.zero;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void ConfigurePanelSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Tidak ada TextureImporter di {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 512;
        importer.isReadable = false;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        // Border 9-slice: sudut bevel dipertahankan, tengah direntangkan.
        settings.spriteBorder = new Vector4(8f, 8f, 8f, 8f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // ---------------------------------------------------------------- baca sumber

    private static string ResolveUiSheetPath()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiSheetFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileName(path);
            if (name.StartsWith(UiSheetPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        Debug.LogError($"Gambar UI pack tidak ditemukan di {UiSheetFolder} (prefix '{UiSheetPrefix}').");
        return null;
    }

    private static Texture2D LoadReadable(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Sumber tidak ditemukan: {path}");
            return null;
        }

        // Always reimport: the Library cache can keep an older, downsampled copy
        // even after maxTextureSize is raised, which breaks the 184 px cell grid.
        // npotScale must be None: ToNearest would shrink 736 px to 512 px.
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null || !texture.isReadable)
        {
            Debug.LogError($"Gagal membaca tekstur: {path} (isReadable={texture?.isReadable})");
            return null;
        }

        Debug.Log($"Sumber {Path.GetFileName(path)}: {texture.width}x{texture.height}, maxTextureSize={importer.maxTextureSize}, readable={texture.isReadable}");

        return texture;
    }

    /// <summary>Marks every pixel connected to the image border that is white-ish.</summary>
    private static bool[] BuildBackgroundMask(Color32[] pixels, int width, int height, int tolerance)
    {
        bool[] mask = new bool[pixels.Length];
        Queue<int> queue = new Queue<int>(width * 2 + height * 2);

        int threshold = 255 - tolerance;

        bool IsWhite(int index)
        {
            Color32 c = pixels[index];
            return c.r > threshold && c.g > threshold && c.b > threshold;
        }

        void Seed(int index)
        {
            if (!mask[index] && IsWhite(index))
            {
                mask[index] = true;
                queue.Enqueue(index);
            }
        }

        for (int x = 0; x < width; x++)
        {
            Seed(x);
            Seed((height - 1) * width + x);
        }

        for (int y = 0; y < height; y++)
        {
            Seed(y * width);
            Seed(y * width + width - 1);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;

            if (x > 0) Seed(index - 1);
            if (x < width - 1) Seed(index + 1);
            if (y > 0) Seed(index - width);
            if (y < height - 1) Seed(index + width);
        }

        return mask;
    }

    private static Color32[] ExtractCell(Color32[] pixels, bool[] background,
        int width, int height, int column, int row)
    {
        Color32[] cell = new Color32[CellSize * CellSize];

        // Unity textures start at the bottom-left; the sheet rows are counted from the top.
        int sourceTop = row * CellSize;
        int sourceLeft = column * CellSize;

        for (int y = 0; y < CellSize; y++)
        {
            int textureY = height - (sourceTop + CellSize) + y;
            for (int x = 0; x < CellSize; x++)
            {
                int index = textureY * width + sourceLeft + x;
                cell[y * CellSize + x] = ApplyAlpha(pixels[index], background[index]);
            }
        }

        return cell;
    }

    private static Color32[] ExtractRect(Color32[] pixels, bool[] background,
        int width, int height, RectInt rect)
    {
        Color32[] result = new Color32[rect.width * rect.height];

        for (int y = 0; y < rect.height; y++)
        {
            int textureY = height - (rect.y + rect.height) + y;
            for (int x = 0; x < rect.width; x++)
            {
                int index = textureY * width + rect.x + x;
                result[y * rect.width + x] = ApplyAlpha(pixels[index], background[index]);
            }
        }

        return result;
    }

    private static Color32 ApplyAlpha(Color32 pixel, bool isBackground)
    {
        if (isBackground)
        {
            return new Color32(0, 0, 0, 0);
        }

        // JPEG leaves a light halo around the subject; make near-white edge pixels
        // semi transparent so the sprite does not show a white outline on the map.
        int minimum = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
        if (minimum > 226)
        {
            byte alpha = (byte)Mathf.Clamp((255 - minimum) * 255 / 29, 0, 255);
            return new Color32(pixel.r, pixel.g, pixel.b, alpha);
        }

        return new Color32(pixel.r, pixel.g, pixel.b, 255);
    }

    /// Panel 84x32 px dipakai sebagai 9-slice; bagian tengahnya direntangkan. Noise JPEG
    /// di tengah panel ikut terentang jadi blok besar, jadi tengah diratakan dan bingkai
    /// dibuat seragam per baris/kolom. Semua pixel panel juga dibuat opak karena halo
    /// JPEG membuat bevel atas semi transparan (terlihat pudar di HP).
    private static Color32[] FlattenPanel(Color32[] pixels, int width, int height, int border)
    {
        if (pixels == null || width <= border * 2 || height <= border * 2)
        {
            return pixels;
        }

        long red = 0, green = 0, blue = 0;
        int count = 0;

        for (int y = border; y < height - border; y++)
        {
            for (int x = border; x < width - border; x++)
            {
                Color32 sample = pixels[y * width + x];
                if (sample.a < 200)
                {
                    continue;
                }

                red += sample.r;
                green += sample.g;
                blue += sample.b;
                count++;
            }
        }

        if (count == 0)
        {
            return pixels;
        }

        Color32 flat = new Color32((byte)(red / count), (byte)(green / count), (byte)(blue / count), 255);
        Color32[] result = new Color32[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            bool inHorizontalBand = y < border || y >= height - border;

            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (pixels[index].a == 0)
                {
                    continue;
                }

                bool inVerticalBand = x < border || x >= width - border;

                if (inHorizontalBand && inVerticalBand)
                {
                    Color32 corner = pixels[index];
                    result[index] = new Color32(corner.r, corner.g, corner.b, 255);
                }
                else if (inHorizontalBand)
                {
                    // Profil bevel diambil dari kolom tengah supaya bingkai rata horizontal.
                    Color32 edge = pixels[y * width + width / 2];
                    result[index] = new Color32(edge.r, edge.g, edge.b, 255);
                }
                else if (inVerticalBand)
                {
                    Color32 edge = pixels[(height / 2) * width + x];
                    result[index] = new Color32(edge.r, edge.g, edge.b, 255);
                }
                else
                {
                    result[index] = flat;
                }
            }
        }

        return result;
    }

    private static Color32[] TrimTransparent(Color32[] pixels, int width, int height,
        out int trimmedWidth, out int trimmedHeight)
    {
        int minX = width, minY = height, maxX = -1, maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a == 0)
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
            trimmedWidth = 0;
            trimmedHeight = 0;
            return null;
        }

        trimmedWidth = maxX - minX + 1;
        trimmedHeight = maxY - minY + 1;
        Color32[] result = new Color32[trimmedWidth * trimmedHeight];

        for (int y = 0; y < trimmedHeight; y++)
        {
            for (int x = 0; x < trimmedWidth; x++)
            {
                result[y * trimmedWidth + x] = pixels[(minY + y) * width + minX + x];
            }
        }

        return result;
    }

    // ---------------------------------------------------------------- util

    private static void WritePng(string path, Color32[] pixels, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets");
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
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
}
