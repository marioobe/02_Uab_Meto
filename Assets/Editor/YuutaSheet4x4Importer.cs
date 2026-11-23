using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Memotong spritesheet karakter 4x4 (16 sel) menjadi frame PNG transparan.
///
/// Spritesheet baru (karakter_baru_4x4.png) memiliki susunan baris:
///   1. jalan menghadap bawah (depan)
///   2. jalan menghadap kiri (profil)
///   3. jalan menghadap kanan (profil)
///   4. jalan membelakangi kamera
///
/// Runtime hanya memakai satu baris profil lalu mencerminkannya (flipX) saat
/// pemain bergerak ke kiri, jadi baris 3 yang dipakai dan baris 2 dilewati.
///
/// Tiap frame dipotong mengikuti tepi alpha, dibersihkan dari sisa putih samar,
/// lalu dipusatkan dan disejajarkan kakinya agar animasi tidak bergoyang.
/// </summary>
public static class YuutaSheet4x4Importer
{
    public const string SourcePath =
        "Assets/foto/aset_transparan_23/aset_karakter_transparan/baru/karakter_baru_4x4.png";
    public const string OutputFolder = "Assets/Art/Characters/Yuuta2";

    // Tinggi isi karakter baru ~273 px. PPU 180 -> ~1,52 unit, setara Yuuta lama
    // (tinggi frame 473 px / PPU 300 = 1,58 unit).
    public const float PixelsPerUnit = 180f;

    private const int Columns = 4;
    private const int Rows = 4;
    private const int SidePadding = 4;
    private const int TopPadding = 4;
    private const byte AlphaThreshold = 8;
    private const byte FringeAlphaMax = 160;
    private const byte FringeChannelMin = 232;

    private struct SheetRow
    {
        public readonly int Index;
        public readonly string Name;

        public SheetRow(int index, string name)
        {
            Index = index;
            Name = name;
        }
    }

