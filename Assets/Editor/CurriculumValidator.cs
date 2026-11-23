using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Validator konten kurikulum (PRA-FASE D, Stage 1).
///
/// Tidak menyentuh aset lama. Hanya memvalidasi byte JSON + audio:
/// - UTF-8 strict (new UTF8Encoding(false, true)) SEBELUM parsing, BOM di-strip setelah decode.
/// - Aturan 15 butir (id, ref objectId, audioRef, header WAV/M4A, status ganda, skill, correctIndex).
/// - OGG/MP3 ditolak sebagai format belum didukung.
/// </summary>
public static class CurriculumValidator
{
    public const int SupportedSchemaVersion = 1;

    public static int LastEditorPassed { get; private set; }
    public static int LastEditorTotal { get; private set; }
    public static int LastParserPassed { get; private set; }
    public static int LastParserTotal { get; private set; }

    // ---------------------------------------------------------------- API

    /// <summary>
    /// Validasi byte JSON kurikulum. Mengembalikan true hanya bila TIDAK ada error.
    /// audioRoot = direktori dasar audio (mis. StreamingAssets/Dawan); audioRef relatif terhadapnya.
    /// </summary>
    public static bool ValidateBytes(byte[] bytes, LearningDatabase db, string audioRoot,
        out List<string> errors)
    {
        errors = new List<string>();

        if (bytes == null || bytes.Length == 0)
        {
            errors.Add("File kosong.");
            return false;
        }

        CurriculumRoot root;
        string parseError;
        if (!CurriculumJson.TryDeserialize(bytes, out root, out parseError))
        {
            errors.Add(parseError);
            return false;
        }

        ValidateRoot(root, db, audioRoot, errors);
        return errors.Count == 0;
    }

    // ---------------------------------------------------------------- aturan

    private static void ValidateRoot(CurriculumRoot root, LearningDatabase db, string audioRoot,
        List<string> errors)
    {
        if (root.schemaVersion > SupportedSchemaVersion)
        {
            errors.Add($"schemaVersion {root.schemaVersion} lebih baru dari {SupportedSchemaVersion}.");
        }
        else if (root.schemaVersion <= 0)
        {
            errors.Add("schemaVersion tidak valid.");
        }

        if (root.topics == null)
        {
            errors.Add("topics null.");
            return;
        }

        HashSet<string> topicIds = new HashSet<string>();
        HashSet<string> lessonIds = new HashSet<string>();
        HashSet<string> itemIds = new HashSet<string>();
        HashSet<string> audioPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int t = 0; t < root.topics.Count; t++)
        {
            TopicData topic = root.topics[t];
            if (topic == null)
            {
                errors.Add($"Topic #{t} null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(topic.id))
            {
                errors.Add($"Topic #{t} tanpa id.");
            }
            else if (!topicIds.Add(topic.id))
            {
                errors.Add($"Duplikasi id topic: {topic.id}");
            }

            if (!IsValidStatus(topic.validationStatus, topic.publicationStatus))
            {
                errors.Add($"Status ilegal pada topic {topic.id}: PUBLISHED wajib VALIDATED.");
            }

            if (topic.lessons == null)
            {
                errors.Add($"Topic {topic.id} lessons null.");
                continue;
            }

            bool allLessonsPublished = true;
            for (int l = 0; l < topic.lessons.Count; l++)
            {
                LessonData lesson = topic.lessons[l];
                if (lesson == null)
                {
                    errors.Add($"Lesson #{l} pada topic {topic.id} null.");
                    allLessonsPublished = false;
                    continue;
                }

                ValidateLesson(topic, lesson, db, audioRoot, lessonIds, itemIds, audioPaths, errors);
                if (lesson.publicationStatus != PublicationStatus.PUBLISHED)
                {
                    allLessonsPublished = false;
                }
            }

            if (topic.publicationStatus == PublicationStatus.PUBLISHED && !allLessonsPublished)
            {
                errors.Add($"Topic {topic.id} PUBLISHED tetapi ada lesson yang belum PUBLISHED.");
            }
        }
    }

    private static void ValidateLesson(TopicData topic, LessonData lesson, LearningDatabase db,
        string audioRoot, HashSet<string> lessonIds, HashSet<string> itemIds,
        HashSet<string> audioPaths, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(lesson.id))
        {
            errors.Add($"Lesson tanpa id pada topic {topic.id}.");
        }
        else if (!lessonIds.Add(lesson.id))
        {
            errors.Add($"Duplikasi id lesson: {lesson.id}");
        }

        if (!IsValidStatus(lesson.validationStatus, lesson.publicationStatus))
        {
            errors.Add($"Status ilegal pada lesson {lesson.id}: PUBLISHED wajib VALIDATED.");
        }

        if (lesson.skills == null || lesson.skills.Count == 0)
        {
            errors.Add($"Lesson {lesson.id} tidak punya skills.");
        }

        if (lesson.items == null)
        {
            errors.Add($"Lesson {lesson.id} items null.");
            return;
        }

        bool allItemsPublishedAndValidated = true;

        for (int i = 0; i < lesson.items.Count; i++)
        {
            LessonItemData item = lesson.items[i];
            if (item == null)
            {
                errors.Add($"Item #{i} pada lesson {lesson.id} null.");
                allItemsPublishedAndValidated = false;
                continue;
            }

            ValidateItem(lesson, item, db, audioRoot, itemIds, audioPaths, errors);

            if (item.validationStatus != ValidationStatus.VALIDATED ||
                item.publicationStatus != PublicationStatus.PUBLISHED)
            {
                allItemsPublishedAndValidated = false;
            }
        }

        if (lesson.publicationStatus == PublicationStatus.PUBLISHED && !allItemsPublishedAndValidated)
        {
            errors.Add($"Lesson {lesson.id} PUBLISHED tetapi ada item yang belum VALIDATED+PUBLISHED.");
        }
    }

