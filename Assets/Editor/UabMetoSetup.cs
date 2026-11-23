using UnityEditor;
using UnityEngine;

/// <summary>
/// Satu perintah untuk seluruh pipeline aset dan scene.
/// Batchmode: Unity.exe -batchmode -quit -projectPath . -executeMethod UabMetoSetup.BuildEverything
/// </summary>
public static class UabMetoSetup
{
    [MenuItem("UAB Meto/0 - Build Everything", priority = 0)]
    public static void BuildEverything()
    {
        Debug.Log("UAB METO SETUP: mulai impor latar...");
        GameArtImporter.ImportAll();

        Debug.Log("UAB METO SETUP: impor aset transparan 23...");
        AsetTransparan23Importer.ImportAll();

        Debug.Log("UAB METO SETUP: impor spritesheet karakter 4x4...");
        YuutaSheet4x4Importer.Import();

        Debug.Log("UAB METO SETUP: membuat data belajar...");
        LearningDataBuilder.BuildAll();

        Debug.Log("UAB METO SETUP: membangun scene...");
        GameSceneBuilder.BuildAll();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("UAB METO SETUP: selesai.");
    }

    [MenuItem("UAB Meto/9 - Verify Art", priority = 90)]
    public static void VerifyArt()
    {
        string[] scenes = GameArtImporter.SceneNames;
        for (int i = 0; i < scenes.Length; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                GameArtImporter.SceneSpritePath(scenes[i]));
            if (sprite == null)
            {
                Debug.LogError($"VERIFY {scenes[i]}: sprite latar TIDAK ADA.");
                continue;
            }

            Debug.Log($"VERIFY {scenes[i]}: rect={sprite.rect.size} world={sprite.bounds.size} " +
                      $"ppu={sprite.pixelsPerUnit} pivot={sprite.pivot}");
        }

        LearningDatabase database = AssetDatabase.LoadAssetAtPath<LearningDatabase>(
            "Assets/Data/Learning/LearningDatabase.asset");
        if (database == null)
        {
            Debug.LogError("VERIFY: database tidak ada.");
            return;
        }

        int missingImages = 0;
        for (int i = 0; i < database.objects.Count; i++)
        {
            if (database.objects[i] == null || database.objects[i].image == null)
            {
                missingImages++;
            }
        }

        Debug.Log($"VERIFY database: {database.objects.Count} objek, {missingImages} tanpa gambar. " +
                  $"Build scenes: {EditorBuildSettings.scenes.Length}");
    }
}