    private struct RectI
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public bool IsEmpty => Width <= 0 || Height <= 0;
    }

    // Hanya baris yang benar-benar dipakai runtime.
    private static readonly SheetRow[] NeededRows =
    {
        new SheetRow(0, "walk_down"),
        new SheetRow(2, "walk_side"),
        new SheetRow(3, "walk_up")
    };

    [MenuItem("Tools/UAB METO/0b. Import Karakter Baru (4x4)", priority = 1)]
    public static void Import()
    {
        if (!File.Exists(SourcePath))
        {
            Debug.LogError($"YuutaSheet4x4Importer: sumber tidak ditemukan {SourcePath}");
            return;
        }

        Directory.CreateDirectory(OutputFolder);

        Texture2D sheet = LoadSheet(SourcePath);
        if (sheet == null)
        {
            return;
        }

        int cellWidth = sheet.width / Columns;
        int cellHeight = sheet.height / Rows;
        Color32[] pixels = sheet.GetPixels32();

        // 1) Ukur tepi isi tiap frame (koordinat dari atas).
        RectI[,] boxes = new RectI[NeededRows.Length, Columns];
        int maxWidth = 0;
        int maxHeight = 0;

        for (int r = 0; r < NeededRows.Length; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                RectI box = Measure(pixels, sheet.width, sheet.height,
                    c * cellWidth, NeededRows[r].Index * cellHeight, cellWidth, cellHeight);
                boxes[r, c] = box;

                if (box.IsEmpty)
                {
                    continue;
                }

                maxWidth = Mathf.Max(maxWidth, box.Width);
                maxHeight = Mathf.Max(maxHeight, box.Height);
            }
        }

        if (maxWidth <= 0 || maxHeight <= 0)
        {
            UnityEngine.Object.DestroyImmediate(sheet);
            Debug.LogError("YuutaSheet4x4Importer: tidak ada piksel karakter yang terbaca.");
            return;
        }

        // 2) Kanvas seragam: lebar = frame terlebar + padding; tinggi = frame
        //    tertinggi + padding atas. Kaki selalu menempel di dasar kanvas.
        int canvasWidth = maxWidth + SidePadding * 2;
        int canvasHeight = maxHeight + TopPadding;
        if (canvasWidth % 2 != 0)
        {
            canvasWidth++;
        }

        if (canvasHeight % 2 != 0)
        {
            canvasHeight++;
        }

        // 3) Tulis 12 frame (3 arah x 4 frame).
        for (int r = 0; r < NeededRows.Length; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                RectI box = boxes[r, c];
                if (box.IsEmpty)
                {
                    Debug.LogWarning($"YuutaSheet4x4Importer: frame kosong " +
                                     $"{NeededRows[r].Name}_{c + 1}");
                    continue;
                }

                Color32[] canvas = new Color32[canvasWidth * canvasHeight];
                int dstX = (canvasWidth - box.Width) / 2;
                int dstY = canvasHeight - box.Height;

                for (int y = 0; y < box.Height; y++)
                {
                    int srcRow = (sheet.height - 1 - (box.Y + y)) * sheet.width;
                    int dstRow = (dstY + y) * canvasWidth + dstX;
                    for (int x = 0; x < box.Width; x++)
                    {
                        Color32 color = pixels[srcRow + box.X + x];
                        canvas[dstRow + x] = Defringe(color);
                    }
                }

                string path = $"{OutputFolder}/yuuta2_{NeededRows[r].Name}_{c + 1}.png";
                WritePng(path, canvas, canvasWidth, canvasHeight);
                ConfigureSprite(path);
            }
        }

        // 4) Bersihkan frame lama yang tidak dipakai lagi (idle_down).
        for (int i = 1; i <= Columns; i++)
        {
            string stale = $"{OutputFolder}/yuuta2_idle_down_{i}.png";
            if (File.Exists(stale))
            {
                AssetDatabase.DeleteAsset(stale);
            }
        }

        UnityEngine.Object.DestroyImmediate(sheet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"YuutaSheet4x4Importer: {NeededRows.Length * Columns} frame dibuat " +
                  $"di {OutputFolder} ({canvasWidth}x{canvasHeight}, " +
                  $"isi maks {maxWidth}x{maxHeight}, PPU {PixelsPerUnit:0}). " +
                  "Baris profil kiri dilewati (runtime memakai flipX).");
    }

    private static Texture2D LoadSheet(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!sheet.LoadImage(bytes))
        {
            UnityEngine.Object.DestroyImmediate(sheet);
            Debug.LogError($"YuutaSheet4x4Importer: gagal membaca {path}");
            return null;
        }

        return sheet;
    }

    /// <summary>
    /// Cari kotak terkecil yang memuat piksel alpha &gt; 8 pada satu sel.
    /// Koordinat memakai acuan kiri-atas (bukan acuan tekstur Unity).
    /// </summary>
    private static RectI Measure(Color32[] pixels, int textureWidth, int textureHeight,
        int cellX, int cellTop, int cellWidth, int cellHeight)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < cellHeight; y++)
        {
            int rowStart = (textureHeight - 1 - (cellTop + y)) * textureWidth;
            for (int x = 0; x < cellWidth; x++)
            {
                if (pixels[rowStart + cellX + x].a <= AlphaThreshold)
                {
                    continue;
                }

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
        {
            return new RectI();
        }

        return new RectI
        {
            X = cellX + minX,
            Y = cellTop + minY,
            Width = maxX - minX + 1,
            Height = maxY - minY + 1
        };
    }

    /// <summary>
    /// Buang sisa putih samar di tepi alpha (halo anti-alias) tanpa menyentuh
    /// putih yang memang bagian dari gambar (mata/hiasan) karena piksel opaque
    /// tidak diubah.
    /// </summary>
    private static Color32 Defringe(Color32 color)
    {
        if (color.a == 0)
        {
            return color;
        }

        if (color.a <= FringeAlphaMax &&
            color.r >= FringeChannelMin &&
            color.g >= FringeChannelMin &&
            color.b >= FringeChannelMin)
        {
            color.a = 0;
        }

        return color;
    }

    private static void WritePng(string path, Color32[] pixelsTopDown, int width, int height)
    {
        // Susunan Color32 dari atas dibalik karena Texture2D memakai acuan kiri-bawah.
        Color32[] bottomUp = new Color32[pixelsTopDown.Length];
        for (int y = 0; y < height; y++)
        {
            Array.Copy(pixelsTopDown, y * width, bottomUp, (height - 1 - y) * width, width);
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(bottomUp);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    private static void ConfigureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
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
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
