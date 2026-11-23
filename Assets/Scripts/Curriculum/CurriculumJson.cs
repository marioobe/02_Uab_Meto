using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

/// <summary>
/// Satu-satunya jalur serialisasi/deserialisasi kurikulum (R7 Alternatif A).
/// Dipakai oleh CurriculumValidator (editor), CurriculumImporter (editor), dan CurriculumLoader (runtime).
///
/// Kebijakan JSON:
/// - UTF-8 strict (new UTF8Encoding(false, true)); BOM di-strip setelah decode.
/// - Enum hanya string (StringEnumConverter AllowIntegerValues = false).
/// - Properti tak dikenal diabaikan (MissingMemberHandling.Ignore).
/// - Nama properti duplikat ditolak (DuplicatePropertyNameHandling.Error).
/// </summary>
public static class CurriculumJson
{
    public const int SchemaVersion = 1;

    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.Indented,
        Converters = new List<JsonConverter>
        {
            new StringEnumConverter { AllowIntegerValues = false }
        }
    };

    private static readonly JsonLoadSettings LoadSettings = new JsonLoadSettings
    {
        CommentHandling = CommentHandling.Ignore,
        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
        LineInfoHandling = LineInfoHandling.Load
    };

    /// <summary>Decode UTF-8 strict + strip satu BOM di awal. Melempar DecoderFallbackException bila invalid.</summary>
    public static string DecodeStrictUtf8(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        UTF8Encoding encoding = new UTF8Encoding(false, true);
        string text = encoding.GetString(bytes);
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text.Substring(1);
        }

        return text;
    }

    /// <summary>Deserialisasi dengan kebijakan bersama. Mengembalikan false + pesan error bila gagal.</summary>
    public static bool TryDeserialize(byte[] bytes, out CurriculumRoot root, out string error)
    {
        root = null;
        error = null;

        if (bytes == null || bytes.Length == 0)
        {
            error = "File kosong.";
            return false;
        }

        string json;
        try
        {
            json = DecodeStrictUtf8(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            error = "UTF-8 tidak valid: " + exception.Message;
            return false;
        }

        try
        {
            using (StringReader stringReader = new StringReader(json))
            using (JsonTextReader reader = new JsonTextReader(stringReader))
            {
                reader.DateParseHandling = DateParseHandling.None;
                JToken token = JToken.Load(reader, LoadSettings);
                if (token == null || token.Type == JTokenType.Null)
                {
                    error = "JSON kosong / tidak dapat dibaca.";
                    return false;
                }

                JsonSerializer serializer = JsonSerializer.Create(Settings);
                root = token.ToObject<CurriculumRoot>(serializer);
            }
        }
        catch (Exception exception)
        {
            error = "JSON tidak valid: " + exception.Message;
            return false;
        }

        if (root == null)
        {
            error = "JSON tidak dapat dipetakan ke CurriculumRoot.";
            return false;
        }

        return true;
    }

    /// <summary>Serialisasi deterministik (enum sebagai string).</summary>
    public static string Serialize(CurriculumRoot root)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        return JsonConvert.SerializeObject(root, Settings);
    }

    /// <summary>Serialisasi deterministik ke UTF-8 tanpa BOM.</summary>
    public static byte[] SerializeUtf8(CurriculumRoot root)
    {
        return new UTF8Encoding(false).GetBytes(Serialize(root));
    }
}
