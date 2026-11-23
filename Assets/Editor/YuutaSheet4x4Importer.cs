using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Memotong spritesheet karakter 4x4 (16 sel) baru menjadi 16 frame PNG transparan.
/// Baris (atas ke bawah): idle_down, walk_down, walk_side, walk_up — sama seperti
/// spritesheet Yuuta lama. Sumber: aset_karakter_transparan/baru/yuuta_spritesheet_4x4.png.
/// </summary>
public static class YuutaSheet4x4Importer
{
    public const string SourcePath =
        "Assets/foto/aset_transparan_23/aset_karakter_transparan/baru/yuuta_spritesheet_4x4.png";
    public const string OutputFolder = "Assets/Art/Characters/Yuuta2";

    private static readonly string[] RowNames = { "idle_down", "walk_down", "walk_side", "walk_up" };

    public static void Import()
    {
        if (!File.Exists(SourcePath))
        {
            Debug.LogWarning($"YuutaSheet4x4Importer: sumber tidak ditemukan {SourcePath}");
            return;
        }

        Directory.CreateDirectory(OutputFolder);

        byte[] bytes = File.ReadAllBytes(SourcePath);
        Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!sheet.LoadImage(bytes))
        {
            Object.DestroyImmediate(sheet);
            Debug.LogError("YuutaSheet4x4Importer: gagal membaca spritesheet.");
            return;
        }

        int cols = 4;
        int rows = 4;
        int cellWidth = sheet.width / cols;
        int cellHeight = sheet.height / rows;
        Color32[] pixels = sheet.GetPixels32();
        int srcWidth = sheet.width;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // Baris 0 (idle_down) berada di atas tekstur.
                int srcX = c * cellWidth;
                int srcTop = r * cellHeight;
                Color32[] cell = new Color32[cellWidth * cellHeight];
                for (int y = 0; y < cellHeight; y++)
                {
                    int textureY = (sheet.height - srcTop - cellHeight) + y;
                    for (int x = 0; x < cellWidth; x++)
                    {
                        cell[y * cellWidth + x] = pixels[textureY * srcWidth + srcX + x];
                    }
                }

                string path = $"{OutputFolder}/yuuta2_{RowNames[r]}_{c + 1}.png";
                WritePng(path, cell, cellWidth, cellHeight);
                ConfigureSprite(path);
            }
        }

        Object.DestroyImmediate(sheet);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"YuutaSheet4x4Importer: {rows * cols} frame dibuat di {OutputFolder} " +
                  $"({cellWidth}x{cellHeight}).");
    }

    private static void WritePng(string path, Color32[] pixels, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
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
        importer.spritePixelsPerUnit = 300f;
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
