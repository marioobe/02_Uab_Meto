using UnityEditor;
using UnityEngine;

/// <summary>
/// Central place for the texture import settings used by the village map.
/// Unity 6 keeps import settings inside Library, so the .meta files cannot carry
/// them; running this code is the only way to set them from the command line or
/// from another editor script.
/// </summary>
public static class MapImporter
{
    public const float PixelsPerUnit = 100f;

    /// <summary>
    /// Configures a PNG as a single sprite. Pass a pivot (0..1) to override the
    /// default center pivot, for example (0.5, 0) for a character standing on its feet.
    /// </summary>
    public static void ConfigureSprite(string assetPath, Vector2? pivot = null)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"MapImporter: tidak ada TextureImporter di {assetPath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;

        if (pivot.HasValue)
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot.Value;
        }
        else
        {
            settings.spriteAlignment = (int)SpriteAlignment.Center;
        }

        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