    private static void ValidateItem(LessonData lesson, LessonItemData item, LearningDatabase db,
        string audioRoot, HashSet<string> itemIds, HashSet<string> audioPaths, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(item.id))
        {
            errors.Add($"Item pada lesson {lesson.id} tanpa id.");
        }
        else if (!itemIds.Add(item.id))
        {
            errors.Add($"Duplikasi id item: {item.id}");
        }

        if (!IsValidStatus(item.validationStatus, item.publicationStatus))
        {
            errors.Add($"Status ilegal pada item {item.id}: PUBLISHED wajib VALIDATED.");
        }

        if (string.IsNullOrWhiteSpace(item.type))
        {
            errors.Add($"Item {item.id} tanpa type.");
        }
        else if (item.type != "vocabulary" && item.type != "dialogue" && item.type != "exercise")
        {
            errors.Add($"Item {item.id} type tidak dikenal: {item.type}");
        }

        if (item.type == "vocabulary")
        {
            if (string.IsNullOrWhiteSpace(item.objectRef))
            {
                errors.Add($"Vocabulary {item.id} tanpa ref.");
            }
            else if (db == null || db.Find(item.objectRef) == null)
            {
                errors.Add($"Vocabulary {item.id} ref yatim: {item.objectRef}");
            }
        }

        if (item.type == "exercise")
        {
            if (string.IsNullOrWhiteSpace(item.skill) || lesson.skills == null ||
                !lesson.skills.Contains(item.skill))
            {
                errors.Add($"Exercise {item.id} skill '{item.skill}' tidak ada pada skills lesson {lesson.id}.");
            }

            if (item.options == null || item.options.Count < 2)
            {
                errors.Add($"Exercise {item.id} butuh minimal 2 opsi.");
            }
            else if (item.correctIndex < 0 || item.correctIndex >= item.options.Count)
            {
                errors.Add($"Exercise {item.id} correctIndex di luar batas.");
            }
        }

        if (item.type == "dialogue")
        {
            if (item.lines == null || item.lines.Count == 0)
            {
                errors.Add($"Dialogue {item.id} tanpa baris.");
            }

            if (string.IsNullOrWhiteSpace(item.audioRef))
            {
                errors.Add($"Dialogue {item.id} tanpa audioRef.");
            }
        }

