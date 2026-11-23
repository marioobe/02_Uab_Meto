#if UNITY_ANDROID
using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Menyuntikkan izin RECORD_AUDIO ke manifest Gradle hasil build (Fase C).
///
/// Tidak membuat/mengganti manifest utama Unity. Hanya menambah satu
/// <c>uses-permission</c> ke <c>unityLibrary/src/main/AndroidManifest.xml</c>,
/// idempotent, dan mempertahankan seluruh elemen yang sudah ada.
/// </summary>
public class AndroidPermissionInjector : IPostGenerateGradleAndroidProject
{
    private const string Permission = "android.permission.RECORD_AUDIO";
    private const string PermissionTag = "<uses-permission android:name=\"android.permission.RECORD_AUDIO\"";

    public int callbackOrder
    {
        get { return 0; }
    }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath))
        {
            throw new BuildFailedException(
                $"[AndroidPermissionInjector] Manifest tidak ditemukan: {manifestPath}");
        }

        string xml;
        try
        {
            xml = File.ReadAllText(manifestPath);
        }
        catch (Exception error)
        {
            throw new BuildFailedException(
                $"[AndroidPermissionInjector] Gagal membaca manifest: {error.Message}");
        }

        if (xml.IndexOf(Permission, StringComparison.Ordinal) >= 0)
        {
            Debug.Log("[AndroidPermissionInjector] RECORD_AUDIO sudah ada (tidak ditambahkan lagi).");
            Validate(xml, manifestPath);
            return;
        }

        int closing = xml.LastIndexOf("</manifest>", StringComparison.Ordinal);
        if (closing < 0)
        {
            throw new BuildFailedException(
                $"[AndroidPermissionInjector] Manifest tidak memiliki </manifest>: {manifestPath}");
        }

        string insertion = "    " + PermissionTag + " />\n";
        string updated = xml.Insert(closing, insertion);

        Validate(updated, manifestPath);

        try
        {
            File.WriteAllText(manifestPath, updated, new UTF8Encoding(false));
        }
        catch (Exception error)
        {
            throw new BuildFailedException(
                $"[AndroidPermissionInjector] Gagal menulis manifest: {error.Message}");
        }

        Debug.Log($"[AndroidPermissionInjector] RECORD_AUDIO ditambahkan ke {manifestPath}");
    }

    private static void Validate(string xml, string manifestPath)
    {
        try
        {
            XDocument.Parse(xml);
        }
        catch (Exception error)
        {
            throw new BuildFailedException(
                $"[AndroidPermissionInjector] XML manifest tidak valid ({manifestPath}): {error.Message}");
        }
    }
}
#endif
