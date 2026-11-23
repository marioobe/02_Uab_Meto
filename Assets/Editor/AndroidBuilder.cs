using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One-click Android APK build for device testing.
/// Menu: Tools > UAB METO > 5. Build Android APK (arm64).
/// Can also run headless:
/// Unity.exe -batchmode -quit -projectPath . -executeMethod AndroidBuilder.BuildApk
/// </summary>
public static class AndroidBuilder
{
    private const string ApkOutputPath = "Builds/Android/UabMeto.apk";
    private const string DebugApkOutputPath = "Builds/Android/UabMeto_Debug.apk";
    private const string UniversalApkOutputPath = "Builds/Android/UabMeto_Universal.apk";

    [MenuItem("Tools/UAB METO/5. Build Android APK", priority = 5)]
    public static void BuildApk()
    {
        if (!EditorSceneManagerSafeSave())
        {
            Debug.Log("Build Android dibatalkan.");
            return;
        }

        ConfigureAndroidPlayerSettings();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("Beralih ke platform Android (butuh reimport, bisa beberapa menit)...");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Gagal beralih ke platform Android.");
                EditorApplication.Exit(1);
                return;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ApkOutputPath) ?? "Builds/Android");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = SceneList(),
            locationPathName = ApkOutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
        UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

        Debug.Log($"BUILD RESULT: {summary.result} | {summary.totalSize / (1024f * 1024f):0.0} MB | errors: {summary.totalErrors} | {ApkOutputPath}");

        if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/UAB METO/5b. Build Android Debug APK", priority = 6)]
    public static void BuildDebugApk()
    {
        if (!EditorSceneManagerSafeSave())
        {
            Debug.Log("Build Android debug dibatalkan.");
            return;
        }

        ConfigureAndroidPlayerSettings();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("Beralih ke platform Android (butuh reimport, bisa beberapa menit)...");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Gagal beralih ke platform Android.");
                EditorApplication.Exit(1);
                return;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(DebugApkOutputPath) ?? "Builds/Android");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = SceneList(),
            locationPathName = DebugApkOutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
        UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

        Debug.Log($"BUILD DEBUG RESULT: {summary.result} | {summary.totalSize / (1024f * 1024f):0.0} MB | " +
                  $"errors: {summary.totalErrors} | {DebugApkOutputPath}");

        if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// Build universal (arm64 + armv7) untuk perangkat sekolah yang belum pasti.
    /// Setelah build, arsitektur dikembalikan ke arm64 agar ProjectSettings tidak
    /// berubah permanen. Bukan build debug: tetap Release, tanpa AllowDebugging.
    /// </summary>
    [MenuItem("Tools/UAB METO/5c. Build Android APK (arm64 + armv7)", priority = 8)]
    public static void BuildUniversalApk()
    {
        if (!EditorSceneManagerSafeSave())
        {
            Debug.Log("Build Android universal dibatalkan.");
            return;
        }

        ConfigureAndroidPlayerSettings();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log("Beralih ke platform Android (butuh reimport, bisa beberapa menit)...");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Gagal beralih ke platform Android.");
                EditorApplication.Exit(1);
                return;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(UniversalApkOutputPath) ?? "Builds/Android");

        // ConfigureAndroidPlayerSettings menetapkan ARM64; simpan nilai itu untuk
        // dipulihkan setelah build, apa pun hasilnya.
        AndroidArchitecture originalArchitectures = PlayerSettings.Android.targetArchitectures;
        bool succeeded = false;
        try
        {
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            AssetDatabase.SaveAssets();
            Debug.Log($"BUILD UNIVERSAL: arsitektur = {PlayerSettings.Android.targetArchitectures}");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = SceneList(),
                locationPathName = UniversalApkOutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            UnityEditor.Build.Reporting.BuildSummary summary = report.summary;

            Debug.Log($"BUILD UNIVERSAL RESULT: {summary.result} | " +
                      $"{summary.totalSize / (1024f * 1024f):0.0} MB | errors: {summary.totalErrors} | " +
                      UniversalApkOutputPath);
            succeeded = summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        }
        finally
        {
            PlayerSettings.Android.targetArchitectures = originalArchitectures;
            AssetDatabase.SaveAssets();
            Debug.Log($"BUILD UNIVERSAL: arsitektur dipulihkan = {PlayerSettings.Android.targetArchitectures}");
        }

        if (!succeeded)
        {
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/UAB METO/6. Apply Android Player Settings", priority = 9)]
    public static void ApplySettingsOnly()
    {
        ConfigureAndroidPlayerSettings();
        Debug.Log("Android player settings diterapkan.");
    }

    private static bool EditorSceneManagerSafeSave()
    {
        return UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    /// <summary>Daftar scene diambil dari Build Settings agar tidak ada duplikasi.</summary>
    private static string[] SceneList()
    {
        List<string> scenes = new List<string>();
        EditorBuildSettingsScene[] configured = EditorBuildSettings.scenes;
        for (int i = 0; i < configured.Length; i++)
        {
            if (configured[i] != null && configured[i].enabled)
            {
                scenes.Add(configured[i].path);
            }
        }

        if (scenes.Count == 0)
        {
            Debug.LogError("Build Settings kosong. Jalankan 'UAB Meto/3 - Build Game Scenes'.");
        }

        return scenes.ToArray();
    }

    private static void ConfigureAndroidPlayerSettings()
    {
        PlayerSettings.companyName = "UAB Meto";
        PlayerSettings.productName = "UAB Meto";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.uabmeto.village");

        // Android 8.0 (API 26), sesuai ProjectSettings; OPPO A3s memakai Android 8.1.
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.bundleVersionCode = 1;

        // Hanya arm64: OPPO A3s sudah 64-bit dan build jadi lebih cepat.
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
        // OptimizeSize: kode IL2CPP lebih kecil -> compile native (clang) lebih ringan
        // dan tidak sering kehabisan RAM/paging di mesin dengan memori terbatas.
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);

        // OpenGLES3 saja, paling aman untuk Adreno 506 / Android 8.1.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        // Peta berbentuk landscape.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;

        AssetDatabase.SaveAssets();
    }
}