        if (!string.IsNullOrWhiteSpace(item.audioRef))
        {
            ValidateAudioRef(item, audioRoot, audioPaths, errors);
        }
    }

    private static void ValidateAudioRef(LessonItemData item, string audioRoot,
        HashSet<string> audioPaths, List<string> errors)
    {
        string audioRef = item.audioRef.Replace('\\', '/');

        // 1. Path absolut ditolak.
        if (Path.IsPathRooted(audioRef) || audioRef.StartsWith("/", StringComparison.Ordinal) ||
            (audioRef.Length > 1 && audioRef[1] == ':'))
        {
            errors.Add($"Audio {item.id} memakai path absolut: {item.audioRef}");
            return;
        }

        // 2. Traversal ditolak (eksplisit + canonical).
        if (audioRef.Contains("../") || audioRef == ".." || audioRef.Contains("..\\"))
        {
            errors.Add($"Audio {item.id} memuat traversal: {item.audioRef}");
            return;
        }

        if (string.IsNullOrWhiteSpace(audioRoot))
        {
            errors.Add($"Audio {item.id} tidak dapat divalidasi: audioRoot kosong.");
            return;
        }

        // 3. Format (ekstensi) sebelum menyentuh berkas. Daftar sama dengan
        // DawanAudioPlayer lewat DawanAudioFormat agar tidak melenceng.
        string extension = Path.GetExtension(audioRef).ToLowerInvariant();
        if (!DawanAudioFormat.IsSupportedExtension(extension))
        {
            errors.Add($"Audio {item.id} format belum didukung: {extension}");
            return;
        }

        // 4. Canonical path wajib di dalam audioRoot.
        string rootFull;
        string full;
        try
        {
            rootFull = Path.GetFullPath(audioRoot).TrimEnd(Path.DirectorySeparatorChar, '/');
            full = Path.GetFullPath(Path.Combine(rootFull, audioRef));
        }
        catch (Exception exception)
        {
            errors.Add($"Audio {item.id} path tidak valid: {exception.Message}");
            return;
        }

        bool inside = full.StartsWith(rootFull + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase) ||
            string.Equals(full, rootFull, StringComparison.OrdinalIgnoreCase);
        if (!inside)
        {
            errors.Add($"Audio {item.id} keluar dari audioRoot: {item.audioRef}");
            return;
        }

        // 5. Keberadaan.
        if (!File.Exists(full))
        {
            errors.Add($"Audio {item.id} tidak ditemukan: {item.audioRef}");
            return;
        }

        // 6. Keterbacaan.
        byte[] data;
        try
        {
            data = File.ReadAllBytes(full);
        }
        catch (Exception exception)
        {
            errors.Add($"Audio {item.id} tidak terbaca: {exception.Message}");
            return;
        }

        if (data.Length == 0)
        {
            errors.Add($"Audio {item.id} kosong (0 byte): {item.audioRef}");
            return;
        }

        // 7. Keunikan path.
        if (!audioPaths.Add(full))
        {
            errors.Add($"Audio path duplikat dipakai lebih dari satu item: {item.audioRef}");
        }

        // 8. Header struktural.
        if (extension == ".wav")
        {
            if (!IsValidWav(data))
            {
                errors.Add($"Audio {item.id} header WAV tidak valid: {item.audioRef}");
            }
        }
        else if (extension == ".m4a")
        {
            if (!IsValidM4a(data))
            {
                errors.Add($"Audio {item.id} header M4A tidak valid: {item.audioRef}");
            }
        }
    }

    private static bool IsValidStatus(ValidationStatus validation, PublicationStatus publication)
    {
        int v = (int)validation;
        int p = (int)publication;
        if (v < 0 || v > 2 || p < 0 || p > 1)
        {
            return false;
        }

        return !(publication == PublicationStatus.PUBLISHED && validation != ValidationStatus.VALIDATED);
    }

    // ---------------------------------------------------------------- audio header

    private static bool IsValidWav(byte[] data)
    {
        if (data == null || data.Length < 44)
        {
            return false;
        }

        if (Tag(data, 0) != "RIFF" || Tag(data, 8) != "WAVE")
        {
            return false;
        }

        uint riffSize = ReadU32Le(data, 4);
        if ((long)riffSize + 8 != data.Length)
        {
            return false;
        }

        int position = 12;
        bool hasFmt = false;
        bool hasData = false;
        int guard = 0;

        while (position + 8 <= data.Length && guard++ < 64)
        {
            string id = Tag(data, position);
            uint size = ReadU32Le(data, position + 4);
            if (size > data.Length - (position + 8))
            {
                return false;
            }

            if (id == "fmt ")
            {
                if (size < 16)
                {
                    return false;
                }

                ushort format = ReadU16Le(data, position + 8);
                if (format != 1)
                {
                    return false;
                }

                hasFmt = true;
            }
            else if (id == "data")
            {
                if (size == 0)
                {
                    return false;
                }

                hasData = true;
            }

            position += 8 + (int)size + ((int)size & 1);
        }

        return hasFmt && hasData;
    }

    private static bool IsValidM4a(byte[] data)
    {
        if (data == null || data.Length < 12)
        {
            return false;
        }

        long position = 0;
        bool hasFtyp = false;
        bool hasOther = false;
        int guard = 0;

        while (position + 8 <= data.Length && guard++ < 64)
        {
            uint size32 = ReadU32Be(data, (int)position);
            string type = Tag(data, (int)position + 4);
            long size;

            if (size32 == 1)
            {
                if (position + 16 > data.Length)
                {
                    return false;
                }

                ulong size64 = ReadU64Be(data, (int)position + 8);
                if (size64 < 16 || size64 > (ulong)data.Length)
                {
                    return false;
                }

                size = (long)size64;
            }
            else if (size32 == 0)
            {
                size = data.Length - position;
            }
            else
            {
                size = size32;
            }

            if (size < 8 || position + size > data.Length)
            {
                return false;
            }

            if (type == "ftyp")
            {
                if (size < 12)
                {
                    return false;
                }

                hasFtyp = true;
            }
            else
            {
                hasOther = true;
            }

            position += size;
        }

        return hasFtyp && hasOther && position == data.Length;
    }

    private static string Tag(byte[] data, int offset)
    {
        if (data == null || offset < 0 || offset + 4 > data.Length)
        {
            return string.Empty;
        }

        return Encoding.ASCII.GetString(data, offset, 4);
    }

    private static uint ReadU32Le(byte[] data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) |
                      (data[offset + 3] << 24));
    }

    private static ushort ReadU16Le(byte[] data, int offset)
    {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }

    private static uint ReadU32Be(byte[] data, int offset)
    {
        return (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                      (data[offset + 2] << 8) | data[offset + 3]);
    }

    private static ulong ReadU64Be(byte[] data, int offset)
    {
        ulong value = 0;
        for (int i = 0; i < 8; i++)
        {
            value = (value << 8) | data[offset + i];
        }

        return value;
    }

    // ---------------------------------------------------------------- self-test

    /// <summary>
    /// Self-test editor (fixture inline). Jalankan:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod CurriculumValidator.EditorSelfTest
    /// </summary>
    public static void EditorSelfTest()
    {
        string audioRoot = Path.Combine(Application.temporaryCachePath, "UabMeto_Stage1_Audio");
        try
        {
            PrepareAudioFixtures(audioRoot);

            LearningDatabase db = ScriptableObject.CreateInstance<LearningDatabase>();
            LearningObjectDefinition periuk = ScriptableObject.CreateInstance<LearningObjectDefinition>();
            periuk.id = "periuk";
            db.objects.Add(periuk);

            int total = 0;
            int passed = 0;

            // ---------------- Positif ----------------
            total++; passed += Check("P01 curriculum valid (M4A)", BuildValidRoot("valid.m4a"), db, audioRoot, true);
            total++; passed += Check("P02 curriculum valid (WAV)", BuildValidRoot("valid.wav"), db, audioRoot, true);
            total++; passed += Check("P03 UTF-8 dengan BOM", WithBom(BuildValidRoot("valid.m4a")), db, audioRoot, true);
            total++; passed += Check("P04 vocabulary tanpa audio", BuildVocabularyOnlyRoot(), db, audioRoot, true);

            // ---------------- Negatif ----------------
            total++; passed += Check("N01 UTF-8 tidak valid", InvalidUtf8(BuildValidRoot("valid.m4a")), db, audioRoot, false);
            total++; passed += Check("N02 schemaVersion 999", Mutate(BuildValidRoot("valid.m4a"), r => r.schemaVersion = 999), db, audioRoot, false);
            total++; passed += Check("N03 duplikasi id topic", Mutate(BuildValidRoot("valid.m4a"), r => r.topics.Add(CloneTopic(r.topics[0]))), db, audioRoot, false);
            total++; passed += Check("N04 duplikasi id lesson", Mutate(BuildValidRoot("valid.m4a"), r => r.topics[0].lessons.Add(CloneLesson(r.topics[0].lessons[0]))), db, audioRoot, false);
            total++; passed += Check("N05 duplikasi id item", Mutate(BuildValidRoot("valid.m4a"), r => r.topics[0].lessons[0].items.Add(CloneItem(r.topics[0].lessons[0].items[0]))), db, audioRoot, false);
            total++; passed += Check("N06 ref yatim", Mutate(BuildValidRoot("valid.m4a"), r => r.topics[0].lessons[0].items[0].objectRef = "tidak_ada"), db, audioRoot, false);
            total++; passed += Check("N07 audioRef traversal", SetDialogueAudio("../escape.m4a"), db, audioRoot, false);
            total++; passed += Check("N08 audioRef absolut", SetDialogueAudio("C:/absolute.m4a"), db, audioRoot, false);
            total++; passed += Check("N09 audioRef .ogg", SetDialogueAudio("valid.ogg"), db, audioRoot, false);
            total++; passed += Check("N10 M4A tanpa ftyp", SetDialogueAudio("bad.m4a"), db, audioRoot, false);
            total++; passed += Check("N11 WAV tanpa RIFF", SetDialogueAudio("bad.wav"), db, audioRoot, false);
            total++; passed += Check("N12 audioRef tidak ada", SetDialogueAudio("missing.m4a"), db, audioRoot, false);
            total++; passed += Check("N13 audioRef 0 byte", SetDialogueAudio("empty.m4a"), db, audioRoot, false);
            total++; passed += Check("N14 audioRef duplikat", DuplicateAudio("valid.m4a"), db, audioRoot, false);
            total++; passed += Check("N15 skill di luar skills", Mutate(BuildValidRoot("valid.m4a"), r => r.topics[0].lessons[0].items[2].skill = "writing"), db, audioRoot, false);
            total++; passed += Check("N16 PUBLISHED + UNVALIDATED", Mutate(BuildValidRoot("valid.m4a"), r => r.topics[0].lessons[0].items[2].validationStatus = ValidationStatus.UNVALIDATED), db, audioRoot, false);
            total++; passed += Check("N17 lesson PUBLISHED tidak lengkap", Mutate(BuildValidRoot("valid.m4a"), r => r.topics[0].lessons[0].items[1].publicationStatus = PublicationStatus.UNPUBLISHED), db, audioRoot, false);
            total++; passed += Check("N18 correctIndex luar batas", Mutate(BuildValidRoot("valid.m4a"), r => r.topics[0].lessons[0].items[2].correctIndex = 9), db, audioRoot, false);
            total++; passed += Check("N19 item tanpa id", Mutate(BuildValidRoot("valid.m4a"), r => r.topics[0].lessons[0].items[2].id = ""), db, audioRoot, false);

            string result = passed == total ? "PASS" : "FAIL";
            LastEditorPassed = passed;
            LastEditorTotal = total;
            Debug.Log($"[Stage1SelfTest] RESULT: {result} {passed}/{total}");

            ParserSelfTest();

            UnityEngine.Object.DestroyImmediate(periuk);
            UnityEngine.Object.DestroyImmediate(db);
        }
        finally
        {
            try
            {
                if (Directory.Exists(audioRoot))
                {
                    Directory.Delete(audioRoot, true);
                }
            }
            catch (Exception)
            {
                // abaikan pembersihan
            }
        }
    }

    /// <summary>
    /// Self-test parser bersama (T118–T123). Dijalankan otomatis oleh EditorSelfTest.
    /// </summary>
    public static void ParserSelfTest()
    {
        int total = 0;
        int passed = 0;

        total++; passed += Expect("T118 duplicate key",
            "{\"schemaVersion\":1,\"schemaVersion\":2,\"topics\":[]}", false);
        total++; passed += Expect("T119 unknown property",
            "{\"schemaVersion\":1,\"locale\":\"id\",\"topics\":[],\"unknown\":123}", true);
        total++; passed += Expect("T120 null/empty",
            "{\"schemaVersion\":1,\"locale\":null,\"topics\":[]}", true);
        total++; passed += Expect("T121 type mismatch",
            "{\"schemaVersion\":\"abc\",\"topics\":[]}", false);
        total++; passed += Expect("T121b enum integer ditolak",
            "{\"schemaVersion\":1,\"topics\":[{\"id\":\"salam\",\"validationStatus\":2}]}", false);
        total++; passed += Expect("T122 numeric boundary",
            "{\"schemaVersion\":2147483647,\"topics\":[]}", true);
        total++; passed += Expect("T123 unicode",
            "{\"schemaVersion\":1,\"locale\":\"id\",\"topics\":[{\"id\":\"salam\",\"title\":{\"id\":\"Salam — it’s ñ\"}}]}",
            true);

        byte[] invalidUtf8 = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"topics\":[]}");
        invalidUtf8[0] = 0xFF;
        total++; passed += ExpectBytes("T123b byte UTF-8 tidak valid", invalidUtf8, false);

        string result = passed == total ? "PASS" : "FAIL";
        LastParserPassed = passed;
        LastParserTotal = total;
        Debug.Log($"[ParserSelfTest] RESULT: {result} {passed}/{total}");
    }

    private static int Expect(string name, string json, bool expectedAccept)
    {
        return ExpectBytes(name, Encoding.UTF8.GetBytes(json), expectedAccept);
    }

    private static int ExpectBytes(string name, byte[] bytes, bool expectedAccept)
    {
        CurriculumRoot root;
        string error;
        bool accepted = CurriculumJson.TryDeserialize(bytes, out root, out error);

        if (accepted == expectedAccept)
        {
            Debug.Log($"[ParserSelfTest] PASS {name} (harapan: {(expectedAccept ? "TERIMA" : "TOLAK")})");
            return 1;
        }

        Debug.LogError($"[ParserSelfTest] GAGAL {name} (harapan: {(expectedAccept ? "TERIMA" : "TOLAK")}, " +
                       $"aktual: {(accepted ? "TERIMA" : "TOLAK")}) error={error}");
        return 0;
    }

    private static int Check(string name, CurriculumRoot root, LearningDatabase db, string audioRoot,
        bool expectedPass)
    {
        byte[] bytes = CurriculumJson.SerializeUtf8(root);
        return Check(name, bytes, db, audioRoot, expectedPass);
    }

    private static int Check(string name, byte[] bytes, LearningDatabase db, string audioRoot,
        bool expectedPass)
    {
        List<string> errors;
        bool actualPass = ValidateBytes(bytes, db, audioRoot, out errors);

        bool ok = actualPass == expectedPass;
        if (ok)
        {
            Debug.Log($"[Stage1SelfTest] PASS {name} (harapan: {(expectedPass ? "LULUS" : "TOLAK")})");
            return 1;
        }

        Debug.LogError($"[Stage1SelfTest] GAGAL {name} (harapan: {(expectedPass ? "LULUS" : "TOLAK")}, " +
                       $"aktual: {(actualPass ? "LULUS" : "TOLAK")}) errors={errors.Count}");
        for (int i = 0; i < errors.Count; i++)
        {
            Debug.LogError("  - " + errors[i]);
        }

        return 0;
    }

    // ---------------------------------------------------------------- fixtures

    private static CurriculumRoot BuildValidRoot(string audioRef)
    {
        CurriculumRoot root = new CurriculumRoot { schemaVersion = 1, locale = "id" };
        TopicData topic = new TopicData
        {
            id = "salam",
            order = 1,
            title = new LocalizedText { id = "Salam dan sapaan" },
            validationStatus = ValidationStatus.VALIDATED,
            publicationStatus = PublicationStatus.PUBLISHED
        };

        LessonData lesson = new LessonData
        {
            id = "salam-01",
            order = 1,
            title = new LocalizedText { id = "Sapaan dasar" },
            objective = new LocalizedText { id = "Menyapa." },
            skills = new List<string> { "reading", "listening" },
            validationStatus = ValidationStatus.VALIDATED,
            publicationStatus = PublicationStatus.PUBLISHED
        };

        lesson.items.Add(new LessonItemData
        {
            id = "salam-01-voc-1",
            type = "vocabulary",
            validationStatus = ValidationStatus.VALIDATED,
            publicationStatus = PublicationStatus.PUBLISHED,
            objectRef = "periuk"
        });

        lesson.items.Add(new LessonItemData
        {
            id = "salam-01-dlg-1",
            type = "dialogue",
            validationStatus = ValidationStatus.VALIDATED,
            publicationStatus = PublicationStatus.PUBLISHED,
            audioRef = audioRef,
            lines = new List<DialogueLineData>
            {
                new DialogueLineData { speaker = "Pemandu", text = "Selamat pagi." },
                new DialogueLineData { speaker = "Pemain", text = "Selamat pagi." }
            }
        });

        lesson.items.Add(new LessonItemData
        {
            id = "salam-01-ex-1",
            type = "exercise",
            validationStatus = ValidationStatus.VALIDATED,
            publicationStatus = PublicationStatus.PUBLISHED,
            skill = "reading",
            kind = "multiple_choice",
            question = "Sapaan?",
            options = new List<string> { "A", "B" },
            correctIndex = 0
        });

        topic.lessons.Add(lesson);
        root.topics.Add(topic);
        return root;
    }

    private static CurriculumRoot BuildVocabularyOnlyRoot()
    {
        CurriculumRoot root = BuildValidRoot("valid.m4a");
        LessonData lesson = root.topics[0].lessons[0];
        lesson.items.RemoveAt(2);
        lesson.items.RemoveAt(1);
        return root;
    }

    private static CurriculumRoot Mutate(CurriculumRoot root, Action<CurriculumRoot> change)
    {
        change(root);
        return root;
    }

    private static CurriculumRoot SetDialogueAudio(string audioRef)
    {
        CurriculumRoot root = BuildValidRoot("valid.m4a");
        root.topics[0].lessons[0].items[1].audioRef = audioRef;
        return root;
    }

    private static CurriculumRoot DuplicateAudio(string audioRef)
    {
        CurriculumRoot root = BuildValidRoot(audioRef);
        LessonData lesson = root.topics[0].lessons[0];
        LessonItemData duplicate = CloneItem(lesson.items[1]);
        duplicate.id = "salam-01-dlg-2";
        lesson.items.Add(duplicate);
        return root;
    }

    private static TopicData CloneTopic(TopicData source)
    {
        TopicData clone = new TopicData
        {
            id = source.id,
            order = source.order + 1,
            title = source.title,
            validationStatus = source.validationStatus,
            publicationStatus = source.publicationStatus
        };
        return clone;
    }

    private static LessonData CloneLesson(LessonData source)
    {
        LessonData clone = new LessonData
        {
            id = source.id,
            order = source.order + 1,
            title = source.title,
            objective = source.objective,
            skills = new List<string>(source.skills),
            validationStatus = source.validationStatus,
            publicationStatus = source.publicationStatus
        };
        return clone;
    }

    private static LessonItemData CloneItem(LessonItemData source)
    {
        return new LessonItemData
        {
            id = source.id,
            type = source.type,
            validationStatus = source.validationStatus,
            publicationStatus = source.publicationStatus,
            objectRef = source.objectRef,
            skill = source.skill,
            kind = source.kind,
            question = source.question,
            options = source.options != null ? new List<string>(source.options) : new List<string>(),
            correctIndex = source.correctIndex,
            audioRef = source.audioRef,
            lines = source.lines != null ? new List<DialogueLineData>(source.lines) : new List<DialogueLineData>()
        };
    }

    private static byte[] WithBom(CurriculumRoot root)
    {
        byte[] json = CurriculumJson.SerializeUtf8(root);
        byte[] result = new byte[json.Length + 3];
        result[0] = 0xEF;
        result[1] = 0xBB;
        result[2] = 0xBF;
        Array.Copy(json, 0, result, 3, json.Length);
        return result;
    }

    private static byte[] InvalidUtf8(CurriculumRoot root)
    {
        byte[] json = CurriculumJson.SerializeUtf8(root);
        json[0] = 0xFF;
        return json;
    }

    // ---------------------------------------------------------------- audio fixtures

    private static void PrepareAudioFixtures(string audioRoot)
    {
        Directory.CreateDirectory(audioRoot);

        File.WriteAllBytes(Path.Combine(audioRoot, "valid.m4a"), BuildM4a());
        File.WriteAllBytes(Path.Combine(audioRoot, "valid.wav"), BuildWav());
        File.WriteAllBytes(Path.Combine(audioRoot, "bad.m4a"), Encoding.ASCII.GetBytes("garbagegarbage"));
        File.WriteAllBytes(Path.Combine(audioRoot, "bad.wav"), Encoding.ASCII.GetBytes("garbagegarbage"));
        File.WriteAllBytes(Path.Combine(audioRoot, "empty.m4a"), new byte[0]);
    }

    private static byte[] BuildM4a()
    {
        // ftyp (16 byte) + mdat (16 byte) = 32 byte
        byte[] data = new byte[32];
        WriteU32Be(data, 0, 16);
        WriteTag(data, 4, "ftyp");
        WriteTag(data, 8, "M4A ");
        WriteU32Be(data, 12, 0);

        WriteU32Be(data, 16, 16);
        WriteTag(data, 20, "mdat");
        for (int i = 24; i < 32; i++)
        {
            data[i] = (byte)i;
        }

        return data;
    }

    private static byte[] BuildWav()
    {
        int dataSize = 4;
        byte[] data = new byte[44 + dataSize];
        WriteTag(data, 0, "RIFF");
        WriteU32Le(data, 4, (uint)(36 + dataSize));
        WriteTag(data, 8, "WAVE");
        WriteTag(data, 12, "fmt ");
        WriteU32Le(data, 16, 16);
        data[20] = 1; // PCM
        data[21] = 0;
        data[22] = 1; // mono
        data[23] = 0;
        WriteU32Le(data, 24, 44100);
        WriteU32Le(data, 28, 88200);
        data[32] = 2;
        data[33] = 0;
        data[34] = 16;
        data[35] = 0;
        WriteTag(data, 36, "data");
        WriteU32Le(data, 40, (uint)dataSize);
        data[44] = 1;
        data[45] = 2;
        data[46] = 3;
        data[47] = 4;
        return data;
    }

    private static void WriteTag(byte[] data, int offset, string tag)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(tag);
        Array.Copy(bytes, 0, data, offset, 4);
    }

    private static void WriteU32Le(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static void WriteU32Be(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)((value >> 24) & 0xFF);
        data[offset + 1] = (byte)((value >> 16) & 0xFF);
        data[offset + 2] = (byte)((value >> 8) & 0xFF);
        data[offset + 3] = (byte)(value & 0xFF);
    }
}
