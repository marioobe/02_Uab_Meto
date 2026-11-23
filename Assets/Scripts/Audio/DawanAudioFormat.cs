using System;
using System.IO;

/// <summary>
/// Satu sumber kebenaran format audio kurikulum, dipakai validator (editor)
/// dan DawanAudioPlayer (runtime) agar keduanya tidak berbeda pendapat.
/// Format utama hasil konversi pipeline adalah M4A; WAV diterima karena
/// validator sudah mendukungnya sejak Stage 1.
/// </summary>
public static class DawanAudioFormat
{
    /// <summary>Ekstensi utama (pipeline selalu menghasilkan m4a).</summary>
    public const string PrimaryExtension = ".m4a";

    /// <summary>Ekstensi yang diterima validator dan pemutar (huruf kecil).</summary>
    public static readonly string[] SupportedExtensions = { ".m4a", ".wav" };

    public static bool IsSupportedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        string normalized = extension.ToLowerInvariant();
        for (int i = 0; i < SupportedExtensions.Length; i++)
        {
            if (SupportedExtensions[i] == normalized)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Normalisasi audioRef relatif ke folder audio (mis. "periuk.m4a",
    /// "sub/uji.wav"). Path absolut, traversal, drive letter, dan ekstensi
    /// yang belum didukung ditolak. Subfolder tetap diizinkan, sama seperti
    /// aturan validator.
    /// </summary>
    public static bool TryNormalizePath(string relativePath, out string normalizedPath)
    {
        normalizedPath = null;
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        string normalized = relativePath.Replace('\\', '/').Trim();
        if (normalized.Length == 0)
        {
            return false;
        }

        if (Path.IsPathRooted(normalized) || normalized.StartsWith("/", StringComparison.Ordinal) ||
            (normalized.Length > 1 && normalized[1] == ':'))
        {
            return false;
        }

        if (normalized.Contains(".."))
        {
            return false;
        }

        string extension = Path.GetExtension(normalized);
        if (normalized.Length <= extension.Length || !IsSupportedExtension(extension))
        {
            return false;
        }

        normalizedPath = normalized;
        return true;
    }
}
